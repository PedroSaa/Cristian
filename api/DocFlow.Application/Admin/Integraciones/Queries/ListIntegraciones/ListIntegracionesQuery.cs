using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Integraciones.Queries.ListIntegraciones;

public record ListIntegracionesQuery : IRequest<IReadOnlyList<IntegracionDto>>;

public class ListIntegracionesQueryHandler : IRequestHandler<ListIntegracionesQuery, IReadOnlyList<IntegracionDto>>
{
    private readonly IIntegracionRepository _repo;

    public ListIntegracionesQueryHandler(IIntegracionRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<IntegracionDto>> Handle(ListIntegracionesQuery query, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items
            .Select(i => new IntegracionDto(
                i.Id,
                i.Nombre,
                i.Tipo.ToString(),
                i.BaseUrl,
                MaskApiKey(i.ApiKey),
                i.Activo,
                i.Settings))
            .ToList();
    }

    private static string MaskApiKey(string apiKey) =>
        string.IsNullOrWhiteSpace(apiKey) || apiKey.Length <= 4
            ? "****"
            : "****" + apiKey[^4..];
}
