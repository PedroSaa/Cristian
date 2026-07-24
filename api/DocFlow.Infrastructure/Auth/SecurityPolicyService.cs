using DocFlow.Application.Common;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace DocFlow.Infrastructure.Auth;

/// <summary>
/// Singleton cache-aside runtime reader for security policy configuration.
/// On cache miss, opens a scope to resolve <see cref="IConfiguracionRepository"/>
/// via <see cref="IServiceScopeFactory"/>. Falls back to hardcoded defaults
/// when neither cache nor repository has a value.
/// </summary>
public sealed class SecurityPolicyService : ISecurityPolicyService
{
    private const string CachePrefix = "SecurityPolicy:";

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    public SecurityPolicyService(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public int GetLockoutMaxAttempts()
        => GetInt("LockoutMaxIntentos");

    public int GetLockoutDurationMinutes()
        => GetInt("LockoutDuracionMinutos");

    public int GetJwtExpirationMinutes()
        => GetInt("JwtExpirationMinutos");

    public int GetTotpWindowSeconds()
        => GetInt("TotpWindowSegundos");

    public int GetPasswordMinLength()
        => GetInt("PasswordMinLength");

    public bool GetPasswordRequireUpper()
        => GetBool("PasswordRequireUpper");

    public bool GetPasswordRequireSpecial()
        => GetBool("PasswordRequireSpecial");

    public bool IsMfaRequiredForAdministrators()
        => GetBool("RequireMfaAdministradores");

    public bool IsMfaRequiredForOtherUsers()
        => GetBool("RequireMfaOtrosUsuarios");

    public int GetLoginRateLimitPermitLimit()
        => GetInt("RateLimitLoginPermitLimit");

    public int GetLoginRateLimitWindowSeconds()
        => GetInt("RateLimitLoginWindowSegundos");

    public int GetRefreshTokenExpirationDays()
        => GetInt("RefreshTokenExpirationDias");

    public void Invalidate(string clave)
    {
        _cache.Remove($"{CachePrefix}{clave}");
    }

    private int GetInt(string clave)
    {
        var cacheKey = $"{CachePrefix}{clave}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached is string str)
            return ResolveInt(clave, str, cacheKey);

        var valor = FetchFromDb(clave);

        if (valor is not null)
        {
            _cache.Set(cacheKey, valor);
            return ResolveInt(clave, valor, cacheKey);
        }

        // Fallback to hardcoded default
        var fallback = SecurityKeyDefinitions.Keys[clave].DefaultValue;
        _cache.Set(cacheKey, fallback);
        return ResolveInt(clave, fallback, cacheKey);
    }

    private bool GetBool(string clave)
    {
        var cacheKey = $"{CachePrefix}{clave}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached is string str)
            return ResolveBool(clave, str, cacheKey);

        var valor = FetchFromDb(clave);

        if (valor is not null)
        {
            _cache.Set(cacheKey, valor);
            return ResolveBool(clave, valor, cacheKey);
        }

        // Fallback to hardcoded default
        var fallback = SecurityKeyDefinitions.Keys[clave].DefaultValue;
        _cache.Set(cacheKey, fallback);
        return ResolveBool(clave, fallback, cacheKey);
    }

    private string? FetchFromDb(string clave)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfiguracionRepository>();
        var config = repo.GetByClaveAsync(clave).GetAwaiter().GetResult();
        return config?.Valor;
    }

    private int ResolveInt(string clave, string value, string cacheKey)
    {
        if (!int.TryParse(value, out var parsed))
            return UseDefaultInt(clave, cacheKey);

        return NormalizeInt(clave, parsed);
    }

    private bool ResolveBool(string clave, string value, string cacheKey)
    {
        if (TryParseBool(value, out var parsed))
            return parsed;

        return UseDefaultBool(clave, cacheKey);
    }

    private static bool TryParseBool(string? value, out bool parsed)
    {
        if (bool.TryParse(value, out parsed))
            return true;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "yes":
            case "y":
            case "on":
            case "si":
            case "sí":
                parsed = true;
                return true;
            case "0":
            case "no":
            case "off":
                parsed = false;
                return true;
            default:
                return false;
        }
    }

    private int UseDefaultInt(string clave, string cacheKey)
    {
        var fallback = SecurityKeyDefinitions.Keys[clave].DefaultValue;
        _cache.Set(cacheKey, fallback);
        return NormalizeInt(clave, int.Parse(fallback));
    }

    private bool UseDefaultBool(string clave, string cacheKey)
    {
        var fallback = SecurityKeyDefinitions.Keys[clave].DefaultValue;
        _cache.Set(cacheKey, fallback);
        return bool.Parse(fallback);
    }

    private static int NormalizeInt(string clave, int value)
    {
        var def = SecurityKeyDefinitions.Keys[clave];
        if (def.MinValue.HasValue && value < def.MinValue.Value) return def.MinValue.Value;
        if (def.MaxValue.HasValue && value > def.MaxValue.Value) return def.MaxValue.Value;
        return value;
    }

}
