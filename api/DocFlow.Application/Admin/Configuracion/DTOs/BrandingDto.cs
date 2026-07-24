namespace DocFlow.Application.Admin.Configuracion.DTOs;

public record BrandingDto(
    string NombreInstitucion,
    string? LogoUrl,
    string LoginTemplateKey,
    string LoginSurfaceTone,
    string? LoginBackgroundMode,
    string? LoginBackgroundPresetKey,
    string? LoginBackgroundUrl,
    string? LoginWelcomeTitle,
    string? LoginWelcomeSubtitle,
    string? LoginWelcomeHelpText,
    string? LoginBrandTagline,
    string? LoginBrandFooterNote);
