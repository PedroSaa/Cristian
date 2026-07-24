using System.Net;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Infrastructure.Services.OrdenesCompra;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Services.OrdenesCompra;

public class MercadoPublicoServiceTests
{
    // Trimmed real payload captured from the public API (codigo=1123-109-SE13).
    private const string FixtureOrdenEncontrada = """
        {"Cantidad":1,"FechaCreacion":"2026-07-03T12:11:50.9684285Z","Version":"v1","Listado":[{
            "Codigo":"1123-109-SE13",
            "Nombre":"Mantención Áreas verdes Junio 2013",
            "CodigoEstado":6,
            "Estado":"Aceptada",
            "Descripcion":"Servicio mantención de áreas verdes.",
            "TipoMoneda":"CLP",
            "Fechas":{"FechaCreacion":"2013-07-05T12:59:15.443","FechaEnvio":"2013-07-05T15:42:43.223","FechaCancelacion":null},
            "TotalNeto":93200.0,"PorcentajeIva":19.0,"Impuestos":17708.0,"Total":110908.0,
            "Comprador":{"CodigoOrganismo":"6937","NombreOrganismo":"INSTITUTO DE DESARROLLO AGROPECUARIO","RutUnidad":"61.307.000-1","NombreUnidad":"Unidad de Compra"},
            "Proveedor":{"Codigo":"191496","Nombre":"MARGOT DEL ROSARIO NÚÑEZ SILVA","RutSucursal":"7.445.387-2","Comuna":"Calama"},
            "Items":{"Cantidad":2,"Listado":[
                {"Correlativo":1,"Producto":"Servicios de sembradío o mantenimiento de jardines","EspecificacionComprador":"Corte de pasto","Cantidad":1.0,"Unidad":null,"Moneda":"CLP","PrecioNeto":46200.0,"Total":46200.0},
                {"Correlativo":2,"Producto":"Servicios de sembradío o mantenimiento de jardines","EspecificacionComprador":"Cambio de tubería","Cantidad":1.0,"Unidad":null,"Moneda":"CLP","PrecioNeto":47000.0,"Total":47000.0}
            ]}
        }]}
        """;

    private const string FixtureSinResultados =
        """{"Cantidad":0,"FechaCreacion":"2026-07-03T08:12:19.2340084-04:00","Version":"v1","Listado":[]}""";

    private readonly Mock<HttpMessageHandler> _handler = new(MockBehavior.Loose);
    private readonly Mock<IIntegracionConfigService> _config = new();

    private MercadoPublicoService CreateSut(string? ticket = "TICKET-DE-PRUEBA")
    {
        _config.Setup(x => x.GetMercadoPublicoTicket()).Returns(ticket ?? string.Empty);
        return new MercadoPublicoService(
            new HttpClient(_handler.Object),
            _config.Object,
            NullLogger<MercadoPublicoService>.Instance);
    }

    private void SetupResponse(HttpStatusCode status, string json)
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(json),
            });
    }

    [Fact]
    public async Task BuscarPorCodigo_WithRealPortalShape_MapsDtoDefensively()
    {
        SetupResponse(HttpStatusCode.OK, FixtureOrdenEncontrada);
        var sut = CreateSut();

        var result = await sut.BuscarPorCodigoAsync("1123-109-SE13");

        result.Should().NotBeNull();
        result!.Codigo.Should().Be("1123-109-SE13");
        result.Nombre.Should().Be("Mantención Áreas verdes Junio 2013");
        result.Estado.Should().Be("Aceptada");
        result.FechaCreacion.Should().Be("2013-07-05T12:59:15.443");
        result.CompradorNombre.Should().Be("INSTITUTO DE DESARROLLO AGROPECUARIO");
        result.CompradorRut.Should().Be("61.307.000-1");
        result.ProveedorNombre.Should().Be("MARGOT DEL ROSARIO NÚÑEZ SILVA");
        result.ProveedorRut.Should().Be("7.445.387-2");
        result.MontoTotal.Should().Be(110908m);
        result.Items.Should().HaveCount(2);
        result.Items[0].Descripcion.Should().Contain("Servicios de sembradío");
        result.Items[0].Cantidad.Should().Be(1m);
        result.Items[0].PrecioUnitario.Should().Be(46200m);
    }

    [Fact]
    public async Task BuscarPorCodigo_WhenCantidadZero_ReturnsNull()
    {
        SetupResponse(HttpStatusCode.OK, FixtureSinResultados);
        var sut = CreateSut();

        var result = await sut.BuscarPorCodigoAsync("1123-99999-SE13");

        result.Should().BeNull();
    }

    [Fact]
    public async Task BuscarPorCodigo_ShouldUseConfiguredBaseUrl_FromIntegraciones()
    {
        // La URL base viene de Admin → Integraciones (card MercadoPublico), no hardcodeada.
        HttpRequestMessage? captured = null;
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(FixtureSinResultados),
            });
        _config.Setup(x => x.GetMercadoPublicoBaseUrl()).Returns("https://portal-alternativo.cl/");
        var sut = CreateSut();

        await sut.BuscarPorCodigoAsync("1123-109-SE13");

        captured.Should().NotBeNull();
        captured!.RequestUri!.ToString().Should()
            .StartWith("https://portal-alternativo.cl/servicios/v1/publico/ordenesdecompra.json?");
    }

    [Fact]
    public async Task BuscarPorCodigo_ShouldFallbackToDefaultBaseUrl_WhenConfigEmpty()
    {
        HttpRequestMessage? captured = null;
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(FixtureSinResultados),
            });
        _config.Setup(x => x.GetMercadoPublicoBaseUrl()).Returns(string.Empty);
        var sut = CreateSut();

        await sut.BuscarPorCodigoAsync("1123-109-SE13");

        captured!.RequestUri!.ToString().Should()
            .StartWith("https://api.mercadopublico.cl/servicios/v1/publico/ordenesdecompra.json?");
    }

    [Fact]
    public async Task BuscarPorCodigo_WhenTicketMissing_ThrowsWithoutCallingPortal()
    {
        var sut = CreateSut(ticket: null);

        var act = () => sut.BuscarPorCodigoAsync("1123-109-SE13");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _handler.Protected().Verify(
            "SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task BuscarPorCodigo_WhenPortal500WithGenericError_Throws()
    {
        // Real shape: the portal wraps failures as {"Codigo":<n>,"Mensaje":"..."}.
        SetupResponse(HttpStatusCode.InternalServerError,
            """{"Codigo":10500,"Mensaje":"Lo sentimos. Hemos detectado que existen peticiones simultáneas."}""");
        var sut = CreateSut();

        var act = () => sut.BuscarPorCodigoAsync("1123-109-SE13");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BuscarPorCodigo_WhenPortalRejectsMalformedCodigo_ReturnsNull()
    {
        // Real behaviour: HTTP 500 + Codigo 10300 ("Los parámetros no son válidos") when the
        // codigo does not match the portal format — for the user that means "not found".
        SetupResponse(HttpStatusCode.InternalServerError,
            """{"Codigo":10300,"Mensaje":"Los parámetros no son válidos."}""");
        var sut = CreateSut();

        var result = await sut.BuscarPorCodigoAsync("NO-EXISTE-123");

        result.Should().BeNull();
    }

    [Fact]
    public async Task BuscarPorCodigo_WhenTicketInvalid_Throws()
    {
        // Real behaviour: HTTP 203 (a success-range code!) + {"Codigo":203,"Mensaje":"Ticket no válido."}.
        SetupResponse((HttpStatusCode)203, """{"Codigo":203,"Mensaje":"Ticket no válido."}""");
        var sut = CreateSut();

        var act = () => sut.BuscarPorCodigoAsync("1123-109-SE13");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ticket*");
    }

    [Fact]
    public async Task BuscarPorCodigo_WhenBodyIsNotJson_Throws()
    {
        SetupResponse(HttpStatusCode.OK, "<html>gateway error</html>");
        var sut = CreateSut();

        var act = () => sut.BuscarPorCodigoAsync("1123-109-SE13");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuscarPorCodigo_WhenCodigoEmpty_ThrowsArgumentException(string? codigo)
    {
        var sut = CreateSut();

        var act = () => sut.BuscarPorCodigoAsync(codigo!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BuscarPorCodigo_WhenNetworkFails_ThrowsInvalidOperation()
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));
        var sut = CreateSut();

        var act = () => sut.BuscarPorCodigoAsync("1123-109-SE13");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
