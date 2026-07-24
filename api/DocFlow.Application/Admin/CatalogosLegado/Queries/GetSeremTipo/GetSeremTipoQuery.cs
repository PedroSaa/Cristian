using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeremTipo;

public record GetSeremTipoQuery(string RemTipo) : IRequest<SeremTipoDto>;

public class GetSeremTipoQueryHandler : IRequestHandler<GetSeremTipoQuery, SeremTipoDto>
{
    private readonly ISeremTipoRepository _repo;

    public GetSeremTipoQueryHandler(ISeremTipoRepository repo) => _repo = repo;

    public async Task<SeremTipoDto> Handle(GetSeremTipoQuery request, CancellationToken ct)
    {
        var tipo = await _repo.GetByIdAsync(request.RemTipo)
            ?? throw new KeyNotFoundException($"Tipo de remitente {request.RemTipo} no encontrado.");

        return new SeremTipoDto(tipo.RemTipo, tipo.RemDesc, tipo.Serems.Count);
    }
}
