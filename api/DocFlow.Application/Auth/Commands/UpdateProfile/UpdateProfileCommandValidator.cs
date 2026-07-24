using FluentValidation;

namespace DocFlow.Application.Auth.Commands.UpdateProfile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Nombre) || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Debe enviar al menos un campo editable del perfil.");

        RuleFor(x => x.Nombre)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Nombre));

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
