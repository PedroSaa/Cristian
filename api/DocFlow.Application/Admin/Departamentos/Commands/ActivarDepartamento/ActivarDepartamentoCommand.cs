using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Departamentos.Commands.ActivarDepartamento;

public record ActivarDepartamentoCommand(Guid Id) : IRequest;

public class ActivarDepartamentoCommandHandler : IRequestHandler<ActivarDepartamentoCommand>
{
    private readonly IDepartamentoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActivarDepartamentoCommandHandler> _logger;

    public ActivarDepartamentoCommandHandler(
        IDepartamentoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActivarDepartamentoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActivarDepartamentoCommand cmd, CancellationToken ct)
    {
        var dep = await _repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Departamento {cmd.Id} no encontrado.");

        dep.Activar();
        await _repo.UpdateAsync(dep);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId, "ActivarDepartamento", "Departamento", dep.Id.ToString(),
            $"Departamento activado: {dep.Nombre}");
        await _auditoria.AddAsync(registro);
    }
}
