using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Common.Branding;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Configuracion.Commands.UploadLoginBackground;

public record UploadLoginBackgroundCommand(
    byte[] Content,
    string FileName,
    string ContentType) : IRequest<ConfiguracionDto>;

public class UploadLoginBackgroundCommandValidator : AbstractValidator<UploadLoginBackgroundCommand>
{
    public UploadLoginBackgroundCommandValidator()
    {
        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El archivo de fondo de login es obligatorio.")
            .Must(content => content is { Length: > 0 })
            .WithMessage("El archivo de fondo de login es obligatorio.")
            .Must(content => content.Length <= BrandingImageUploadValidation.MaxImageBytes)
            .WithMessage($"El fondo de login no puede superar {BrandingImageUploadValidation.MaxImageMegabytes} MB.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .Must(BrandingImageUploadValidation.HasAllowedExtension)
            .WithMessage("El fondo de login debe ser una imagen PNG, JPG, JPEG, GIF o WEBP.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("El tipo de contenido es obligatorio.")
            .Must((cmd, contentType) => BrandingImageUploadValidation.HasMatchingContentType(cmd.FileName, contentType))
            .WithMessage("El tipo de contenido del fondo de login no coincide con una imagen PNG, JPG, JPEG, GIF o WEBP.");

        RuleFor(x => x.Content)
            .Must((cmd, content) => BrandingImageUploadValidation.HasMatchingSignature(content, cmd.FileName))
            .WithMessage("El contenido del fondo de login no coincide con una imagen PNG, JPG, JPEG, GIF o WEBP.");
    }
}

public class UploadLoginBackgroundCommandHandler : IRequestHandler<UploadLoginBackgroundCommand, ConfiguracionDto>
{
    private readonly IBrandingLogoStorageService _storage;
    private readonly IConfiguracionRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public UploadLoginBackgroundCommandHandler(
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

    public async Task<ConfiguracionDto> Handle(UploadLoginBackgroundCommand cmd, CancellationToken ct)
    {
        var backgroundPath = await _storage.SaveLoginBackgroundAsync(cmd.Content, cmd.FileName, ct);

        var backgroundMode = await _repo.GetByClaveAsync(BrandingConfigKeys.LoginBackgroundMode);
        if (backgroundMode is null)
        {
            backgroundMode = ConfiguracionSistema.Crear(
                Guid.NewGuid(),
                BrandingConfigKeys.LoginBackgroundMode,
                LoginBackgroundCatalog.ModeImage,
                "Modo del fondo de login");
        }
        else
        {
            backgroundMode.Actualizar(LoginBackgroundCatalog.ModeImage, backgroundMode.Descripcion);
        }

        await _repo.UpsertAsync(backgroundMode);

        var existing = await _repo.GetByClaveAsync(BrandingConfigKeys.LoginBackgroundUrl);
        ConfiguracionSistema config;
        if (existing is null)
        {
            config = ConfiguracionSistema.Crear(
                Guid.NewGuid(),
                BrandingConfigKeys.LoginBackgroundUrl,
                backgroundPath,
                "URL del fondo de login");
        }
        else
        {
            existing.Actualizar(backgroundPath, existing.Descripcion);
            config = existing;
        }

        await _repo.UpsertAsync(config);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "UploadLoginBackground",
            "ConfiguracionSistema",
            config.Id.ToString(),
            $"Fondo de login actualizado: {backgroundPath}"));

        return new ConfiguracionDto(config.Id, config.Clave, config.Valor, config.Descripcion, config.ActualizadoEn);
    }
}
