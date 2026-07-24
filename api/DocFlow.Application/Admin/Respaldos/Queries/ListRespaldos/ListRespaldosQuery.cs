using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Respaldos.Queries.ListRespaldos;

public record ListRespaldosQuery : IRequest<IReadOnlyList<RespaldoDto>>;

public class ListRespaldosQueryHandler : IRequestHandler<ListRespaldosQuery, IReadOnlyList<RespaldoDto>>
{
    private readonly IRespaldoRepository _repo;

    public ListRespaldosQueryHandler(IRespaldoRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<RespaldoDto>> Handle(ListRespaldosQuery query, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items
            .Select(r => new RespaldoDto(
                r.Id,
                r.Nombre,
                r.FechaCreacion,
                r.TamanioBytes,
                r.Estado,
                r.Ruta))
            .ToList();
    }
}
