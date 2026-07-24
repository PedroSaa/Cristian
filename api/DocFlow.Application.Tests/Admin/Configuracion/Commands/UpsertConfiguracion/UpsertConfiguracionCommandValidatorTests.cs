using DocFlow.Application.Admin.Configuracion.Commands.UpsertConfiguracion;
using DocFlow.Application.Common.Branding;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Commands.UpsertConfiguracion;

public class UpsertConfiguracionCommandValidatorTests
{
    private readonly UpsertConfiguracionCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Command()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "MAX_INTENTOS", Valor: "5", Descripcion: "Descripción");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Clave_Empty()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "", Valor: "5");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Clave);
    }

    [Fact]
    public void Should_Have_Error_When_Valor_Empty()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "MAX_INTENTOS", Valor: "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor);
    }

    // ---- Security key range validation (Slice 4) ----

    [Fact]
    public void Should_Have_Error_When_Security_Int_Key_Below_Minimum()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "LockoutMaxIntentos", Valor: "0");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El valor debe estar entre 1 y 10 para la clave LockoutMaxIntentos.");
    }

    [Fact]
    public void Should_Have_Error_When_Session_Duration_Is_Below_Minimum()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "JwtExpirationMinutos", Valor: "14");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El valor debe estar entre 15 y 1440 para la clave JwtExpirationMinutos.");
    }

    [Fact]
    public void Should_Have_Error_When_Password_Min_Length_Is_Below_Minimum()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "PasswordMinLength", Valor: "7");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El valor debe estar entre 8 y 32 para la clave PasswordMinLength.");
    }

    [Fact]
    public void Should_Have_Error_When_Totp_Window_Is_Below_Minimum()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "TotpWindowSegundos", Valor: "89");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El valor debe estar entre 90 y 300 para la clave TotpWindowSegundos.");
    }

    [Fact]
    public void Should_Have_Error_When_Security_Int_Key_Above_Maximum()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "JwtExpirationMinutos", Valor: "9999");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El valor debe estar entre 15 y 1440 para la clave JwtExpirationMinutos.");
    }

    [Fact]
    public void Should_Have_Error_When_Security_Int_Key_Not_A_Number()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "PasswordMinLength", Valor: "abc");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El valor debe estar entre 8 y 32 para la clave PasswordMinLength.");
    }

    [Fact]
    public void Should_Have_Error_When_Security_Bool_Key_Not_True_Or_False()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "PasswordRequireUpper", Valor: "yes");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El valor debe ser 'true' o 'false' para la clave PasswordRequireUpper.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Security_Int_Key_Within_Range()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "LockoutMaxIntentos", Valor: "5");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Security_Bool_Key_Valid()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "PasswordRequireUpper", Valor: "true");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Non_Security_Key_Any_Value()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: "NombreInstitucion", Valor: "any-value");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Not_Have_Error_When_LoginBackgroundMode_Is_Approved()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginBackgroundMode, Valor: LoginBackgroundCatalog.ModeColor);
        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Have_Error_When_LoginBackgroundMode_Is_Unsafe()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginBackgroundMode, Valor: "satellite");
        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El modo de fondo de login debe ser image, color o gradient.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_LoginBackgroundPresetKey_Is_Approved()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginBackgroundPresetKey, Valor: "midnight-indigo");
        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Have_Error_When_LoginBackgroundPresetKey_Is_Unknown()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginBackgroundPresetKey, Valor: "sunset-rain");
        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("La clave de preset de fondo de login no es válida.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_LoginBackgroundUrl_Is_A_Branding_Path()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginBackgroundUrl, Valor: "/branding/login-background.png");
        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Not_Have_Error_When_LoginBackgroundUrl_Is_Empty()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginBackgroundUrl, Valor: string.Empty);
        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Theory]
    [InlineData(BrandingConfigKeys.LoginWelcomeTitle)]
    [InlineData(BrandingConfigKeys.LoginWelcomeSubtitle)]
    [InlineData(BrandingConfigKeys.LoginWelcomeHelpText)]
    [InlineData(BrandingConfigKeys.LoginBrandTagline)]
    [InlineData(BrandingConfigKeys.LoginBrandFooterNote)]
    public void Should_Not_Have_Error_When_Login_Welcome_Text_Is_Empty(string key)
    {
        var cmd = new UpsertConfiguracionCommand(Clave: key, Valor: string.Empty);
        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Theory]
    [InlineData(BrandingConfigKeys.LoginWelcomeTitle, 61)]
    [InlineData(BrandingConfigKeys.LoginWelcomeSubtitle, 101)]
    [InlineData(BrandingConfigKeys.LoginWelcomeHelpText, 121)]
    [InlineData(BrandingConfigKeys.LoginBrandTagline, 41)]
    [InlineData(BrandingConfigKeys.LoginBrandFooterNote, 81)]
    public void Should_Have_Error_When_Login_Welcome_Text_Exceeds_Limit(string key, int length)
    {
        var cmd = new UpsertConfiguracionCommand(Clave: key, Valor: new string('a', length));
        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Have_Error_When_LoginBackgroundUrl_Is_External()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginBackgroundUrl, Valor: "https://evil.example.com/login-background.png");
        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("La URL del fondo de login debe apuntar a un archivo en /branding/.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_LoginTemplateKey_Is_Approved()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginTemplateKey, Valor: "split-brand");
        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Have_Error_When_LoginTemplateKey_Is_Unknown()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginTemplateKey, Valor: "split-panel");
        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("La plantilla de login debe ser centered-brand o split-brand.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_LoginSurfaceTone_Is_Approved()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginSurfaceTone, Valor: "dark");
        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Should_Have_Error_When_LoginSurfaceTone_Is_Unknown()
    {
        var cmd = new UpsertConfiguracionCommand(Clave: BrandingConfigKeys.LoginSurfaceTone, Valor: "sepia");
        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Valor)
            .WithErrorMessage("El tono de la superficie de login debe ser light o dark.");
    }
}
