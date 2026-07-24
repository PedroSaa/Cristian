using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Common.Mappings;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Usuarios.Commands.ActualizarUsuario;

public record ActualizarUsuarioCommand(
    Guid Id,
    string Nombres,
    string ApellidoPaterno,
    string ApellidoMaterno,
    string? Telefono,
    string? Direccion,
    string Rol,
    Guid? DepartamentoId,
    string? Email = null,
    string? Rut = null
) : IRequest;

public class ActualizarUsuarioCommandValidator : AbstractValidator<ActualizarUsuarioCommand>
{
    public ActualizarUsuarioCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El id es obligatorio.");
        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(150).WithMessage("Los nombres no pueden superar los 150 caracteres.");
        RuleFor(x => x.ApellidoPaterno)
            .MaximumLength(100).WithMessage("El apellido paterno no puede superar los 100 caracteres.");
        RuleFor(x => x.ApellidoMaterno)
            .MaximumLength(100).WithMessage("El apellido materno no puede superar los 100 caracteres.");
        RuleFor(x => x.Telefono)
            .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));
        RuleFor(x => x.Direccion)
            .MaximumLength(250).WithMessage("La dirección no puede superar los 250 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Direccion));
        RuleFor(x => x.Rol)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .MaximumLength(100).WithMessage("El rol no puede superar los 100 caracteres.");
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(200).WithMessage("El email no puede superar los 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Rut)
            .MaximumLength(20).WithMessage("El RUT no puede superar los 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Rut));
    }
}

public class ActualizarUsuarioCommandHandler : IRequestHandler<ActualizarUsuarioCommand>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActualizarUsuarioCommandHandler> _logger;
    private readonly IRolRepository _rolRepo;

    public ActualizarUsuarioCommandHandler(
        IUsuarioAdminRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActualizarUsuarioCommandHandler> logger,
        IRolRepository rolRepo)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
        _rolRepo = rolRepo;
    }

    public async Task Handle(ActualizarUsuarioCommand cmd, CancellationToken ct)
    {
        var usuario = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con id {cmd.Id}.");

        var personal = usuario.Personal
            ?? throw new KeyNotFoundException($"No se encontró el perfil personal para el usuario con id {cmd.Id}.");

        // Validate email uniqueness if changed
        if (!string.IsNullOrWhiteSpace(cmd.Email) && !string.Equals(cmd.Email, personal.Correo, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repo.ExistsByCorreoAsync(cmd.Email, ct))
                throw new InvalidOperationException($"Ya existe un usuario con el correo {cmd.Email}.");
        }

        // Validate RUT uniqueness if changed
        if (cmd.Rut is not null && !string.Equals(cmd.Rut, personal.Rut, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(cmd.Rut) && await _repo.ExistsByRutAsync(cmd.Rut, ct))
                throw new InvalidOperationException($"Ya existe un usuario con el RUT {cmd.Rut}.");
        }

        // Resolve RolId from the dynamic role repository
        var displayRol = usuario.RolNombre;
        if (string.IsNullOrWhiteSpace(displayRol) && usuario.RolId.HasValue)
        {
            var currentRol = await _rolRepo.GetByIdAsync(usuario.RolId.Value);
            displayRol = currentRol?.Nombre;
        }
        displayRol ??= string.Empty;
        var rolId = usuario.RolId;
        var rolNombre = displayRol;
        var roleChanged = !string.Equals(cmd.Rol, displayRol, StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(cmd.Rol, displayRol, StringComparison.OrdinalIgnoreCase))
        {
            var rol = await _rolRepo.GetByNombreAsync(cmd.Rol);
            if (rol is null)
                _logger.LogWarning("Rol entity not found for '{Rol}' on update — RolId will be null", cmd.Rol);
            rolId = rol?.Id;
            rolNombre = rol?.Nombre ?? cmd.Rol;
        }

        personal.Actualizar(
            cmd.Nombres,
            cmd.ApellidoPaterno ?? string.Empty,
            cmd.ApellidoMaterno ?? string.Empty,
            cmd.Rut,
            cmd.Email,
            cmd.Telefono,
            cmd.Direccion);

        usuario.ActualizarAcceso(rolId, cmd.DepartamentoId);
        if (roleChanged)
            usuario.RevokeAuthSessions();
        await _repo.UpdateAsync(personal, usuario, ct);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "ActualizarUsuario",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario actualizado: nombre={personal.Nombres} {personal.ApellidoPaterno} {personal.ApellidoMaterno}, email={personal.Correo}, rol={rolNombre}, rut={personal.Rut}");
        await _auditoria.AddAsync(registro);
    }
}
