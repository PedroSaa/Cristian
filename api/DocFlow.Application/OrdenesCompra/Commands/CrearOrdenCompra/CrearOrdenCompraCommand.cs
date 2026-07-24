using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.CrearOrdenCompra;

public record CrearOrdenCompraCommand(
    Guid ProveedorId,
    DateTime Fecha,
    string? Moneda = null,
    string? FormaPago = null,
    string? PlazoEntrega = null,
    string? LugarEntrega = null,
    string? Observaciones = null,
    IReadOnlyList<OrdenCompraItemInput>? Items = null
) : IRequest<OrdenCompraDto>;

public class CrearOrdenCompraValidator : AbstractValidator<CrearOrdenCompraCommand>
{
    public CrearOrdenCompraValidator()
    {
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

public class OrdenCompraItemInputValidator : AbstractValidator<OrdenCompraItemInput>
{
    // Database column limits: Cantidad numeric(18,4), PrecioUnitario numeric(18,2).
    // Values above these caps would overflow at the database and surface as a 500.
    public const decimal CantidadMaxima = 9_999_999_999.9999m;
    public const decimal PrecioUnitarioMaximo = 9_999_999_999_999.99m;

    public OrdenCompraItemInputValidator()
    {
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción del ítem es obligatoria.")
            .MaximumLength(300).WithMessage("La descripción del ítem no puede superar los 300 caracteres.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad del ítem debe ser mayor que cero.")
            .LessThanOrEqualTo(CantidadMaxima)
                .WithMessage("La cantidad del ítem no puede superar 9.999.999.999,9999.");

        RuleFor(x => x.PrecioUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario del ítem no puede ser negativo.")
            .LessThanOrEqualTo(PrecioUnitarioMaximo)
                .WithMessage("El precio unitario del ítem no puede superar 9.999.999.999.999,99.");
    }
}

public class CrearOrdenCompraHandler : IRequestHandler<CrearOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public CrearOrdenCompraHandler(
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

    public async Task<OrdenCompraDto> Handle(CrearOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var proveedor = await _proveedores.GetByIdAsync(cmd.ProveedorId, ct)
            ?? throw new ValidationException("El proveedor indicado no existe.");

        var oc = OrdenCompra.Crear(
            Guid.NewGuid(),
            cmd.ProveedorId,
            cmd.Fecha,
            usuarioId,
            cmd.Moneda,
            cmd.FormaPago,
            cmd.PlazoEntrega,
            cmd.LugarEntrega,
            cmd.Observaciones);

        oc.ReemplazarItems((cmd.Items ?? []).Select(i =>
            new OrdenCompraItemData(i.Descripcion, i.Cantidad, i.PrecioUnitario)));

        await _repo.AddAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraCreada",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra creada en borrador para el proveedor {proveedor.Nombre} (total {oc.Total} {oc.Moneda})."));

        return OrdenCompraMapper.ToDto(oc, proveedor);
    }
}
