using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.OrdenesCompra.Queries.GetAdjuntoContenido;

public record GetAdjuntoContenidoQuery(Guid OrdenCompraId, Guid AdjuntoId) : IRequest<OrdenCompraAdjuntoContenidoDto>;

public class GetAdjuntoContenidoValidator : AbstractValidator<GetAdjuntoContenidoQuery>
{
    public GetAdjuntoContenidoValidator()
    {
        RuleFor(x => x.OrdenCompraId)
            .NotEmpty().WithMessage("El identificador de la orden de compra es obligatorio.");

        RuleFor(x => x.AdjuntoId)
            .NotEmpty().WithMessage("El identificador del adjunto es obligatorio.");
    }
}

public class GetAdjuntoContenidoHandler : IRequestHandler<GetAdjuntoContenidoQuery, OrdenCompraAdjuntoContenidoDto>
{
    private readonly IOrdenCompraRepository _repo;

    public GetAdjuntoContenidoHandler(IOrdenCompraRepository repo) => _repo = repo;

    public async Task<OrdenCompraAdjuntoContenidoDto> Handle(GetAdjuntoContenidoQuery q, CancellationToken ct)
    {
        var adjunto = await _repo.GetAdjuntoAsync(q.OrdenCompraId, q.AdjuntoId, ct)
            ?? throw new KeyNotFoundException("El adjunto no existe.");

        return new OrdenCompraAdjuntoContenidoDto(
            adjunto.NombreArchivo,
            adjunto.ContentType,
            adjunto.Contenido);
    }
}
