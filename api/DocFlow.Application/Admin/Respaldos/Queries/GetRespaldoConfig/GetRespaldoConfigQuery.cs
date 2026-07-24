using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Respaldos.Queries.GetRespaldoConfig;

public record GetRespaldoConfigQuery : IRequest<RespaldoConfigDto>;

public class GetRespaldoConfigQueryHandler : IRequestHandler<GetRespaldoConfigQuery, RespaldoConfigDto>
{
    private readonly IRespaldoRepository _repo;

    public GetRespaldoConfigQueryHandler(IRespaldoRepository repo)
    {
        _repo = repo;
    }

    public async Task<RespaldoConfigDto> Handle(GetRespaldoConfigQuery query, CancellationToken ct)
    {
        var config = await _repo.GetRespaldoConfigAsync();

        if (config is null)
        {
            // Return defaults if no config exists yet
            return new RespaldoConfigDto(
                Guid.Empty,
                IntervaloMinutos: 60,
                Habilitado: false,
                MaxBackupCount: 10,
                RetentionDays: 30,
                OutputPath: "./Respaldos",
                TimeoutMinutos: 5,
                ActualizadoEn: DateTime.MinValue);
        }

        return new RespaldoConfigDto(
            config.Id,
            config.IntervaloMinutos,
            config.Habilitado,
            config.MaxBackupCount,
            config.RetentionDays,
            config.OutputPath,
            config.TimeoutMinutos,
            config.ActualizadoEn);
    }
}
