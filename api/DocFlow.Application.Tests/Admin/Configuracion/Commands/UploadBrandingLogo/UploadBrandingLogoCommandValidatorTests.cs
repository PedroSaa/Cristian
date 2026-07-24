using DocFlow.Application.Admin.Configuracion.Commands.UploadBrandingLogo;
using DocFlow.Application.Common.Branding;
using FluentValidation.TestHelper;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Commands.UploadBrandingLogo;

public class UploadBrandingLogoCommandValidatorTests
{
    private readonly UploadBrandingLogoCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Png()
    {
        var cmd = new UploadBrandingLogoCommand(TestImages.Png, "logo.png", "image/png");

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Errors_For_Valid_Jpeg()
    {
        var cmd = new UploadBrandingLogoCommand(TestImages.Jpeg, "logo.jpg", "image/jpeg");

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_File_Is_Svg()
    {
        var cmd = new UploadBrandingLogoCommand("<svg></svg>"u8.ToArray(), "logo.svg", "image/svg+xml");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("El logo debe ser una imagen PNG, JPG, JPEG, GIF o WEBP.");
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Type_Is_Spoofed()
    {
        var cmd = new UploadBrandingLogoCommand("not an image"u8.ToArray(), "logo.png", "image/png");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("El contenido del logo no coincide con una imagen PNG, JPG, JPEG, GIF o WEBP.");
    }

    [Fact]
    public void Should_Have_Error_When_File_Is_Too_Large()
    {
        var content = new byte[BrandingImageUploadValidation.MaxImageBytes + 1];
        Array.Copy(TestImages.Png, content, TestImages.Png.Length);
        var cmd = new UploadBrandingLogoCommand(content, "logo.png", "image/png");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("El logo no puede superar 5 MB.");
    }

    private static class TestImages
    {
        public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];
    }
}
