using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Departamentos.Commands.DesactivarDepartamento;

public record DesactivarDepartamentoCommand(Guid Id) : IRequest;

public class DesactivarDepartamentoCommandHandler : IRequestHandler<DesactivarDepartamentoCommand>
{
    private readonly IDepartamentoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DesactivarDepartamentoCommandHandler> _logger;

    public DesactivarDepartamentoCommandHandler(
        IDepartamentoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<DesactivarDepartamentoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(DesactivarDepartamentoCommand cmd, CancellationToken ct)
    {
        var dep = await _repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Departamento {cmd.Id} no encontrado.");

        dep.Desactivar();
        await _repo.UpdateAsync(dep);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId, "DesactivarDepartamento", "Departamento", dep.Id.ToString(),
            $"Departamento desactivado: {dep.Nombre}");
        await _auditoria.AddAsync(registro);
    }
}
