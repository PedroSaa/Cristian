using System.Threading.Channels;
using DocFlow.Application.Admin.Respaldos.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Respaldos.Commands.TriggerRespaldo;

public record TriggerRespaldoCommand : IRequest<RespaldoDto>;

public class TriggerRespaldoCommandHandler : IRequestHandler<TriggerRespaldoCommand, RespaldoDto>
{
    private readonly IRespaldoRepository _repo;
    private readonly ChannelWriter<Guid> _channelWriter;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public TriggerRespaldoCommandHandler(
        IRespaldoRepository repo,
        ChannelWriter<Guid> channelWriter,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _channelWriter = channelWriter;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<RespaldoDto> Handle(TriggerRespaldoCommand command, CancellationToken ct)
    {
        var config = await _repo.GetRespaldoConfigAsync();
        var outputPath = config?.OutputPath ?? "Respaldos";

        var nombre = $"Respaldo-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var ruta = System.IO.Path.Combine(outputPath, $"{nombre}.sql.gz");
        var respaldo = Respaldo.Crear(Guid.NewGuid(), nombre, ruta);

        // Save as Pendiente
        await _repo.AddAsync(respaldo);

        // Enqueue for background processing
        await _channelWriter.WriteAsync(respaldo.Id, ct);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId, "GenerarRespaldo", "Respaldo", respaldo.Id.ToString(),
            $"Respaldo generado: {respaldo.Nombre}",
            _currentUser.IpAddress, _currentUser.UserAgent));

        return new RespaldoDto(
            respaldo.Id,
            respaldo.Nombre,
            respaldo.FechaCreacion,
            respaldo.TamanioBytes,
            respaldo.Estado,
            respaldo.Ruta);
    }
}
