using DocFlow.Application.Admin.Usuarios.Firma.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Firma.Commands.GuardarFirmaUsuario;

/// <summary>
/// Upserts the signature for a user: creates it if none exists, partially updates it otherwise (one per user).
/// This is a PARTIAL update — fields that are not resent are preserved:
/// <list type="bullet">
/// <item>Image (<paramref name="Imagen"/> + <paramref name="ContentType"/>): optional. When provided it is
/// validated and replaces the stored image; when omitted the existing image is kept (required on creation).</item>
/// <item>PIN (<paramref name="Clave"/>): optional. When provided (non-blank) it is encrypted with
/// <see cref="IFirmaClaveProtector"/> and replaces the stored one; when omitted the existing encrypted PIN is kept.</item>
/// <item>Acronym (<paramref name="Sigla"/>): always applied as received.</item>
/// </list>
/// </summary>
public record GuardarFirmaUsuarioCommand(
    Guid UsuarioId,
    byte[]? Imagen,
    string? ContentType,
    string? Clave = null,
    string? Sigla = null) : IRequest<FirmaUsuarioMetadataDto>;

public class GuardarFirmaUsuarioValidator : AbstractValidator<GuardarFirmaUsuarioCommand>
{
    public GuardarFirmaUsuarioValidator()
    {
        RuleFor(x => x.UsuarioId)
            .NotEmpty().WithMessage("El identificador del usuario es obligatorio.");

        // The image is optional (partial update). Validate it only when it is actually sent;
        // whether a missing image is acceptable (existing signature) or not (creation) is decided in the handler.
        When(x => x.Imagen is { Length: > 0 }, () =>
        {
            RuleFor(x => x.Imagen!)
                .Must(img => img.LongLength <= FirmaImagenValidation.MaxImageBytes)
                .WithMessage($"La imagen de la firma no puede superar {FirmaImagenValidation.MaxImageMegabytes} MB.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("El tipo de contenido es obligatorio.")
                .Must(FirmaImagenValidation.IsAllowedContentType)
                .WithMessage("La firma debe ser una imagen PNG o JPEG.");
        });

        RuleFor(x => x.Sigla)
            .MaximumLength(FirmaUsuario.SiglaMaxLength)
            .WithMessage($"La sigla no puede superar los {FirmaUsuario.SiglaMaxLength} caracteres.");
    }
}

public class GuardarFirmaUsuarioHandler : IRequestHandler<GuardarFirmaUsuarioCommand, FirmaUsuarioMetadataDto>
{
    private readonly IFirmaUsuarioRepository _repo;
    private readonly IFirmaClaveProtector _claveProtector;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public GuardarFirmaUsuarioHandler(
        IFirmaUsuarioRepository repo,
        IFirmaClaveProtector claveProtector,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _claveProtector = claveProtector;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<FirmaUsuarioMetadataDto> Handle(GuardarFirmaUsuarioCommand cmd, CancellationToken ct)
    {
        var usuarioAuditoria = _currentUser.RequireAuthenticatedUserId();

        var existente = await _repo.GetByUsuarioAsync(cmd.UsuarioId, ct);

        // Resolve the FINAL image: use the new one (validated) when sent; otherwise keep the existing
        // signature's image. Creation without an image is not allowed.
        byte[] imagenFinal;
        string contentTypeFinal;
        if (cmd.Imagen is { Length: > 0 })
        {
            // The declared content type is client-controlled: also verify the file's magic bytes
            // so a renamed non-image can't pass as PNG/JPEG.
            if (!FirmaImagenValidation.HasMatchingSignature(cmd.Imagen, cmd.ContentType))
                throw new ValidationException("El contenido del archivo no corresponde a una imagen PNG o JPEG.");

            imagenFinal = cmd.Imagen;
            contentTypeFinal = cmd.ContentType!;
        }
        else if (existente is not null)
        {
            imagenFinal = existente.ImagenFirma;
            contentTypeFinal = existente.ContentType;
        }
        else
        {
            throw new ValidationException("La imagen de la firma es obligatoria.");
        }

        // Resolve the FINAL encrypted PIN: encrypt the new one when sent; otherwise KEEP the existing
        // encrypted PIN (partial update — omitting the clave must not clear it). Null on creation without clave.
        var claveCifradaFinal = string.IsNullOrWhiteSpace(cmd.Clave)
            ? existente?.ClaveCifrada
            : _claveProtector.Protect(cmd.Clave);

        FirmaUsuario firma;
        bool esNueva;
        if (existente is null)
        {
            firma = FirmaUsuario.Crear(Guid.NewGuid(), cmd.UsuarioId, imagenFinal, contentTypeFinal, claveCifradaFinal, cmd.Sigla);
            esNueva = true;
        }
        else
        {
            existente.Actualizar(imagenFinal, contentTypeFinal, claveCifradaFinal, cmd.Sigla);
            firma = existente;
            esNueva = false;
        }

        await _repo.UpsertAsync(firma, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioAuditoria,
            esNueva ? "FirmaUsuarioCreada" : "FirmaUsuarioActualizada",
            nameof(FirmaUsuario),
            cmd.UsuarioId.ToString(),
            $"Firma {(esNueva ? "configurada" : "reemplazada")} para el usuario {cmd.UsuarioId} " +
            $"({firma.ImagenFirma.LongLength} bytes, clave: {(claveCifradaFinal is null ? "no" : "sí")})."));

        return new FirmaUsuarioMetadataDto(
            firma.UsuarioId,
            TieneFirma: true,
            TieneClave: firma.ClaveCifrada is not null,
            firma.Sigla,
            firma.ContentType,
            firma.ImagenFirma.LongLength,
            firma.CreadoEn,
            firma.ActualizadoEn);
    }
}
