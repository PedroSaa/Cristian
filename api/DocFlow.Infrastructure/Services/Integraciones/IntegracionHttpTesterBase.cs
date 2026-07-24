using System.Diagnostics;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;

namespace DocFlow.Infrastructure.Services.Integraciones;

/// <summary>
/// Base class for HTTP-based integration testers.
/// Performs a plain GET to BaseUrl, measures latency, and determines reachability
/// by ADR-1: ANY HTTP response (any status code) means the host was reached → Success=true.
/// Only transport failures (DNS, TLS, timeout) → Success=false.
/// ApiKey is NEVER included in the result message.
/// </summary>
public abstract class IntegracionHttpTesterBase : IIntegracionTester
{
    private readonly IHttpClientFactory _factory;

    protected IntegracionHttpTesterBase(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public abstract TipoIntegracion Tipo { get; }

    public async Task<ConexionTestResult> TestAsync(ConfiguracionIntegracion config, CancellationToken ct)
    {
        var client = _factory.CreateClient("integraciones-test");
        var sw = Stopwatch.StartNew();
        try
        {
            using var resp = await client.GetAsync(
                config.BaseUrl,
                HttpCompletionOption.ResponseHeadersRead,
                ct);
            sw.Stop();

            // ADR-1: any HTTP status = host reachable = success
            var statusCode = (int)resp.StatusCode;
            return new ConexionTestResult(
                true,
                $"Servidor alcanzable (HTTP {statusCode}).",
                (int)sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            return new ConexionTestResult(false, "La conexión excedió el tiempo de espera.", null);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            // ex.Message is safe — no ApiKey was sent in the request
            return new ConexionTestResult(false, $"No se pudo conectar al servidor: {ex.Message}", null);
        }
    }
}
