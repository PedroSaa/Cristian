using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeTiptar;

public record GetSeTiptarQuery(string DFTACCION) : IRequest<SeTiptarDto>;

public class GetSeTiptarQueryHandler : IRequestHandler<GetSeTiptarQuery, SeTiptarDto>
{
    private readonly ISeTiptarRepository _repo;

    public GetSeTiptarQueryHandler(ISeTiptarRepository repo) => _repo = repo;

    public async Task<SeTiptarDto> Handle(GetSeTiptarQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.DFTACCION)
            ?? throw new KeyNotFoundException($"Acción de tarea {request.DFTACCION} no encontrada.");

        return new SeTiptarDto(entity.DFTACCION, entity.DFTACOBSV, entity.DFTACDESC);
    }
}
