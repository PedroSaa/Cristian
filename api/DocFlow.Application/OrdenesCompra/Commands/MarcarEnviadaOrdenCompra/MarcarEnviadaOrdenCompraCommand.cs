using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Commands.MarcarEnviadaOrdenCompra;

public record MarcarEnviadaOrdenCompraCommand(Guid Id) : IRequest<OrdenCompraDto>;

public class MarcarEnviadaOrdenCompraValidator : AbstractValidator<MarcarEnviadaOrdenCompraCommand>
{
    public MarcarEnviadaOrdenCompraValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");
    }
}

public class MarcarEnviadaOrdenCompraHandler : IRequestHandler<MarcarEnviadaOrdenCompraCommand, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public MarcarEnviadaOrdenCompraHandler(
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

    public async Task<OrdenCompraDto> Handle(MarcarEnviadaOrdenCompraCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        var oc = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        oc.MarcarEnviada();

        await _repo.UpdateAsync(oc, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "OrdenCompraMarcadaEnviada",
            "OrdenCompra",
            oc.Id.ToString(),
            $"Orden de compra {oc.Numero} marcada como enviada al proveedor."));

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);
        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);
        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
