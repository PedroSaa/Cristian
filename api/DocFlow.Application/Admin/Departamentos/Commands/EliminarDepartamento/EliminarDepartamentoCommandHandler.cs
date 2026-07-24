using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Departamentos.Commands.EliminarDepartamento;

public class EliminarDepartamentoCommandHandler : IRequestHandler<EliminarDepartamentoCommand>
{
    private readonly IDepartamentoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarDepartamentoCommandHandler> _logger;

    public EliminarDepartamentoCommandHandler(
        IDepartamentoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarDepartamentoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarDepartamentoCommand cmd, CancellationToken ct)
    {
        var dep = await _repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Departamento {cmd.Id} no encontrado.");

        if (dep.Usuarios.Count != 0)
            throw new InvalidOperationException("No se puede eliminar un departamento con usuarios asignados");

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _repo.DeleteAsync(dep);

        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "EliminarDepartamento",
            "Departamento",
            dep.Id.ToString(),
            $"Departamento eliminado: {dep.Nombre} ({dep.Codigo})");
        await _auditoria.AddAsync(registro);
    }
}
