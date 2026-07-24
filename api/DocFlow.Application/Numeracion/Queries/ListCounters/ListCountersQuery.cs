using DocFlow.Application.Common;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Mappings;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Numeracion.Queries.ListCounters;

public record ListCountersQuery(
    int Page = 1,
    int PageSize = 20,
    bool? Activo = null,
    string? CodigoContador = null,
    string? OrgDepCod = null
) : IRequest<PagedResult<CounterListDto>>;

public class ListCountersHandler : IRequestHandler<ListCountersQuery, PagedResult<CounterListDto>>
{
    private readonly ICounterService _counterService;

    public ListCountersHandler(ICounterService counterService)
        => _counterService = counterService;

    public async Task<PagedResult<CounterListDto>> Handle(ListCountersQuery q, CancellationToken ct)
    {
        var total = await _counterService.GetCountAsync(q.Activo, q.CodigoContador, q.OrgDepCod, ct);
        var items = await _counterService.GetPaginatedAsync(q.Page, q.PageSize, q.Activo, q.CodigoContador, q.OrgDepCod, ct);

        var totalPaginas = total == 0 ? 1 : (int)Math.Ceiling((double)total / q.PageSize);
        var dtos = items.Select(c => c.ToListDto()).ToList();

        return new PagedResult<CounterListDto>(dtos, total, q.Page, totalPaginas);
    }
}
