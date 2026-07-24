using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Common.Branding;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Configuracion.Commands.UploadBrandingLogo;

public record UploadBrandingLogoCommand(
    byte[] Content,
    string FileName,
    string ContentType) : IRequest<ConfiguracionDto>;

public class UploadBrandingLogoCommandValidator : AbstractValidator<UploadBrandingLogoCommand>
{
    public UploadBrandingLogoCommandValidator()
    {
        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El archivo de logo es obligatorio.")
            .Must(content => content is { Length: > 0 })
            .WithMessage("El archivo de logo es obligatorio.")
            .Must(content => content.Length <= BrandingImageUploadValidation.MaxImageBytes)
            .WithMessage($"El logo no puede superar {BrandingImageUploadValidation.MaxImageMegabytes} MB.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .Must(BrandingImageUploadValidation.HasAllowedExtension)
            .WithMessage("El logo debe ser una imagen PNG, JPG, JPEG, GIF o WEBP.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("El tipo de contenido es obligatorio.")
            .Must((cmd, contentType) => BrandingImageUploadValidation.HasMatchingContentType(cmd.FileName, contentType))
            .WithMessage("El tipo de contenido del logo no coincide con una imagen PNG, JPG, JPEG, GIF o WEBP.");

        RuleFor(x => x.Content)
            .Must((cmd, content) => BrandingImageUploadValidation.HasMatchingSignature(content, cmd.FileName))
            .WithMessage("El contenido del logo no coincide con una imagen PNG, JPG, JPEG, GIF o WEBP.");
    }
}

public class UploadBrandingLogoCommandHandler : IRequestHandler<UploadBrandingLogoCommand, ConfiguracionDto>
{
    private readonly IBrandingLogoStorageService _storage;
    private readonly IConfiguracionRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public UploadBrandingLogoCommandHandler(
        IBrandingLogoStorageService storage,
        IConfiguracionRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _storage = storage;
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<ConfiguracionDto> Handle(UploadBrandingLogoCommand cmd, CancellationToken ct)
    {
        var logoPath = await _storage.SaveAsync(cmd.Content, cmd.FileName, ct);

        var existing = await _repo.GetByClaveAsync(BrandingConfigKeys.LogoUrl);
        ConfiguracionSistema config;
        if (existing is null)
        {
            config = ConfiguracionSistema.Crear(
                Guid.NewGuid(),
                BrandingConfigKeys.LogoUrl,
                logoPath,
                "URL del logo institucional");
        }
        else
        {
            existing.Actualizar(logoPath, existing.Descripcion);
            config = existing;
        }

        await _repo.UpsertAsync(config);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "UploadBrandingLogo",
            "ConfiguracionSistema",
            config.Id.ToString(),
            $"Logo institucional actualizado: {logoPath}"));

        return new ConfiguracionDto(config.Id, config.Clave, config.Valor, config.Descripcion, config.ActualizadoEn);
    }
}
