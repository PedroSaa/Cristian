using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.EnviarAprobacionOrdenCompra;

public record EnviarAprobacionOrdenCompraCommand(Guid Id) : IRequest<OrdenCompraDto>;

public class EnviarAprobacionOrdenCompraValidator : AbstractValidator<EnviarAprobacionOrdenCompraCommand>
{
    public EnviarAprobacionOrdenCompraValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");
    }
}

public class EnviarAprobacionOrdenCompraHandler : IRequestHandler<EnviarAprobacionOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IOrdenCompraNumeracionService _numeracion;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public EnviarAprobacionOrdenCompraHandler(
        IOrdenCompraRepository repo,
        IProveedorRepository proveedores,
        IOrdenCompraNumeracionService numeracion,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _proveedores = proveedores;
        _numeracion = numeracion;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<OrdenCompraDto> Handle(EnviarAprobacionOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        // Consume a number only when the submission is actually possible and the order
        // has no number yet — a resubmission after rejection keeps its original number
        // and an invalid submission must not burn correlatives.
        var puedeEnviar = oc.Estado is EstadoOrdenCompra.Borrador or EstadoOrdenCompra.Rechazada
                          && oc.Items.Count > 0;

        var numero = oc.Numero;
        if (puedeEnviar && numero is null)
            numero = await _numeracion.ObtenerSiguienteNumeroAsync(ct);

        oc.EnviarAAprobacion(numero);

        await _repo.UpdateAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraEnviadaAprobacion",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra {oc.Numero} enviada a aprobación."));

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);
        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
