using DocFlow.Application.Admin.Roles.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Roles.Commands.ActualizarRol;

public record ActualizarRolCommand(Guid Id, string Nombre, string? Descripcion) : IRequest<RolDto>;

public class ActualizarRolCommandValidator : AbstractValidator<ActualizarRolCommand>
{
    public ActualizarRolCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del rol es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Descripcion));
    }
}

public class ActualizarRolCommandHandler : IRequestHandler<ActualizarRolCommand, RolDto>
{
    private readonly IRolRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public ActualizarRolCommandHandler(
        IRolRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<RolDto> Handle(ActualizarRolCommand cmd, CancellationToken ct)
    {
        var rol = await _repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"No se encontró el rol con id {cmd.Id}.");

        if (!string.Equals(rol.Nombre, cmd.Nombre, StringComparison.OrdinalIgnoreCase)
            && await _repo.ExistsByNombreAsync(cmd.Nombre))
            throw new InvalidOperationException($"Ya existe un rol con el nombre {cmd.Nombre}.");

        rol.Update(cmd.Nombre, cmd.Descripcion);
        await _repo.UpdateAsync(rol);

        var registro = RegistroAuditoria.Crear(
            _currentUser.RequireAuthenticatedUserId(),
            "RolActualizado",
            "Rol",
            rol.Id.ToString(),
            $"Rol actualizado: {rol.Nombre}");
        await _auditoria.AddAsync(registro);

        return new RolDto(rol.Id, rol.Nombre, rol.Descripcion, rol.EsSistema);
    }
}
