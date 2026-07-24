using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompra;

public record GetOrdenCompraQuery(Guid Id) : IRequest<OrdenCompraDto>;

public class GetOrdenCompraValidator : AbstractValidator<GetOrdenCompraQuery>
{
    public GetOrdenCompraValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");
    }
}

public class GetOrdenCompraHandler : IRequestHandler<GetOrdenCompraQuery, OrdenCompraDto>
{
    private readonly IOrdenCompraRepository _repo;
    private readonly IProveedorRepository _proveedores;

    public GetOrdenCompraHandler(IOrdenCompraRepository repo, IProveedorRepository proveedores)
    {
        _repo = repo;
        _proveedores = proveedores;
    }

    public async Task<OrdenCompraDto> Handle(GetOrdenCompraQuery q, CancellationToken ct)
    {
        var oc = await _repo.GetByIdAsync(q.Id, ct)
            ?? throw new KeyNotFoundException("La orden de compra no existe.");

        var proveedor = await _proveedores.GetByIdAsync(oc.ProveedorId, ct);
        var adjuntos = await _repo.GetAdjuntosMetadataAsync(oc.Id, ct);

        return OrdenCompraMapper.ToDto(oc, proveedor, adjuntos);
    }
}
