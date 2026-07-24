using DocFlow.Application.Admin.Configuracion.Commands.UploadLoginBackground;
using DocFlow.Application.Common.Branding;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Commands.UploadLoginBackground;

public class UploadLoginBackgroundCommandValidatorTests
{
    private readonly UploadLoginBackgroundCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Command()
    {
        var cmd = new UploadLoginBackgroundCommand(TestImages.Png, "login-background.png", "image/png");

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Jpeg()
    {
        var cmd = new UploadLoginBackgroundCommand(TestImages.Jpeg, "login-background.jpeg", "image/jpeg");

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        var cmd = new UploadLoginBackgroundCommand(Array.Empty<byte>(), "login-background.png", "image/png");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("El archivo de fondo de login es obligatorio.");
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Not_Image()
    {
        var cmd = new UploadLoginBackgroundCommand(new byte[] { 0x01 }, "login-background.txt", "text/plain");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void Should_Have_Error_When_File_Is_Svg()
    {
        var cmd = new UploadLoginBackgroundCommand("<svg></svg>"u8.ToArray(), "login-background.svg", "image/svg+xml");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("El fondo de login debe ser una imagen PNG, JPG, JPEG, GIF o WEBP.");
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Type_Is_Spoofed()
    {
        var cmd = new UploadLoginBackgroundCommand("not an image"u8.ToArray(), "login-background.png", "image/png");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("El contenido del fondo de login no coincide con una imagen PNG, JPG, JPEG, GIF o WEBP.");
    }

    [Fact]
    public void Should_Have_Error_When_File_Is_Too_Large()
    {
        var content = new byte[BrandingImageUploadValidation.MaxImageBytes + 1];
        Array.Copy(TestImages.Png, content, TestImages.Png.Length);
        var cmd = new UploadLoginBackgroundCommand(content, "login-background.png", "image/png");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("El fondo de login no puede superar 5 MB.");
    }

    private static class TestImages
    {
        public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];
    }
}
