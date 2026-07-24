using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSerem;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Commands.CrearSerem;

public class CrearSeremCommandValidatorTests
{
    private readonly CrearSeremCommandValidator _validator = new();

    private static CrearSeremCommand BaseValid(
        string? remSector = null,
        string? remComuna = null,
        string? remEmail = null,
        string? remDirec = null)
        => new(
            RemCod: "REM001",
            RemTipo: "ABC",
            RemNomb: "Remitente de prueba",
            RemSector: remSector,
            RemComuna: remComuna,
            RemEmail: remEmail,
            RemDirec: remDirec);

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Command_With_Null_Optionals()
    {
        var result = _validator.TestValidate(BaseValid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Email_Format_Invalid()
    {
        var result = _validator.TestValidate(BaseValid(remEmail: "not-an-email"));
        result.ShouldHaveValidationErrorFor(x => x.RemEmail);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Email_Null()
    {
        var result = _validator.TestValidate(BaseValid(remEmail: null));
        result.ShouldNotHaveValidationErrorFor(x => x.RemEmail);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(BaseValid(remEmail: new string('a', 31) + "@test.cl"));
        result.ShouldHaveValidationErrorFor(x => x.RemEmail);
    }

    [Fact]
    public void Should_Have_Error_When_Sector_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(BaseValid(remSector: new string('X', 21)));
        result.ShouldHaveValidationErrorFor(x => x.RemSector);
    }

    [Fact]
    public void Should_Have_Error_When_Comuna_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(BaseValid(remComuna: new string('X', 19)));
        result.ShouldHaveValidationErrorFor(x => x.RemComuna);
    }

    [Fact]
    public void Should_Have_Error_When_Direccion_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(BaseValid(remDirec: new string('X', 61)));
        result.ShouldHaveValidationErrorFor(x => x.RemDirec);
    }
}
