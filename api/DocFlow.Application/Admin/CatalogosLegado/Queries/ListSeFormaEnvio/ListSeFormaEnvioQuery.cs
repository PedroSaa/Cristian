using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeFormaEnvio;

public record ListSeFormaEnvioQuery : IRequest<IReadOnlyList<SeFormaEnvioDto>>;

public class ListSeFormaEnvioQueryHandler : IRequestHandler<ListSeFormaEnvioQuery, IReadOnlyList<SeFormaEnvioDto>>
{
    private readonly ISeFormaEnvioRepository _repo;

    public ListSeFormaEnvioQueryHandler(ISeFormaEnvioRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeFormaEnvioDto>> Handle(ListSeFormaEnvioQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items.Select(x => new SeFormaEnvioDto(x.IdFormaEnvio, x.FormaEnvio)).ToList();
    }
}
