using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSerems;

public record ListSeremsQuery(string? RemTipo = null) : IRequest<IReadOnlyList<SeremDto>>;

public class ListSeremsQueryHandler : IRequestHandler<ListSeremsQuery, IReadOnlyList<SeremDto>>
{
    private readonly ISeremRepository _repo;

    public ListSeremsQueryHandler(ISeremRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeremDto>> Handle(ListSeremsQuery request, CancellationToken ct)
        => (await _repo.GetAllAsync(request.RemTipo))
            .Select(Map)
            .ToList();

    private static SeremDto Map(DocFlow.Domain.Entities.Serem x) => new(
        x.RemCod,
        x.RemTipo,
        x.Tipo.RemDesc,
        x.RemRutValid,
        x.RemSector,
        x.RemNomb,
        x.RemComuna,
        x.RemNro,
        x.RemEmail,
        x.RemFax,
        x.RemRut,
        x.RemDirec,
        x.RemTelef,
        x.RemZip,
        x.RemRegion,
        x.RemBlock,
        x.RemCalle,
        x.RemCodDocDigital);
}
