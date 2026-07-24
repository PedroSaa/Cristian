using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.AgregarAdjuntoOrdenCompra;

public record AgregarAdjuntoOrdenCompraCommand(
    Guid OrdenCompraId,
    string NombreArchivo,
    string ContentType,
    string ContenidoBase64
) : IRequest<OrdenCompraAdjuntoDto>;

public class AgregarAdjuntoOrdenCompraValidator : AbstractValidator<AgregarAdjuntoOrdenCompraCommand>
{
    public AgregarAdjuntoOrdenCompraValidator()
    {
        RuleFor(x => x.OrdenCompraId)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");

        RuleFor(x => x.NombreArchivo)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .MaximumLength(255).WithMessage("El nombre del archivo no puede superar los 255 caracteres.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("El tipo de contenido es obligatorio.")
            .MaximumLength(100).WithMessage("El tipo de contenido no puede superar los 100 caracteres.");

        RuleFor(x => x.ContenidoBase64)
            .NotEmpty().WithMessage("El contenido del adjunto es obligatorio.");
    }
}

public class AgregarAdjuntoOrdenCompraHandler : IRequestHandler<AgregarAdjuntoOrdenCompraCommand, OrdenCompraAdjuntoDto>
{
    public const long TamanoMaximoBytes = 10 * 1024 * 1024; // 10 MB
    public const int MaximoAdjuntos = 10;

    /// <summary>
    /// Allowed attachment content types: documents and images that make sense as purchase
    /// order backing files. Anything else (HTML, executables, generic binaries) is rejected
    /// to reduce the stored-XSS / malware surface of the attachment endpoint.
    /// </summary>
    private static readonly HashSet<string> ContentTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/webp",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
    };

    private readonly IOrdenCompraRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public AgregarAdjuntoOrdenCompraHandler(
        IOrdenCompraRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<OrdenCompraAdjuntoDto> Handle(AgregarAdjuntoOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.OrdenCompraId, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        // Regla de dominio: agregar respaldos es válido en todo estado salvo Anulada
        // (los respaldos posteriores a la aprobación —guías, recepciones— son legítimos).
        oc.ExigirPuedeAgregarAdjuntos();

        if (!ContentTypesPermitidos.Contains(cmd.ContentType.Trim()))
            throw new ValidationException(
                "Tipo de archivo no permitido. Se aceptan PDF, imágenes (PNG, JPEG, WEBP), documentos Word/Excel y texto plano.");

        var existentes = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        if (existentes.Count >= MaximoAdjuntos)
            throw new ValidationException(
                $"La orden de compra ya tiene el máximo de {MaximoAdjuntos} adjuntos.");

        byte[] contenido;
        try
        {
            contenido = Convert.FromBase64String(cmd.ContenidoBase64);
        }
        catch (FormatException)
        {
            throw new ValidationException("El contenido del adjunto no es un base64 válido.");
        }

        if (contenido.LongLength > TamanoMaximoBytes)
            throw new ValidationException("El adjunto no puede superar los 10 MB.");
        if (contenido.Length == 0)
            throw new ValidationException("El contenido del adjunto no puede estar vacío.");

        // The declared content type alone is client-controlled: also verify the file's
        // magic bytes so a renamed executable can't pass as PDF/image/Office.
        if (!FirmaCoincideConTipo(contenido, cmd.ContentType.Trim()))
            throw new ValidationException(
                "El contenido del archivo no corresponde al tipo declarado.");

        var adjunto = OrdenCompraAdjunto.Crear(
            Guid.NewGuid(),
            oc.Id,
            cmd.NombreArchivo,
            cmd.ContentType,
            contenido,
            usuarioId);

        await _repo.AddAdjuntoAsync(adjunto, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraAdjuntoAgregado",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Adjunto agregado a la orden de compra {oc.Numero ?? "(borrador)"}: {adjunto.NombreArchivo} ({adjunto.Tamano} bytes)."));

        return new OrdenCompraAdjuntoDto(
            adjunto.Id,
            adjunto.NombreArchivo,
            adjunto.ContentType,
            adjunto.Tamano,
            adjunto.SubidoPor,
            adjunto.CreadoEn.ToString("o"));
    }

    // ── Magic-byte validation ────────────────────────────────────────────────

    /// <summary>File signatures per allowed content type.</summary>
    private static readonly Dictionary<string, byte[][]> FirmasPorTipo = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = [[0x25, 0x50, 0x44, 0x46]],                          // %PDF
        ["image/png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
        // OpenXML (docx/xlsx) is a zip container: PK\x03\x04
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [[0x50, 0x4B, 0x03, 0x04]],
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = [[0x50, 0x4B, 0x03, 0x04]],
        // Legacy Office (doc/xls) is an OLE compound document
        ["application/msword"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        ["application/vnd.ms-excel"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
    };

    private static bool FirmaCoincideConTipo(byte[] contenido, string contentType)
    {
        // image/webp: RIFF container — "RIFF" at 0 plus "WEBP" at offset 8.
        if (string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase))
        {
            return EmpiezaCon(contenido, [0x52, 0x49, 0x46, 0x46])
                && contenido.Length >= 12
                && contenido[8] == 0x57 && contenido[9] == 0x45 && contenido[10] == 0x42 && contenido[11] == 0x50;
        }

        // text/plain has no signature: reject NUL bytes in the head (typical of renamed binaries).
        if (string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase))
        {
            var alcance = Math.Min(contenido.Length, 512);
            for (var i = 0; i < alcance; i++)
            {
                if (contenido[i] == 0x00)
                    return false;
            }
            return true;
        }

        return FirmasPorTipo.TryGetValue(contentType, out var firmas)
            && firmas.Any(f => EmpiezaCon(contenido, f));
    }

    private static bool EmpiezaCon(byte[] contenido, byte[] firma)
        => contenido.Length >= firma.Length && contenido.AsSpan(0, firma.Length).SequenceEqual(firma);
}
