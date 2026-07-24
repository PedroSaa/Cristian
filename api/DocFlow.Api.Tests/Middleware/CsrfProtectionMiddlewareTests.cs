using DocFlow.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DocFlow.Api.Tests.Middleware;

public class CsrfProtectionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithNonGetAndNoCustomHeader_Returns400()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var middleware = new CsrfProtectionMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WithNonGetAndXRequestedWith_Proceeds()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithNonGetAndXCsrfToken_Proceeds()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Headers["Cookie"] = "XSRF-TOKEN=some-token";
        httpContext.Request.Headers["X-CSRF-TOKEN"] = "some-token";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithMismatchedCsrfToken_Returns400_AndReissuesFreshCookie()
    {
        // Arrange: cookie presente pero header que NO coincide (cookie vieja/corrupta).
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Headers["Cookie"] = "XSRF-TOKEN=stale-token";
        httpContext.Request.Headers["X-CSRF-TOKEN"] = "otro-valor";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(_ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert: rechaza el pipeline pero SANA la cookie en la misma respuesta 400,
        // con un token nuevo (distinto del viejo) para que el reintento del cliente coincida.
        wasCalled.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(400);

        var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("XSRF-TOKEN=");
        setCookieHeader.Should().NotContain("XSRF-TOKEN=stale-token");
    }

    [Fact]
    public async Task InvokeAsync_WithGetRequest_ProceedsWithoutHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithClaveUnicaCallback_ProceedsWithoutHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/auth/claveunica/callback";
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_GetRequest_OnHttp_Should_SetNonSecureCsrfCookie()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.Should().Contain("XSRF-TOKEN=");
        setCookieHeader.Should().NotContain("secure");
    }

    [Fact]
    public async Task InvokeAsync_WithLoginEndpoint_ProceedsWithoutHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/auth/login";
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithRefreshEndpoint_ProceedsWithoutHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/auth/refresh";
        httpContext.Request.Headers["Content-Type"] = "application/json";
        var wasCalled = false;
        var middleware = new CsrfProtectionMiddleware(ctx =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        wasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }
}
