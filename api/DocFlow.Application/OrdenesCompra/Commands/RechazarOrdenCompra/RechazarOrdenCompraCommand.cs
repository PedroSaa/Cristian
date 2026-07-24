using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.RechazarOrdenCompra;

public record RechazarOrdenCompraCommand(Guid Id, string Comentario) : IRequest<OrdenCompraDto>;

public class RechazarOrdenCompraValidator : AbstractValidator<RechazarOrdenCompraCommand>
{
    public RechazarOrdenCompraValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");

        RuleFor(x => x.Comentario)
            .NotEmpty().WithMessage("El comentario de rechazo es obligatorio.")
            .MaximumLength(1000).WithMessage("El comentario no puede superar los 1000 caracteres.");
    }
}

public class RechazarOrdenCompraHandler : IRequestHandler<RechazarOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public RechazarOrdenCompraHandler(
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

    public async Task<OrdenCompraDto> Handle(RechazarOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        oc.Rechazar(usuarioId, cmd.Comentario);

        await _repo.UpdateAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraRechazada",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra {oc.Numero} rechazada: {cmd.Comentario}"));

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);
        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
