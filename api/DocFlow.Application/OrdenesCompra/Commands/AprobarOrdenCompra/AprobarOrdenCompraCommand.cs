using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.AprobarOrdenCompra;

public record AprobarOrdenCompraCommand(Guid Id, string? Comentario = null) : IRequest<OrdenCompraDto>;

public class AprobarOrdenCompraValidator : AbstractValidator<AprobarOrdenCompraCommand>
{
    public AprobarOrdenCompraValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");

        RuleFor(x => x.Comentario)
            .MaximumLength(1000).WithMessage("El comentario no puede superar los 1000 caracteres.");
    }
}

public class AprobarOrdenCompraHandler : IRequestHandler<AprobarOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public AprobarOrdenCompraHandler(
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

    public async Task<OrdenCompraDto> Handle(AprobarOrdenCompraCommand cmd, CancellationToken ct)
    {
        // The approver is always the authenticated user — never taken from the request body.
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        // The domain enforces both the state transition and the segregation-of-duties
        // rule (the creator cannot approve their own order).
        oc.Aprobar(usuarioId, cmd.Comentario);

        await _repo.UpdateAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraAprobada",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra {oc.Numero} aprobada."));

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);
        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
