using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForpla;

public record GetSeForplaQuery(string CodForm) : IRequest<SeForplaDto>;

public class GetSeForplaQueryHandler : IRequestHandler<GetSeForplaQuery, SeForplaDto>
{
    private readonly ISeForplaRepository _repo;

    public GetSeForplaQueryHandler(ISeForplaRepository repo) => _repo = repo;

    public async Task<SeForplaDto> Handle(GetSeForplaQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.CodForm)
            ?? throw new KeyNotFoundException($"Plantilla {request.CodForm} no encontrada.");

        return new SeForplaDto(entity.CodForm, entity.Usucod, entity.TipoCod, entity.NomForm, entity.BlobForm, entity.SisForm, entity.ObsForm, entity.ExtForm, entity.Alto, entity.Ancho);
    }
}
