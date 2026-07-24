using DocFlow.Application.Admin.Usuarios.DTOs;
using DocFlow.Application.Common;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Common.Mappings;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Usuarios.Commands.CrearUsuario;

public record CrearUsuarioCommand(
    string Nombres,
    string ApellidoPaterno,
    string ApellidoMaterno,
    string? Telefono,
    string? Direccion,
    string Email,
    string Rol,
    Guid? DepartamentoId,
    string Password,
    string? Rut = null,
    string? Usucod = null
) : IRequest<UsuarioAdminDto>;

public class CrearUsuarioCommandValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioCommandValidator(ISecurityPolicyService securityPolicy)
    {
        var minLength = securityPolicy.GetPasswordMinLength();
        var requireUpper = securityPolicy.GetPasswordRequireUpper();
        var requireSpecial = securityPolicy.GetPasswordRequireSpecial();

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(200).WithMessage("El email no puede superar los 200 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(minLength).WithMessage($"La contraseña debe tener al menos {minLength} caracteres.")
            .Must(password =>
            {
                var result = PasswordPolicyValidator.Validate(password, minLength, requireUpper, requireSpecial);
                return result.IsValid;
            }).WithMessage(x =>
            {
                var result = PasswordPolicyValidator.Validate(x.Password, minLength, requireUpper, requireSpecial);
                return $"La contraseña no cumple con la política de seguridad configurada: {string.Join("; ", result.Errors)}";
            });

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

        RuleFor(x => x.Usucod)
            .MaximumLength(25).WithMessage("El código de usuario no puede superar los 25 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Usucod));

        RuleFor(x => x.Rut)
            .MaximumLength(20).WithMessage("El RUT no puede superar los 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Rut));
    }
}

public class CrearUsuarioCommandHandler : IRequestHandler<CrearUsuarioCommand, UsuarioAdminDto>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CrearUsuarioCommandHandler> _logger;
    private readonly IRolRepository _rolRepo;

    public CrearUsuarioCommandHandler(
        IUsuarioAdminRepository repo,
        IAuditoriaRepository auditoria,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        ILogger<CrearUsuarioCommandHandler> logger,
        IRolRepository rolRepo)
    {
        _repo = repo;
        _auditoria = auditoria;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _logger = logger;
        _rolRepo = rolRepo;
    }

    public async Task<UsuarioAdminDto> Handle(CrearUsuarioCommand cmd, CancellationToken ct)
    {
        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        if (await _repo.ExistsByCorreoAsync(cmd.Email, ct))
            throw new InvalidOperationException($"Ya existe un usuario con el correo {cmd.Email}.");

        if (!string.IsNullOrWhiteSpace(cmd.Rut) && await _repo.ExistsByRutAsync(cmd.Rut, ct))
            throw new InvalidOperationException($"Ya existe un usuario con el RUT {cmd.Rut}.");

        var usucod = await ResolveUsucodAsync(cmd.Usucod, cmd.Email, ct);
        if (!string.IsNullOrWhiteSpace(cmd.Usucod) && await _repo.ExistsByUsucodAsync(usucod, ct))
            throw new InvalidOperationException($"Ya existe un usuario con el código {usucod}.");
        var passwordHash = _passwordHasher.Hash(cmd.Password);

        // Resolve RolId from the dynamic role repository
        var rol = await _rolRepo.GetByNombreAsync(cmd.Rol);
        if (rol is null)
            _logger.LogWarning("Rol entity not found for '{Rol}' — RolId will be null", cmd.Rol);
        var rolId = rol?.Id;
        var rolNombre = rol?.Nombre ?? cmd.Rol;

        var personal = SePersonal.Crear(
            usucod,
            cmd.Nombres,
            cmd.ApellidoPaterno ?? string.Empty,
            cmd.ApellidoMaterno ?? string.Empty,
            cmd.Rut,
            cmd.Email,
            cmd.Telefono,
            cmd.Direccion,
            estado: true);

        var usuario = SeUsuari.Crear(
            Guid.NewGuid(),
            usucod,
            passwordHash,
            rolId,
            cmd.DepartamentoId,
            estadoCuenta: true);

        usuario.VincularPersonal(personal);
        await _repo.CreateAsync(personal, usuario, ct);

        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "CrearUsuario",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario creado: {personal.Correo} con rol {rolNombre} (usucod={usucod})");
        await _auditoria.AddAsync(registro);

        var creado = await _repo.GetByIdAsync(usuario.Id, ct) ?? usuario;
        return creado.ToAdminDto();
    }

    private async Task<string> ResolveUsucodAsync(string? requestedUsucod, string email, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requestedUsucod))
            return requestedUsucod.Trim();

        var baseUsucod = UsuarioSplitMapper.BuildUsucodCandidate(email);
        _logger.LogWarning("Usucod no fue provisto; usando fallback temporal derivado del correo '{Correo}' => '{UsucodBase}'", email, baseUsucod);

        var candidate = baseUsucod;
        var suffix = 1;
        while (await _repo.ExistsByUsucodAsync(candidate, ct))
        {
            candidate = UsuarioSplitMapper.BuildUsucodCandidate(email, suffix++);
        }

        return candidate;
    }
}
