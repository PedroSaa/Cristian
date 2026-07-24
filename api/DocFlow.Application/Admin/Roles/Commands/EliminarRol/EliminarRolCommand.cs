using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Roles.Commands.EliminarRol;

public record EliminarRolCommand(Guid Id) : IRequest;

public class EliminarRolCommandValidator : AbstractValidator<EliminarRolCommand>
{
    public EliminarRolCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El Id del rol es obligatorio.");
    }
}

public class EliminarRolCommandHandler : IRequestHandler<EliminarRolCommand>
{
    private readonly IRolRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarRolCommandHandler> _logger;

    public EliminarRolCommandHandler(
        IRolRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarRolCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarRolCommand cmd, CancellationToken ct)
    {
        var rol = await _repo.GetByIdWithUsuariosAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Rol {cmd.Id} no encontrado.");

        if (rol.EsSistema)
            throw new InvalidOperationException("No se puede eliminar un rol del sistema.");

        if (rol.Usuarios.Count != 0)
            throw new InvalidOperationException($"No se puede eliminar un rol con {rol.Usuarios.Count} usuarios asignados.");

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _repo.DeleteAsync(rol);

        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "RolEliminado",
            "Rol",
            rol.Id.ToString(),
            $"Rol eliminado: {rol.Nombre}");
        await _auditoria.AddAsync(registro);
    }
}
