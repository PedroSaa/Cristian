using DocFlow.Application.Admin.Departamentos.Commands.ActualizarDepartamento;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.ActualizarDepartamento;

public class ActualizarDepartamentoCommandValidatorTests
{
    private readonly ActualizarDepartamentoCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Command()
    {
        var cmd = new ActualizarDepartamentoCommand(Guid.NewGuid(), "Test", "TEST-001");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Nombre_Empty()
    {
        var cmd = new ActualizarDepartamentoCommand(Guid.NewGuid(), "", "TEST-001");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    [Fact]
    public void Should_Have_Error_When_Codigo_Empty()
    {
        var cmd = new ActualizarDepartamentoCommand(Guid.NewGuid(), "Test", "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }
}
