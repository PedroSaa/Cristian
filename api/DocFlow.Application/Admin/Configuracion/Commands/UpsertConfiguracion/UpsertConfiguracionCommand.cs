using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Common;
using DocFlow.Application.Common.Branding;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Configuracion.Commands.UpsertConfiguracion;

public record UpsertConfiguracionCommand(
    string Clave,
    string Valor,
    string? Descripcion = null
) : IRequest<ConfiguracionDto>;

public class UpsertConfiguracionCommandValidator : AbstractValidator<UpsertConfiguracionCommand>
{
    public UpsertConfiguracionCommandValidator()
    {
        RuleFor(x => x.Clave)
            .NotEmpty().WithMessage("La clave es obligatoria.")
            .MaximumLength(100).WithMessage("La clave no puede superar los 100 caracteres.");

        RuleFor(x => x.Valor)
            .MaximumLength(2000).WithMessage("El valor no puede superar los 2000 caracteres.");

        When(x => x.Clave != BrandingConfigKeys.LoginBackgroundUrl && !IsOptionalBrandingTextKey(x.Clave) && !IsOptionalValueKey(x.Clave), () =>
        {
            RuleFor(x => x.Valor)
                .NotEmpty().WithMessage("El valor es obligatorio.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginWelcomeTitle, () =>
        {
            RuleFor(x => x.Valor)
                .MaximumLength(60)
                .WithMessage("El título de bienvenida no puede superar los 60 caracteres.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginWelcomeSubtitle, () =>
        {
            RuleFor(x => x.Valor)
                .MaximumLength(100)
                .WithMessage("El subtítulo de bienvenida no puede superar los 100 caracteres.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginWelcomeHelpText, () =>
        {
            RuleFor(x => x.Valor)
                .MaximumLength(120)
                .WithMessage("El texto de ayuda no puede superar los 120 caracteres.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginBrandTagline, () =>
        {
            RuleFor(x => x.Valor)
                .MaximumLength(40)
                .WithMessage("El texto sobre el nombre no puede superar los 40 caracteres.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginBrandFooterNote, () =>
        {
            RuleFor(x => x.Valor)
                .MaximumLength(80)
                .WithMessage("La nota al pie no puede superar los 80 caracteres.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginBackgroundMode, () =>
        {
            RuleFor(x => x.Valor)
                .Must(LoginBackgroundCatalog.IsValidMode)
                .WithMessage("El modo de fondo de login debe ser image, color o gradient.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginBackgroundPresetKey, () =>
        {
            RuleFor(x => x.Valor)
                .Must(LoginBackgroundCatalog.IsValidPresetKey)
                .WithMessage("La clave de preset de fondo de login no es válida.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginBackgroundUrl, () =>
        {
            RuleFor(x => x.Valor)
                .Must(LoginBackgroundCatalog.IsValidImagePath)
                .WithMessage("La URL del fondo de login debe apuntar a un archivo en /branding/.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginTemplateKey, () =>
        {
            RuleFor(x => x.Valor)
                .Must(BrandingLoginDesignCatalog.IsValidLoginTemplateKey)
                .WithMessage("La plantilla de login debe ser centered-brand o split-brand.");
        });

        When(x => x.Clave == BrandingConfigKeys.LoginSurfaceTone, () =>
        {
            RuleFor(x => x.Valor)
                .Must(BrandingLoginDesignCatalog.IsValidLoginSurfaceTone)
                .WithMessage("El tono de la superficie de login debe ser light o dark.");
        });

        // Security key range validation — enforced only for known security keys
        When(x => SecurityKeyDefinitions.IsSecurityKey(x.Clave), () =>
        {
            RuleFor(x => x.Valor)
                .Must((cmd, valor) => BeValidForSecurityKey(cmd.Clave, valor))
                .WithMessage(cmd =>
                {
                    var def = SecurityKeyDefinitions.Keys[cmd.Clave];
                    return def.Type switch
                    {
                        SecurityKeyType.Int => $"El valor debe estar entre {def.MinValue} y {def.MaxValue} para la clave {cmd.Clave}.",
                        SecurityKeyType.Bool => $"El valor debe ser 'true' o 'false' para la clave {cmd.Clave}.",
                        _ => $"El valor no es válido para la clave {cmd.Clave}."
                    };
                });
        });
    }

    private static bool BeValidForSecurityKey(string clave, string valor)
    {
        if (!SecurityKeyDefinitions.Keys.TryGetValue(clave, out var def))
            return true; // not a security key — skip (already guarded by When)

        return def.Type switch
        {
            SecurityKeyType.Int => int.TryParse(valor, out var n) && n >= def.MinValue && n <= def.MaxValue,
            SecurityKeyType.Bool => valor is "true" or "false",
            _ => false
        };
    }

    private static bool IsOptionalBrandingTextKey(string clave)
        => clave is BrandingConfigKeys.LoginWelcomeTitle or BrandingConfigKeys.LoginWelcomeSubtitle or BrandingConfigKeys.LoginWelcomeHelpText
            or BrandingConfigKeys.LoginBrandTagline or BrandingConfigKeys.LoginBrandFooterNote;

    // Claves cuyo valor es legítimamente opcional (pueden quedar vacías). EmailSoporte
    // es el correo de soporte mostrado a usuarios; una organización puede no tenerlo.
    private static bool IsOptionalValueKey(string clave)
        => clave is "EmailSoporte";

}

public class UpsertConfiguracionCommandHandler : IRequestHandler<UpsertConfiguracionCommand, ConfiguracionDto>
{
    private readonly IConfiguracionRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ISecurityPolicyService _securityPolicy;
    private readonly ILogger<UpsertConfiguracionCommandHandler> _logger;

    public UpsertConfiguracionCommandHandler(
        IConfiguracionRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ISecurityPolicyService securityPolicy,
        ILogger<UpsertConfiguracionCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _securityPolicy = securityPolicy;
        _logger = logger;
    }

    public async Task<ConfiguracionDto> Handle(UpsertConfiguracionCommand cmd, CancellationToken ct)
    {
        var existing = await _repo.GetByClaveAsync(cmd.Clave);
        var valor = NormalizeValueForStorage(cmd.Clave, cmd.Valor);

        ConfiguracionSistema config;
        if (existing is not null)
        {
            existing.Actualizar(valor, cmd.Descripcion ?? existing.Descripcion);
            config = existing;
        }
        else
        {
            config = ConfiguracionSistema.Crear(Guid.NewGuid(), cmd.Clave, valor, cmd.Descripcion ?? string.Empty);
        }

        await _repo.UpsertAsync(config);

        // Invalidate cache for security keys so auth services pick up the new value immediately
        if (SecurityKeyDefinitions.IsSecurityKey(config.Clave))
            _securityPolicy.Invalidate(config.Clave);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId, "UpsertConfiguracion", "ConfiguracionSistema", config.Id.ToString(),
            $"Configuración actualizada: {config.Clave} = {config.Valor}");
        await _auditoria.AddAsync(registro);

        return new ConfiguracionDto(config.Id, config.Clave, config.Valor, config.Descripcion, config.ActualizadoEn);
    }

    private static string NormalizeValueForStorage(string clave, string valor)
        => clave is BrandingConfigKeys.LoginWelcomeTitle or BrandingConfigKeys.LoginWelcomeSubtitle or BrandingConfigKeys.LoginWelcomeHelpText or BrandingConfigKeys.LoginBrandTagline or BrandingConfigKeys.LoginBrandFooterNote or BrandingConfigKeys.LoginTemplateKey or BrandingConfigKeys.LoginSurfaceTone
            ? valor.Trim()
            : valor;
}
