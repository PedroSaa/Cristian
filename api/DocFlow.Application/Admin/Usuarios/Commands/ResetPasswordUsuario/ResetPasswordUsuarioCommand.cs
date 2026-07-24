using DocFlow.Application.Common;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Usuarios.Commands.ResetPasswordUsuario;

public record ResetPasswordUsuarioCommand(Guid Id, string NuevaPassword) : IRequest;

public class ResetPasswordUsuarioCommandValidator : AbstractValidator<ResetPasswordUsuarioCommand>
{
    public ResetPasswordUsuarioCommandValidator(ISecurityPolicyService securityPolicy)
    {
        var minLength = securityPolicy.GetPasswordMinLength();
        var requireUpper = securityPolicy.GetPasswordRequireUpper();
        var requireSpecial = securityPolicy.GetPasswordRequireSpecial();

        RuleFor(x => x.Id).NotEmpty().WithMessage("El id es obligatorio.");
        RuleFor(x => x.NuevaPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(minLength).WithMessage($"La contraseña debe tener al menos {minLength} caracteres.")
            .Must(password =>
            {
                var result = PasswordPolicyValidator.Validate(password, minLength, requireUpper, requireSpecial);
                return result.IsValid;
            }).WithMessage(x =>
            {
                var result = PasswordPolicyValidator.Validate(x.NuevaPassword, minLength, requireUpper, requireSpecial);
                return $"La contraseña no cumple con la política de seguridad configurada: {string.Join("; ", result.Errors)}";
            });
    }
}

public class ResetPasswordUsuarioCommandHandler : IRequestHandler<ResetPasswordUsuarioCommand>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ResetPasswordUsuarioCommandHandler> _logger;

    public ResetPasswordUsuarioCommandHandler(
        IUsuarioAdminRepository repo,
        IAuditoriaRepository auditoria,
        IPasswordHasher passwordHasher,
        IMediator mediator,
        ICurrentUser currentUser,
        ILogger<ResetPasswordUsuarioCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _passwordHasher = passwordHasher;
        _mediator = mediator;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordUsuarioCommand cmd, CancellationToken ct)
    {
        var usuario = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con id {cmd.Id}.");

        var personal = usuario.Personal
            ?? throw new KeyNotFoundException($"No se encontró el perfil personal para el usuario con id {cmd.Id}.");

        var passwordHash = _passwordHasher.Hash(cmd.NuevaPassword);

        usuario.SetPassword(passwordHash);
        usuario.RevokeAuthSessions();
        await _repo.UpdateAsync(personal, usuario, ct);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "ResetPasswordUsuario",
            "Usuario",
            usuario.Id.ToString(),
            $"Contraseña reseteada para usuario: {personal.Correo}");
        await _auditoria.AddAsync(registro);

        await _mediator.Publish(new PasswordCambiadoEvent(usuario.Id, "admin", usuarioId), ct);
    }
}
