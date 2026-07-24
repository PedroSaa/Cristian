using DocFlow.Application.Common;
using DocFlow.Application.Common.Interfaces;
using FluentValidation;

namespace DocFlow.Application.Auth.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator(ISecurityPolicyService securityPolicy)
    {
        var minLength = securityPolicy.GetPasswordMinLength();
        var requireUpper = securityPolicy.GetPasswordRequireUpper();
        var requireSpecial = securityPolicy.GetPasswordRequireSpecial();

        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(minLength).WithMessage($"La contraseña debe tener al menos {minLength} caracteres.")
            .MaximumLength(100)
            .Must(password =>
            {
                var result = PasswordPolicyValidator.Validate(password, minLength, requireUpper, requireSpecial);
                return result.IsValid;
            }).WithMessage(x =>
            {
                var result = PasswordPolicyValidator.Validate(x.NewPassword, minLength, requireUpper, requireSpecial);
                return $"La contraseña no cumple con la política de seguridad configurada: {string.Join("; ", result.Errors)}";
            });
    }
}
