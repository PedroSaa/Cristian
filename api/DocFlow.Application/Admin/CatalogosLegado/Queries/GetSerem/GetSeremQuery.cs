using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSerem;

public record GetSeremQuery(string RemCod) : IRequest<SeremDto>;

public class GetSeremQueryHandler : IRequestHandler<GetSeremQuery, SeremDto>
{
    private readonly ISeremRepository _repo;

    public GetSeremQueryHandler(ISeremRepository repo) => _repo = repo;

    public async Task<SeremDto> Handle(GetSeremQuery request, CancellationToken ct)
    {
        var serem = await _repo.GetByIdAsync(request.RemCod)
            ?? throw new KeyNotFoundException($"Remitente {request.RemCod} no encontrado.");

        return new SeremDto(
            serem.RemCod,
            serem.RemTipo,
            serem.Tipo.RemDesc,
            serem.RemRutValid,
            serem.RemSector,
            serem.RemNomb,
            serem.RemComuna,
            serem.RemNro,
            serem.RemEmail,
            serem.RemFax,
            serem.RemRut,
            serem.RemDirec,
            serem.RemTelef,
            serem.RemZip,
            serem.RemRegion,
            serem.RemBlock,
            serem.RemCalle,
            serem.RemCodDocDigital);
    }
}
