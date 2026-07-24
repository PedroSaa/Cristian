using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Integraciones.Queries.GetIntegracion;

public record GetIntegracionQuery(Guid Id) : IRequest<IntegracionDto>;

public class GetIntegracionQueryHandler : IRequestHandler<GetIntegracionQuery, IntegracionDto>
{
    private readonly IIntegracionRepository _repo;

    public GetIntegracionQueryHandler(IIntegracionRepository repo) => _repo = repo;

    public async Task<IntegracionDto> Handle(GetIntegracionQuery query, CancellationToken ct)
    {
        var integracion = await _repo.GetByIdAsync(query.Id)
            ?? throw new KeyNotFoundException($"Integración {query.Id} no encontrada.");

        return new IntegracionDto(
            integracion.Id,
            integracion.Nombre,
            integracion.Tipo.ToString(),
            integracion.BaseUrl,
            MaskApiKey(integracion.ApiKey),
            integracion.Activo,
            integracion.Settings);
    }

    private static string MaskApiKey(string apiKey) =>
        string.IsNullOrWhiteSpace(apiKey) || apiKey.Length <= 4
            ? "****"
            : "****" + apiKey[^4..];
}
