using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Application.OrdenesCompra.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.VincularMercadoPublicoOrdenCompra;

public record VincularMercadoPublicoOrdenCompraCommand(Guid OrdenCompraId, string Codigo)
    : IRequest<OrdenCompraDto>;

public class VincularMercadoPublicoOrdenCompraValidator
    : AbstractValidator<VincularMercadoPublicoOrdenCompraCommand>
{
    public VincularMercadoPublicoOrdenCompraValidator()
    {
        RuleFor(x => x.OrdenCompraId)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código de Mercado Público es obligatorio.")
            .MaximumLength(40).WithMessage("El código de Mercado Público no puede superar los 40 caracteres.");
    }
}

public class VincularMercadoPublicoOrdenCompraHandler
    : IRequestHandler<VincularMercadoPublicoOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IMercadoPublicoService _mercadoPublico;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public VincularMercadoPublicoOrdenCompraHandler(
        IOrdenCompraRepository repo,
        IProveedorRepository proveedores,
        IMercadoPublicoService mercadoPublico,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _proveedores = proveedores;
        _mercadoPublico = mercadoPublico;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<OrdenCompraDto> Handle(VincularMercadoPublicoOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.OrdenCompraId, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        var codigo = cmd.Codigo.Trim();

        // The code must exist in the portal before linking it. A missing code is a user
        // input error (400), while a missing ticket / portal failure bubbles up as
        // InvalidOperationException (503 at the API layer).
        var ordenPortal = await _mercadoPublico.BuscarPorCodigoAsync(codigo, ct)
            ?? throw new ValidationException("El código indicado no existe en Mercado Público.");

        try
        {
            oc.VincularMercadoPublico(ordenPortal.Codigo);
        }
        catch (InvalidOperationException ex)
        {
            // Domain state rejection (e.g. cancelled order) is a request error, not a portal outage.
            throw new ValidationException(ex.Message);
        }

        await _repo.UpdateAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraVinculadaMercadoPublico",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra {oc.Numero ?? "(borrador)"} vinculada a Mercado Público con código {ordenPortal.Codigo}"));

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);
        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
