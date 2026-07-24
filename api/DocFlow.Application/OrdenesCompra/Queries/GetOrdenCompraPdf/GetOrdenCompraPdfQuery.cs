using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Application.OrdenesCompra.Interfaces;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompraPdf;

public record GetOrdenCompraPdfQuery(Guid Id) : IRequest<OrdenCompraPdfDto>;

public class GetOrdenCompraPdfValidator : AbstractValidator<GetOrdenCompraPdfQuery>
{
    public GetOrdenCompraPdfValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");
    }
}

public class GetOrdenCompraPdfHandler : IRequestHandler<GetOrdenCompraPdfQuery, OrdenCompraPdfDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly ISeUsuariRepository _usuarios;
    private readonly IOrdenCompraPdfService _pdfService;

    public GetOrdenCompraPdfHandler(
        IOrdenCompraRepository repo,
        IProveedorRepository proveedores,
        ISeUsuariRepository usuarios,
        IOrdenCompraPdfService pdfService)
    {
        _repo = repo;
        _proveedores = proveedores;
        _usuarios = usuarios;
        _pdfService = pdfService;
    }

    public async Task<OrdenCompraPdfDto> Handle(GetOrdenCompraPdfQuery q, CancellationToken ct)
    {
        var oc = await _repo.GetByIdAsync(q.Id, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);

        // Approver display name — read-only lookup, best effort (null when unavailable).
        string? aprobadorNombre = null;
        var estaAprobada = oc.Estado is EstadoOrdenCompra.Aprobada or EstadoOrdenCompra.Enviada;
        if (estaAprobada && oc.AprobadoPor is { } aprobadorId)
        {
            var usuario = await _usuarios.GetByIdAsync(aprobadorId, ct);
            var personal = usuario?.Personal;
            if (personal is not null)
            {
                aprobadorNombre = string.Join(" ",
                    new[] { personal.Nombres, personal.ApellidoPaterno, personal.ApellidoMaterno }
                        .Where(parte => !string.IsNullOrWhiteSpace(parte)));
            }
        }

        var data = new OrdenCompraPdfData(
            oc.Numero,
            oc.Fecha,
            oc.Estado.ToString(),
            oc.Moneda,
            proveedor?.Nombre ?? string.Empty,
            proveedor?.Rut.Formatted ?? string.Empty,
            proveedor?.Contacto ?? string.Empty,
            proveedor?.Email ?? string.Empty,
            proveedor?.Telefono ?? string.Empty,
            proveedor?.Direccion ?? string.Empty,
            oc.FormaPago,
            oc.PlazoEntrega,
            oc.LugarEntrega,
            oc.Observaciones,
            oc.Neto,
            oc.Iva,
            oc.Total,
            oc.Items
                .OrderBy(i => i.NumeroLinea)
                .Select(i => new OrdenCompraPdfItem(i.NumeroLinea, i.Descripcion, i.Cantidad, i.PrecioUnitario, i.TotalLinea))
                .ToList(),
            estaAprobada ? aprobadorNombre : null,
            estaAprobada ? oc.AprobadoEn : null,
            estaAprobada ? oc.ComentarioAprobacion : null);

        var contenido = _pdfService.Generar(data);

        return new OrdenCompraPdfDto(BuildFileName(oc.Numero, oc.Id), contenido);
    }

    private static string BuildFileName(string? numero, Guid id)
    {
        var baseName = string.IsNullOrWhiteSpace(numero) ? id.ToString() : numero;
        var sanitized = string.Concat(baseName.Select(c =>
            char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-'));
        return $"orden-compra-{sanitized}.pdf";
    }
}
