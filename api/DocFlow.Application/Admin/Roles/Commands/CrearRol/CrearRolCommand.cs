using DocFlow.Application.Admin.Roles.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Roles.Commands.CrearRol;

public record CrearRolCommand(string Nombre, string? Descripcion) : IRequest<RolDto>;

public class CrearRolCommandValidator : AbstractValidator<CrearRolCommand>
{
    public CrearRolCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del rol es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Descripcion));
    }
}

public class CrearRolCommandHandler : IRequestHandler<CrearRolCommand, RolDto>
{
    private readonly IRolRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public CrearRolCommandHandler(
        IRolRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<RolDto> Handle(CrearRolCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        if (await _repo.ExistsByNombreAsync(cmd.Nombre))
            throw new InvalidOperationException($"Ya existe un rol con el nombre {cmd.Nombre}.");

        var rol = new Rol(Guid.NewGuid(), cmd.Nombre, cmd.Descripcion);
        await _repo.CreateAsync(rol);

        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "RolCreado",
            "Rol",
            rol.Id.ToString(),
            $"Rol creado: {rol.Nombre}");
        await _auditoria.AddAsync(registro);

        return new RolDto(rol.Id, rol.Nombre, rol.Descripcion, rol.EsSistema);
    }
}
