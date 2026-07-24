using DocFlow.Application.Admin.Departamentos.Commands.EliminarDepartamento;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.EliminarDepartamento;

public class EliminarDepartamentoCommandValidatorTests
{
    private readonly EliminarDepartamentoCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Id()
    {
        var cmd = new EliminarDepartamentoCommand(Guid.NewGuid());
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Id_Empty()
    {
        var cmd = new EliminarDepartamentoCommand(Guid.Empty);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
