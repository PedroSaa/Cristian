using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Mappings;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Numeracion.Queries.GetCounter;

public record GetCounterQuery(Guid Id) : IRequest<CounterDto>;

public class GetCounterHandler : IRequestHandler<GetCounterQuery, CounterDto>
{
    private readonly ICounterService _counterService;

    public GetCounterHandler(ICounterService counterService)
        => _counterService = counterService;

    public async Task<CounterDto> Handle(GetCounterQuery q, CancellationToken ct)
    {
        var entity = await _counterService.GetByIdAsync(q.Id, ct);
        return entity.ToDto();
    }
}
