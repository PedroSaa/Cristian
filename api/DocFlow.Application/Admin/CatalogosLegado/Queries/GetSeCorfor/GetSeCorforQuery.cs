using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeCorfor;

public record GetSeCorforQuery(string CorrTip) : IRequest<SeCorforDto>;

public class GetSeCorforQueryHandler : IRequestHandler<GetSeCorforQuery, SeCorforDto>
{
    private readonly ISeCorforRepository _repo;

    public GetSeCorforQueryHandler(ISeCorforRepository repo) => _repo = repo;

    public async Task<SeCorforDto> Handle(GetSeCorforQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.CorrTip)
            ?? throw new KeyNotFoundException($"Correlativo {request.CorrTip} no encontrado.");

        return new SeCorforDto(entity.CorrTip, entity.CorrNro, entity.CorrDes, entity.CorrFch);
    }
}
