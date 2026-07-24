using System.Threading.Channels;
using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Respaldos.Commands.RestoreRespaldo;

public record RestoreRespaldoCommand(Guid RespaldoId) : IRequest<RestoreLogDto>;

public record RestoreRequest(Guid RespaldoId, Guid RestoreLogId);

public class RestoreRespaldoCommandHandler : IRequestHandler<RestoreRespaldoCommand, RestoreLogDto>
{
    private readonly IRestoreLogRepository _repo;
    private readonly IRespaldoRepository _respaldoRepo;
    private readonly ChannelWriter<RestoreRequest> _channelWriter;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public RestoreRespaldoCommandHandler(
        IRestoreLogRepository repo,
        IRespaldoRepository respaldoRepo,
        ChannelWriter<RestoreRequest> channelWriter,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _respaldoRepo = respaldoRepo;
        _channelWriter = channelWriter;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<RestoreLogDto> Handle(RestoreRespaldoCommand command, CancellationToken ct)
    {
        var respaldo = await _respaldoRepo.GetByIdAsync(command.RespaldoId);
        if (respaldo is null)
            throw new KeyNotFoundException($"Respaldo {command.RespaldoId} no encontrado.");

        if (respaldo.Estado != EstadoRespaldo.Completado)
            throw new InvalidOperationException(
                "El respaldo debe estar en estado Completado para restaurarse.");

        var restoreLog = RestoreLog.Crear(command.RespaldoId);

        await _repo.AddAsync(restoreLog);

        await _channelWriter.WriteAsync(
            new RestoreRequest(command.RespaldoId, restoreLog.Id), ct);

        // Auditar: restaurar la BD es la operación más sensible del sistema.
        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId, "RestaurarRespaldo", "Respaldo", command.RespaldoId.ToString(),
            $"Restauración iniciada desde el respaldo: {respaldo.Nombre}",
            _currentUser.IpAddress, _currentUser.UserAgent));

        return new RestoreLogDto(
            restoreLog.Id,
            restoreLog.RespaldoId,
            restoreLog.FechaInicio,
            restoreLog.FechaFin,
            restoreLog.Estado,
            restoreLog.MensajeError);
    }
}
