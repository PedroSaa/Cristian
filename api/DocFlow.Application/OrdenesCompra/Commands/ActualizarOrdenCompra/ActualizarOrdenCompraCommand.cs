using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Commands.CrearOrdenCompra;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.ActualizarOrdenCompra;

public record ActualizarOrdenCompraCommand(
    Guid Id,
    Guid ProveedorId,
    DateTime Fecha,
    string? Moneda = null,
    string? FormaPago = null,
    string? PlazoEntrega = null,
    string? LugarEntrega = null,
    string? Observaciones = null,
    IReadOnlyList<OrdenCompraItemInput>? Items = null
) : IRequest<OrdenCompraDto>;

public class ActualizarOrdenCompraValidator : AbstractValidator<ActualizarOrdenCompraCommand>
{
    public ActualizarOrdenCompraValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");

        RuleFor(x => x.ProveedorId)
            .NotEmpty().WithMessage("El proveedor es obligatorio.");

        RuleFor(x => x.Moneda)
            .MaximumLength(10).WithMessage("La moneda no puede superar los 10 caracteres.");

        RuleFor(x => x.FormaPago)
            .MaximumLength(200).WithMessage("La forma de pago no puede superar los 200 caracteres.");

        RuleFor(x => x.PlazoEntrega)
            .MaximumLength(200).WithMessage("El plazo de entrega no puede superar los 200 caracteres.");

        RuleFor(x => x.LugarEntrega)
            .MaximumLength(300).WithMessage("El lugar de entrega no puede superar los 300 caracteres.");

        RuleFor(x => x.Observaciones)
            .MaximumLength(2000).WithMessage("Las observaciones no pueden superar los 2000 caracteres.");

        RuleForEach(x => x.Items).SetValidator(new OrdenCompraItemInputValidator());
    }
}

public class ActualizarOrdenCompraHandler : IRequestHandler<ActualizarOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public ActualizarOrdenCompraHandler(
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

    public async Task<OrdenCompraDto> Handle(ActualizarOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        var proveedor = await _proveedores.GetByIdAsync(cmd.ProveedorId, ct)
            ?? throw new ValidationException("El proveedor indicado no existe.");

        oc.ActualizarDatos(
            cmd.ProveedorId,
            cmd.Fecha,
            cmd.Moneda,
            cmd.FormaPago,
            cmd.PlazoEntrega,
            cmd.LugarEntrega,
            cmd.Observaciones);

        oc.ReemplazarItems((cmd.Items ?? []).Select(i =>
            new OrdenCompraItemData(i.Descripcion, i.Cantidad, i.PrecioUnitario)));

        await _repo.UpdateAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraActualizada",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra actualizada (total {oc.Total} {oc.Moneda})."));

        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
