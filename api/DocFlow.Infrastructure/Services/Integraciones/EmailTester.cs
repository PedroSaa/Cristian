using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;

namespace DocFlow.Infrastructure.Services.Integraciones;

/// <summary>
/// Placeholder tester for Email connectors.
/// HTTP probing is not applicable for Email (SMTP/IMAP) — returns a clear not-supported message
/// without making any network call.
/// </summary>
public class EmailTester : IIntegracionTester
{
    public TipoIntegracion Tipo => TipoIntegracion.Email;

    public Task<ConexionTestResult> TestAsync(ConfiguracionIntegracion config, CancellationToken ct)
    {
        return Task.FromResult(new ConexionTestResult(
            false,
            "La prueba HTTP no está soportada para Email (requiere SMTP/IMAP).",
            null));
    }
}
