using DocFlow.Application.Admin.Departamentos.Commands.CrearDepartamento;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.CrearDepartamento;

public class CrearDepartamentoCommandValidatorTests
{
    private readonly CrearDepartamentoCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Command()
    {
        var cmd = new CrearDepartamentoCommand(Nombre: "Test", Codigo: "TEST-001");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Nombre_Empty()
    {
        var cmd = new CrearDepartamentoCommand(Nombre: "", Codigo: "TEST-001");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    [Fact]
    public void Should_Have_Error_When_Codigo_Empty()
    {
        var cmd = new CrearDepartamentoCommand(Nombre: "Test", Codigo: "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }
}
