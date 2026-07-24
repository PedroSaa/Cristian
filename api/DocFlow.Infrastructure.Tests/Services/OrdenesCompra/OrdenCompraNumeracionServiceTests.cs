using DocFlow.Domain.Entities.NumeracionesDocumento;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.ValueObjects;
using DocFlow.Infrastructure.Services.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Services.OrdenesCompra;

public class OrdenCompraNumeracionServiceTests
{
    private readonly Mock<ICounterService> _counterMock = new();
    private readonly Mock<IPlantillaService> _plantillaMock = new();
    private readonly OrdenCompraNumeracionService _service;

    public OrdenCompraNumeracionServiceTests()
    {
        _service = new OrdenCompraNumeracionService(_counterMock.Object, _plantillaMock.Object);
    }

    private void SetupSinPlantillaActiva()
        => _plantillaMock.Setup(p => p.ListarAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    private void SetupPlantillaActiva(string patron, int rellenoCeros, string periodicidad = "CONTINUO", int valorInicial = 0)
        => _plantillaMock.Setup(p => p.ListarAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlantillaNumeracion(1, "Orden de Compra", patron,
                periodicidad: periodicidad, rellenoCeros: rellenoCeros, valorInicial: valorInicial)]);

    [Fact]
    public async Task Should_Use_Default_Pattern_With_Padding4_When_No_Active_Template()
    {
        SetupSinPlantillaActiva();
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be($"OC-{DateTime.UtcNow.Year}-0007");
    }

    [Fact]
    public async Task Should_Request_Counter_With_OrdenCompra_Code()
    {
        SetupSinPlantillaActiva();
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _service.ObtenerSiguienteNumeroAsync();

        _counterMock.Verify(c => c.NextValueAsync(
            It.Is<CounterKey>(k => k.CodigoContador == "ORDEN_COMPRA"),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Ignore_Active_Templates_Not_Designated_For_OC()
    {
        // Las plantillas activas del motor numeran documentos; solo aplica a OC una
        // plantilla designada por convención (descripción con "orden").
        _plantillaMock.Setup(p => p.ListarAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlantillaNumeracion(0, "Solo correlativo", "{correlativo}",
                periodicidad: "CONTINUO", rellenoCeros: 0, valorInicial: 0)]);
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be($"OC-{DateTime.UtcNow.Year}-0007");
    }

    [Fact]
    public async Task Should_Use_Active_Template_Pattern_And_Padding()
    {
        SetupPlantillaActiva("ORD/{ano2}/{correlativo}", rellenoCeros: 6, periodicidad: "MENSUAL");
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), "MENSUAL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        var ano2 = DateTime.UtcNow.ToString("yy");
        numero.Should().Be($"ORD/{ano2}/000042");
        _counterMock.Verify(c => c.NextValueAsync(It.IsAny<CounterKey>(), "MENSUAL", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Create_Counter_When_Missing_And_Retry()
    {
        SetupSinPlantillaActiva();
        _counterMock.SetupSequence(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("No se encontró un contador."))
            .ReturnsAsync(1);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be($"OC-{DateTime.UtcNow.Year}-0001");
        _counterMock.Verify(c => c.CreateCounterAsync(
            It.Is<CounterKey>(k => k.CodigoContador == "ORDEN_COMPRA"),
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Survive_Concurrent_Counter_Creation()
    {
        SetupSinPlantillaActiva();
        _counterMock.SetupSequence(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("No se encontró un contador."))
            .ReturnsAsync(3);
        _counterMock.Setup(c => c.CreateCounterAsync(It.IsAny<CounterKey>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Ya existe un contador."));

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be($"OC-{DateTime.UtcNow.Year}-0003");
    }

    [Fact]
    public async Task Should_Replace_Irrelevant_Tokens_With_Empty()
    {
        SetupPlantillaActiva("{tipo}{formato}OC-{correlativo}", rellenoCeros: 0);
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be("OC-5");
    }

    [Fact]
    public async Task Should_Not_Use_Templates_That_Only_Contain_Orden()
    {
        // "Orden de Pago" u "Ordenanza Municipal" no deben secuestrar la numeración de OC:
        // solo aplica una plantilla cuya descripción contenga la frase "orden de compra".
        _plantillaMock.Setup(p => p.ListarAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PlantillaNumeracion(1, "Orden de Pago", "OP-{correlativo}", periodicidad: "CONTINUO", rellenoCeros: 2),
                new PlantillaNumeracion(2, "Ordenanza Municipal", "ORD-{correlativo}", periodicidad: "CONTINUO", rellenoCeros: 2),
            ]);
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be($"OC-{DateTime.UtcNow.Year}-0007");
    }

    [Fact]
    public async Task Should_Pick_OrdenDeCompra_Template_Among_Other_Orden_Templates()
    {
        _plantillaMock.Setup(p => p.ListarAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PlantillaNumeracion(1, "Orden de Pago", "OP-{correlativo}", periodicidad: "CONTINUO", rellenoCeros: 2),
                new PlantillaNumeracion(2, "Orden de Compra", "OCX-{correlativo}", periodicidad: "CONTINUO", rellenoCeros: 3),
            ]);
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be("OCX-007");
    }

    [Fact]
    public async Task Should_Pick_Deterministically_When_Multiple_Candidates()
    {
        // Con varias candidatas se ordena por descripción (ordinal, sin distinción de mayúsculas)
        // para que el resultado no dependa del orden en que las devuelva el motor.
        _plantillaMock.Setup(p => p.ListarAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PlantillaNumeracion(1, "Orden de compra servicios", "S-{correlativo}", periodicidad: "CONTINUO", rellenoCeros: 2),
                new PlantillaNumeracion(2, "Orden de Compra bienes", "B-{correlativo}", periodicidad: "CONTINUO", rellenoCeros: 2),
            ]);
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be("B-07");
    }

    [Fact]
    public async Task Should_Match_Descripcion_With_Irregular_Whitespace()
    {
        _plantillaMock.Setup(p => p.ListarAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlantillaNumeracion(1, "Orden  de   COMPRA interna", "W-{correlativo}",
                periodicidad: "CONTINUO", rellenoCeros: 2)]);
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        numero.Should().Be("W-07");
    }

    [Fact]
    public async Task Should_Support_Date_Tokens()
    {
        SetupPlantillaActiva("OC-{ano}{mes}{dia}-{correlativo}", rellenoCeros: 3);
        _counterMock.Setup(c => c.NextValueAsync(It.IsAny<CounterKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        var numero = await _service.ObtenerSiguienteNumeroAsync();

        var now = DateTime.UtcNow;
        numero.Should().Be($"OC-{now.Year:D4}{now.Month:D2}{now.Day:D2}-012");
    }
}
