using DocFlow.Application.Numeracion.Commands.CreateCounter;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Numeracion.Validators;

public class CreateCounterValidatorTests
{
    private readonly CreateCounterValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenAllFieldsValid()
    {
        var cmd = new CreateCounterCommand("DOC", "ORG001", 0, "", "", "ANUAL", 0);

        var result = _validator.TestValidate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldFail_WhenCodigoContadorIsEmpty()
    {
        var cmd = new CreateCounterCommand("", "ORG001");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.CodigoContador);
    }

    [Fact]
    public void ShouldFail_WhenOrgDepCodIsEmpty()
    {
        var cmd = new CreateCounterCommand("DOC", "");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.OrgDepCod);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVALIDA")]
    [InlineData("SEMESTRAL")]
    public void ShouldFail_WhenPeriodicidadIsInvalid(string periodicidad)
    {
        var cmd = new CreateCounterCommand("DOC", "ORG001", 0, "", "", periodicidad, 0);

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Periodicidad);
    }

    [Theory]
    [InlineData("CONTINUO")]
    [InlineData("ANUAL")]
    [InlineData("MENSUAL")]
    public void ShouldPass_WhenPeriodicidadIsValid(string periodicidad)
    {
        var cmd = new CreateCounterCommand("DOC", "ORG001", 0, "", "", periodicidad, 0);

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Periodicidad);
    }

    [Theory]
    [InlineData("anual")]
    [InlineData("Mensual")]
    [InlineData(" continuo ")]
    public void ShouldPass_WhenPeriodicidadIsValid_CaseInsensitive(string periodicidad)
    {
        var cmd = new CreateCounterCommand("DOC", "ORG001", 0, "", "", periodicidad, 0);

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Periodicidad);
    }

    [Fact]
    public void ShouldFail_WhenValorInicialIsNegative()
    {
        var cmd = new CreateCounterCommand("DOC", "ORG001", 0, "", "", "CONTINUO", -1);

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ValorInicial);
    }

    [Fact]
    public void ShouldPass_WhenValorInicialIsZero()
    {
        var cmd = new CreateCounterCommand("DOC", "ORG001", 0, "", "", "CONTINUO", 0);

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ValorInicial);
    }

    [Fact]
    public void ShouldFail_WhenCodigoContadorExceedsMaxLength()
    {
        var cmd = new CreateCounterCommand(new string('X', 51), "ORG001");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.CodigoContador);
    }

    [Fact]
    public void ShouldFail_WhenOrgDepCodExceedsMaxLength()
    {
        var cmd = new CreateCounterCommand("DOC", new string('X', 21));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.OrgDepCod);
    }
}
