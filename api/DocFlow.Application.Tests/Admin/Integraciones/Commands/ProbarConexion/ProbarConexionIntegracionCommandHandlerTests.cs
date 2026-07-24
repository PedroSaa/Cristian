using DocFlow.Application.Admin.Integraciones.Commands.ProbarConexion;
using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Integraciones.Commands.ProbarConexion;

// Hand-rolled fake so no HttpClient plumbing is needed
public class FakeIntegracionTester : IIntegracionTester
{
    public TipoIntegracion Tipo { get; init; }
    public ConexionTestResult ResultToReturn { get; init; } = new(true, "OK", 42);
    public int DelayMs { get; init; }
    public bool WasCalled { get; private set; }

    public async Task<ConexionTestResult> TestAsync(ConfiguracionIntegracion config, CancellationToken ct)
    {
        WasCalled = true;
        if (DelayMs > 0)
            await Task.Delay(DelayMs, ct); // slower than the handler timeout → its linked CTS fires
        ct.ThrowIfCancellationRequested();
        return ResultToReturn;
    }
}

public class ProbarConexionIntegracionCommandHandlerTests
{
    private readonly Mock<IIntegracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ProbarConexionIntegracionCommandHandler>> _loggerMock = new();

    private static IConfiguration BuildConfig(int timeoutSeconds = 10)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Integraciones:TestTimeoutSeconds"] = timeoutSeconds.ToString()
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private ProbarConexionIntegracionCommandHandler BuildHandler(
        IIntegracionTester? tester = null,
        IConfiguration? config = null)
    {
        var testers = tester is null
            ? Enumerable.Empty<IIntegracionTester>()
            : new[] { tester };

        return new ProbarConexionIntegracionCommandHandler(
            _repoMock.Object,
            testers,
            config ?? BuildConfig(),
            _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Dispatch_To_Tester_And_Map_Result()
    {
        // Arrange
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "https://api.docdigital.cl", "sk-secret", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);

        var fake = new FakeIntegracionTester
        {
            Tipo = TipoIntegracion.DocDigital,
            ResultToReturn = new ConexionTestResult(true, "Servidor alcanzable (HTTP 200).", 123)
        };

        var handler = BuildHandler(tester: fake);

        // Act
        var result = await handler.Handle(new ProbarConexionIntegracionCommand(id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Mensaje.Should().Contain("200");
        result.LatencyMs.Should().Be(123);
        fake.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_NoSoportado_When_No_Tester_Registered()
    {
        // Arrange
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "ChileProveedor", TipoIntegracion.ChileProveedor,
            "https://api.chileproveedor.cl", "key", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);

        // No tester registered for ChileProveedor
        var handler = BuildHandler(tester: null);

        // Act
        var act = async () => await handler.Handle(new ProbarConexionIntegracionCommand(id), CancellationToken.None);

        // Assert — MUST NOT throw
        var result = await handler.Handle(new ProbarConexionIntegracionCommand(id), CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Mensaje.Should().Contain("no soportada");
        result.LatencyMs.Should().BeNull();
    }

    [Fact]
    public async Task Should_Return_GuardMessage_And_Not_Call_Tester_When_BaseUrl_Empty()
    {
        // Arrange
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "", "sk-secret", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);

        var fake = new FakeIntegracionTester { Tipo = TipoIntegracion.DocDigital };
        var handler = BuildHandler(tester: fake);

        // Act
        var result = await handler.Handle(new ProbarConexionIntegracionCommand(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Mensaje.Should().Contain("URL");
        result.LatencyMs.Should().BeNull();
        fake.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Entity_Not_Found()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ConfiguracionIntegracion?)null);

        var handler = BuildHandler();

        // Act
        var act = async () => await handler.Handle(new ProbarConexionIntegracionCommand(id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Should_Return_Timeout_When_Tester_Exceeds_Configured_Timeout()
    {
        // Arrange
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "SII", TipoIntegracion.SII,
            "https://maullin.sii.cl", "apikey", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);

        // Tester slower than the configured timeout → the handler's OWN linked CTS must fire
        var slowTester = new FakeIntegracionTester { Tipo = TipoIntegracion.SII, DelayMs = 5000 };
        var handler = BuildHandler(tester: slowTester, config: BuildConfig(timeoutSeconds: 0));

        // Act — outer token is NOT cancelled; only the handler's timeout should trigger
        var result = await handler.Handle(new ProbarConexionIntegracionCommand(id), CancellationToken.None);

        // Assert — timeout branch (not the client-cancelled branch)
        result.Success.Should().BeFalse();
        result.Mensaje.Should().Contain("límite");
        result.LatencyMs.Should().BeNull();
        slowTester.WasCalled.Should().BeTrue();
    }
}
