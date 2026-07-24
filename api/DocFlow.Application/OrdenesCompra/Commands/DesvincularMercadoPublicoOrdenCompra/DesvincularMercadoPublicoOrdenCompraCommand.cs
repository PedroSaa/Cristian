using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.DesvincularMercadoPublicoOrdenCompra;

public record DesvincularMercadoPublicoOrdenCompraCommand(Guid OrdenCompraId) : IRequest<OrdenCompraDto>;

public class DesvincularMercadoPublicoOrdenCompraValidator
    : AbstractValidator<DesvincularMercadoPublicoOrdenCompraCommand>
{
    public DesvincularMercadoPublicoOrdenCompraValidator()
    {
        RuleFor(x => x.OrdenCompraId)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");
    }
}

public class DesvincularMercadoPublicoOrdenCompraHandler
    : IRequestHandler<DesvincularMercadoPublicoOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public DesvincularMercadoPublicoOrdenCompraHandler(
        IOrdenCompraRepository repo,
        IProveedorRepository proveedores,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _proveedores = proveedores;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<OrdenCompraDto> Handle(DesvincularMercadoPublicoOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.OrdenCompraId, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        var codigoAnterior = oc.CodigoMercadoPublico;

        oc.DesvincularMercadoPublico();

        await _repo.UpdateAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraDesvinculadaMercadoPublico",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra {oc.Numero ?? "(borrador)"} desvinculada de Mercado Público (código anterior: {codigoAnterior ?? "ninguno"})"));

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);
        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
