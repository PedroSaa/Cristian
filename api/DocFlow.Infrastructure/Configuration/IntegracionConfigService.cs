using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocFlow.Infrastructure.Configuration;

/// <summary>
/// Singleton cache-aside (mismo patrón que SecurityPolicyService) que resuelve la config de
/// integraciones leyendo de configuraciones_integracion (BD) con fallback a appsettings.
/// Cachea un objeto agregado por integración para que cada miss sea una sola lectura a la BD
/// y la invalidación sea atómica.
/// </summary>
public sealed class IntegracionConfigService : IIntegracionConfigService
{
    private const string CachePrefix = "IntegracionConfig:";
    private const string DocDigitalNombre = "DocDigital";
    private const string OnlyOfficeNombre = "OnlyOffice";
    private const string MercadoPublicoNombre = "MercadoPublico";
    private const string MercadoPublicoDefaultBaseUrl = "https://api.mercadopublico.cl";

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public IntegracionConfigService(
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public string GetDocDigitalBaseUrl() => GetDocDigitalConfig().BaseUrl;
    public int GetDocDigitalPollingIntervalMinutes() => GetDocDigitalConfig().PollingIntervalMinutes;
    public string GetDocDigitalSystemUserEmail() => GetDocDigitalConfig().SystemUserEmail;

    public string GetOnlyOfficeDocumentServerUrl() => GetOnlyOfficeConfig().DocumentServerUrl;
    public string GetOnlyOfficeCallbackUrl() => GetOnlyOfficeConfig().CallbackUrl;
    public string GetOnlyOfficeBackendInternalUrl() => GetOnlyOfficeConfig().BackendInternalUrl;

    public string GetMercadoPublicoTicket() => GetMercadoPublicoConfig().Ticket;
    public string GetMercadoPublicoCodigoOrganismo() => GetMercadoPublicoConfig().CodigoOrganismo;
    public string GetMercadoPublicoBaseUrl() => GetMercadoPublicoConfig().BaseUrl;

    public void Invalidate(string nombreIntegracion)
        => _cache.Remove($"{CachePrefix}{nombreIntegracion}");

    private DocDigitalRuntimeConfig GetDocDigitalConfig()
    {
        var cacheKey = $"{CachePrefix}{DocDigitalNombre}";
        if (_cache.TryGetValue(cacheKey, out DocDigitalRuntimeConfig? cached) && cached is not null)
            return cached;

        var row = FetchRow(DocDigitalNombre);

        var baseUrl = Resolve(
            row?.BaseUrl,
            _configuration["DocDigital:BaseUrl"],
            "");
        var systemUserEmail = Resolve(
            row?.GetSetting(IntegracionSettingsKeys.SystemUserEmail),
            _configuration["DocDigital:SystemUserEmail"],
            "");
        var intervalRaw = Resolve(
            row?.GetSetting(IntegracionSettingsKeys.PollingIntervalMinutes),
            _configuration["DocDigital:PollingIntervalMinutes"],
            "15");
        var interval = int.TryParse(intervalRaw, out var n) && n > 0 ? n : 15;

        var config = new DocDigitalRuntimeConfig(baseUrl, interval, systemUserEmail);
        _cache.Set(cacheKey, config);
        return config;
    }

    private OnlyOfficeRuntimeConfig GetOnlyOfficeConfig()
    {
        var cacheKey = $"{CachePrefix}{OnlyOfficeNombre}";
        if (_cache.TryGetValue(cacheKey, out OnlyOfficeRuntimeConfig? cached) && cached is not null)
            return cached;

        var row = FetchRow(OnlyOfficeNombre);

        var documentServerUrl = Resolve(
            row?.BaseUrl,
            _configuration["OnlyOffice:DocumentServerUrl"],
            "");
        var callbackUrl = Resolve(
            row?.GetSetting(IntegracionSettingsKeys.CallbackUrl),
            _configuration["OnlyOffice:CallbackUrl"],
            "");
        var backendInternalUrl = Resolve(
            row?.GetSetting(IntegracionSettingsKeys.BackendInternalUrl),
            _configuration["OnlyOffice:BackendInternalUrl"],
            "");

        var config = new OnlyOfficeRuntimeConfig(documentServerUrl, callbackUrl, backendInternalUrl);
        _cache.Set(cacheKey, config);
        return config;
    }

    private MercadoPublicoRuntimeConfig GetMercadoPublicoConfig()
    {
        var cacheKey = $"{CachePrefix}{MercadoPublicoNombre}";
        if (_cache.TryGetValue(cacheKey, out MercadoPublicoRuntimeConfig? cached) && cached is not null)
            return cached;

        var row = FetchRow(MercadoPublicoNombre);

        var ticket = Resolve(
            row?.GetSetting(IntegracionSettingsKeys.Ticket),
            _configuration["MercadoPublico:Ticket"],
            "");
        var codigoOrganismo = Resolve(
            row?.GetSetting(IntegracionSettingsKeys.CodigoOrganismo),
            _configuration["MercadoPublico:CodigoOrganismo"],
            "");
        var baseUrl = Resolve(
            row?.BaseUrl,
            _configuration["MercadoPublico:BaseUrl"],
            MercadoPublicoDefaultBaseUrl);

        var config = new MercadoPublicoRuntimeConfig(ticket, codigoOrganismo, baseUrl);
        _cache.Set(cacheKey, config);
        return config;
    }

    /// <summary>BD (si no vacío) → appsettings (si no vacío) → fallback.</summary>
    private static string Resolve(string? dbValue, string? appsettingsValue, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(dbValue)) return dbValue;
        if (!string.IsNullOrWhiteSpace(appsettingsValue)) return appsettingsValue;
        return fallback;
    }

    private ConfiguracionIntegracion? FetchRow(string nombre)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IIntegracionRepository>();
        return repo.GetByNombreAsync(nombre).GetAwaiter().GetResult();
    }

    private sealed record DocDigitalRuntimeConfig(string BaseUrl, int PollingIntervalMinutes, string SystemUserEmail);

    private sealed record OnlyOfficeRuntimeConfig(string DocumentServerUrl, string CallbackUrl, string BackendInternalUrl);

    private sealed record MercadoPublicoRuntimeConfig(string Ticket, string CodigoOrganismo, string BaseUrl);
}
