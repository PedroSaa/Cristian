using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Integraciones.Commands.ActualizarIntegracion;

public record ActualizarIntegracionCommand(
    Guid Id,
    string BaseUrl,
    string? ApiKey,
    bool Activo,
    IReadOnlyDictionary<string, string>? Settings = null
) : IRequest<IntegracionDto>;

public class ActualizarIntegracionCommandValidator : AbstractValidator<ActualizarIntegracionCommand>
{
    public ActualizarIntegracionCommandValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("La URL base es obligatoria.")
            .MaximumLength(500).WithMessage("La URL no puede superar los 500 caracteres.");

        // Validación condicional de settings NO secretos (solo si vienen).
        When(x => HasSetting(x, IntegracionSettingsKeys.SystemUserEmail), () =>
        {
            RuleFor(x => x.Settings![IntegracionSettingsKeys.SystemUserEmail])
                .EmailAddress().WithMessage("El email del usuario de sistema no es válido.")
                .OverridePropertyName(IntegracionSettingsKeys.SystemUserEmail);
        });

        When(x => HasSetting(x, IntegracionSettingsKeys.PollingIntervalMinutes), () =>
        {
            RuleFor(x => x.Settings![IntegracionSettingsKeys.PollingIntervalMinutes])
                .Must(BeIntegerInRange)
                .WithMessage("El intervalo de sondeo debe ser un entero entre 1 y 1440 minutos.")
                .OverridePropertyName(IntegracionSettingsKeys.PollingIntervalMinutes);
        });
    }

    private static bool HasSetting(ActualizarIntegracionCommand cmd, string clave)
        => cmd.Settings is not null
           && cmd.Settings.TryGetValue(clave, out var valor)
           && !string.IsNullOrWhiteSpace(valor);

    private static bool BeIntegerInRange(string valor)
        => int.TryParse(valor, out var n) && n >= 1 && n <= 1440;
}

public class ActualizarIntegracionCommandHandler : IRequestHandler<ActualizarIntegracionCommand, IntegracionDto>
{
    private readonly IIntegracionRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly IIntegracionConfigService _integracionConfig;
    private readonly ILogger<ActualizarIntegracionCommandHandler> _logger;

    public ActualizarIntegracionCommandHandler(
        IIntegracionRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        IIntegracionConfigService integracionConfig,
        ILogger<ActualizarIntegracionCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _integracionConfig = integracionConfig;
        _logger = logger;
    }

    public async Task<IntegracionDto> Handle(ActualizarIntegracionCommand cmd, CancellationToken ct)
    {
        var integracion = await _repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Integración {cmd.Id} no encontrada.");

        // If ApiKey is null or empty, keep the existing one
        var apiKey = string.IsNullOrWhiteSpace(cmd.ApiKey) ? integracion.ApiKey : cmd.ApiKey;

        integracion.Actualizar(integracion.Nombre, cmd.BaseUrl, apiKey, cmd.Activo);
        if (cmd.Settings is not null)
            integracion.ActualizarSettings(cmd.Settings);

        await _repo.UpdateAsync(integracion);

        // El runtime lee esta config cacheada; invalidamos para que tome el nuevo valor ya.
        _integracionConfig.Invalidate(integracion.Nombre);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId, "ActualizarIntegracion", "ConfiguracionIntegracion", integracion.Id.ToString(),
            $"Integración actualizada: {integracion.Nombre}, Activo={integracion.Activo}");
        await _auditoria.AddAsync(registro);

        return new IntegracionDto(
            integracion.Id,
            integracion.Nombre,
            integracion.Tipo.ToString(),
            integracion.BaseUrl,
            MaskApiKey(integracion.ApiKey),
            integracion.Activo,
            integracion.Settings);
    }

    private static string MaskApiKey(string apiKey) =>
        string.IsNullOrWhiteSpace(apiKey) || apiKey.Length <= 4
            ? "****"
            : "****" + apiKey[^4..];
}
