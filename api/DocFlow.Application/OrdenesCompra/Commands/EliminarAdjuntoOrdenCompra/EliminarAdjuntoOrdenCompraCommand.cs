using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.EliminarAdjuntoOrdenCompra;

public record EliminarAdjuntoOrdenCompraCommand(Guid OrdenCompraId, Guid AdjuntoId) : IRequest;

public class EliminarAdjuntoOrdenCompraValidator : AbstractValidator<EliminarAdjuntoOrdenCompraCommand>
{
    public EliminarAdjuntoOrdenCompraValidator()
    {
        RuleFor(x => x.OrdenCompraId)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");

        RuleFor(x => x.AdjuntoId)
            .NotEmpty().WithMessage("El identificador del adjunto es obligatorio.");
    }
}

public class EliminarAdjuntoOrdenCompraHandler : IRequestHandler<EliminarAdjuntoOrdenCompraCommand>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public EliminarAdjuntoOrdenCompraHandler(
        IOrdenCompraRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(EliminarAdjuntoOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.OrdenCompraId, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        // Regla de dominio: los respaldos que sustentaron la decisión de jefatura no se
        // pueden eliminar tras la aprobación (integridad documental / control interno).
        oc.ExigirPuedeEliminarAdjuntos();

        var adjunto = await _repo.GetAdjuntoAsync(cmd.OrdenCompraId, cmd.AdjuntoId, ct)
            ?? throw new KeyNotFoundException("El adjunto no existe.");

        await _repo.RemoveAdjuntoAsync(adjunto, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraAdjuntoEliminado",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Adjunto eliminado de la orden de compra {oc.Numero ?? "(borrador)"}: {adjunto.NombreArchivo}."));
    }
}
