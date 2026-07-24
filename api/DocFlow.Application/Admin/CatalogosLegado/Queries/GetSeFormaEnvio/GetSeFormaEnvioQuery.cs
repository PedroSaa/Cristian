using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeFormaEnvio;

public record GetSeFormaEnvioQuery(short IdFormaEnvio) : IRequest<SeFormaEnvioDto>;

public class GetSeFormaEnvioQueryHandler : IRequestHandler<GetSeFormaEnvioQuery, SeFormaEnvioDto>
{
    private readonly ISeFormaEnvioRepository _repo;

    public GetSeFormaEnvioQueryHandler(ISeFormaEnvioRepository repo) => _repo = repo;

    public async Task<SeFormaEnvioDto> Handle(GetSeFormaEnvioQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.IdFormaEnvio)
            ?? throw new KeyNotFoundException($"Forma de envío {request.IdFormaEnvio} no encontrada.");

        return new SeFormaEnvioDto(entity.IdFormaEnvio, entity.FormaEnvio);
    }
}
