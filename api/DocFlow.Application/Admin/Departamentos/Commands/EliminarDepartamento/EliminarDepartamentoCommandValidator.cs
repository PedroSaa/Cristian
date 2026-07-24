using FluentValidation;

namespace DocFlow.Application.Admin.Departamentos.Commands.EliminarDepartamento;

public class EliminarDepartamentoCommandValidator : AbstractValidator<EliminarDepartamentoCommand>
{
    public EliminarDepartamentoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
