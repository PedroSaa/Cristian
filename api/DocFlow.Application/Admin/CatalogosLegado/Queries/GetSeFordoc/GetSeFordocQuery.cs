using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeFordoc;

public record GetSeFordocQuery(short TipoCod) : IRequest<SeFordocDto>;

public class GetSeFordocQueryHandler : IRequestHandler<GetSeFordocQuery, SeFordocDto>
{
    private readonly ISeFordocRepository _repo;

    public GetSeFordocQueryHandler(ISeFordocRepository repo) => _repo = repo;

    public async Task<SeFordocDto> Handle(GetSeFordocQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.TipoCod)
            ?? throw new KeyNotFoundException($"Formato de documento {request.TipoCod} no encontrado.");

        return new SeFordocDto(entity.TipoCod, entity.TipoRec, entity.TipoInt, entity.TipoDesc, entity.CorrN, entity.CorrFecha, entity.TipoEnv, entity.SeFordocVistaI, entity.SeFordocVistaE, entity.SeFordocVistaR, entity.SeFordocFormatoNum);
    }
}
