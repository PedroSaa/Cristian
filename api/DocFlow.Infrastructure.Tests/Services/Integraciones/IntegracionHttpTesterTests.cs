using System.Net;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using DocFlow.Infrastructure.Services.Integraciones;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Services.Integraciones;

public class IntegracionHttpTesterTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Loose);
    private readonly Mock<IHttpClientFactory> _factoryMock = new();

    private HttpClient CreateHttpClient() => new HttpClient(_handlerMock.Object);

    private DocDigitalTester BuildTester()
    {
        _factoryMock
            .Setup(f => f.CreateClient("integraciones-test"))
            .Returns(CreateHttpClient());
        return new DocDigitalTester(_factoryMock.Object);
    }

    private static ConfiguracionIntegracion BuildConfig(
        TipoIntegracion tipo = TipoIntegracion.DocDigital,
        string baseUrl = "https://api.docdigital.cl",
        string apiKey = "SENTINEL-KEY-DO-NOT-EXPOSE")
    {
        return ConfiguracionIntegracion.Crear(Guid.NewGuid(), "DocDigital", tipo, baseUrl, apiKey);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private void SetupHttpException(Exception ex)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(ex);
    }

    [Fact]
    public async Task Should_Return_Success_True_When_Http_200()
    {
        SetupHttpResponse(HttpStatusCode.OK);
        var tester = BuildTester();
        var config = BuildConfig();

        var result = await tester.TestAsync(config, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.LatencyMs.Should().NotBeNull("latency must be populated when the host responds");
        result.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
        result.Mensaje.Should().Contain("200");
    }

    [Fact]
    public async Task Should_Return_Success_True_When_Http_401()
    {
        // ADR-1: any HTTP status = host reachable = success
        SetupHttpResponse(HttpStatusCode.Unauthorized);
        var tester = BuildTester();
        var config = BuildConfig();

        var result = await tester.TestAsync(config, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Mensaje.Should().Contain("401");
    }

    [Fact]
    public async Task Should_Return_Success_True_When_Http_500()
    {
        // ADR-1: even a 500 means the server answered
        SetupHttpResponse(HttpStatusCode.InternalServerError);
        var tester = BuildTester();
        var config = BuildConfig();

        var result = await tester.TestAsync(config, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Mensaje.Should().Contain("500");
    }

    [Fact]
    public async Task Should_Return_Success_False_When_HttpRequestException()
    {
        SetupHttpException(new HttpRequestException("DNS resolution failed"));
        var tester = BuildTester();
        var config = BuildConfig();

        var result = await tester.TestAsync(config, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Mensaje.Should().Contain("No se pudo conectar");
        result.LatencyMs.Should().BeNull();
    }

    [Fact]
    public async Task Should_Return_Success_False_When_TaskCanceledException()
    {
        SetupHttpException(new TaskCanceledException("Request timed out"));
        var tester = BuildTester();
        var config = BuildConfig();

        var result = await tester.TestAsync(config, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Mensaje.Should().NotBeNullOrWhiteSpace();
        result.LatencyMs.Should().BeNull();
    }

    [Fact]
    public async Task Should_Not_Expose_ApiKey_In_Mensaje()
    {
        // The sentinel should NEVER appear in the result message
        SetupHttpResponse(HttpStatusCode.OK);
        var tester = BuildTester();
        var config = BuildConfig(apiKey: "SENTINEL-KEY-DO-NOT-EXPOSE");

        var result = await tester.TestAsync(config, CancellationToken.None);

        result.Mensaje.Should().NotContain("SENTINEL-KEY-DO-NOT-EXPOSE");
    }

    [Fact]
    public async Task EmailTester_Should_Return_NotSupported_Without_HttpCall()
    {
        // EmailTester must NOT call the factory/handler at all
        var emailTester = new EmailTester();
        var config = ConfiguracionIntegracion.Crear(Guid.NewGuid(), "Email", TipoIntegracion.Email,
            "smtp://mail.example.cl", "email-key");

        var result = await emailTester.TestAsync(config, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Mensaje.Should().Contain("SMTP");
        result.LatencyMs.Should().BeNull();

        // Verify the http factory was never touched
        _factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }
}
