using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Respaldos.Queries.GetRestoreLogs;

public record GetRestoreLogsQuery(Guid RespaldoId) : IRequest<IReadOnlyList<RestoreLogDto>>;

public class GetRestoreLogsQueryHandler : IRequestHandler<GetRestoreLogsQuery, IReadOnlyList<RestoreLogDto>>
{
    private readonly IRestoreLogRepository _repo;

    public GetRestoreLogsQueryHandler(IRestoreLogRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<RestoreLogDto>> Handle(
        GetRestoreLogsQuery query, CancellationToken ct)
    {
        var logs = await _repo.GetAllAsync();

        return logs
            .Where(l => l.RespaldoId == query.RespaldoId)
            .OrderByDescending(l => l.FechaInicio)
            .Select(l => new RestoreLogDto(
                l.Id,
                l.RespaldoId,
                l.FechaInicio,
                l.FechaFin,
                l.Estado,
                l.MensajeError))
            .ToList();
    }
}
