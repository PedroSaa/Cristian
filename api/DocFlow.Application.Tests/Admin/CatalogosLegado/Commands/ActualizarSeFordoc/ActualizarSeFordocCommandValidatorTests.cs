using DocFlow.Application.Admin.CatalogosLegado.Commands.ActualizarSeFordoc;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Commands.ActualizarSeFordoc;

public class ActualizarSeFordocCommandValidatorTests
{
    private readonly ActualizarSeFordocCommandValidator _validator = new();

    private static ActualizarSeFordocCommand BaseValid(int corrN = 0, string tipoDesc = "Formato de prueba", short tipoRec = 1, short tipoInt = 1)
        => new(
            TipoCod: 1,
            TipoRec: tipoRec,
            TipoInt: tipoInt,
            TipoDesc: tipoDesc,
            CorrN: corrN,
            TipoEnv: null,
            SeFordocVistaI: 0,
            SeFordocVistaE: 0,
            SeFordocVistaR: 0,
            SeFordocFormatoNum: null);

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Command()
    {
        var result = _validator.TestValidate(BaseValid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Errors_When_TipoRec_And_TipoInt_Are_Zero()
    {
        // Legacy rule: TipoRec/TipoInt default to 0 when empty; 0 must be accepted.
        var result = _validator.TestValidate(BaseValid(tipoRec: 0, tipoInt: 0));
        result.ShouldNotHaveValidationErrorFor(x => x.TipoRec);
        result.ShouldNotHaveValidationErrorFor(x => x.TipoInt);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_CorrN_Negative()
    {
        var result = _validator.TestValidate(BaseValid(corrN: -1));
        result.ShouldHaveValidationErrorFor(x => x.CorrN);
    }

    [Fact]
    public void Should_Have_Error_When_TipoDesc_Empty()
    {
        var result = _validator.TestValidate(BaseValid(tipoDesc: string.Empty));
        result.ShouldHaveValidationErrorFor(x => x.TipoDesc);
    }

    [Fact]
    public void Should_Have_Error_When_TipoDesc_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(BaseValid(tipoDesc: new string('X', 101)));
        result.ShouldHaveValidationErrorFor(x => x.TipoDesc);
    }
}
