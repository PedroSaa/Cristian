using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using DocFlow.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Configuration;

public sealed class IntegracionConfigServiceTests
{
    private static (IntegracionConfigService Sut, Mock<IIntegracionRepository> Repo) CreateSut(
        Dictionary<string, string?>? appsettings = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());

        var repoMock = new Mock<IIntegracionRepository>(MockBehavior.Strict);

        var innerProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        innerProvider
            .Setup(p => p.GetService(typeof(IIntegracionRepository)))
            .Returns(repoMock.Object);

        var scope = new Mock<IServiceScope>(MockBehavior.Strict);
        scope.Setup(s => s.ServiceProvider).Returns(innerProvider.Object);
        scope.Setup(s => s.Dispose());

        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(appsettings ?? new Dictionary<string, string?>())
            .Build();

        var sut = new IntegracionConfigService(cache, scopeFactory.Object, configuration);
        return (sut, repoMock);
    }

    private static ConfiguracionIntegracion DocDigitalRow(
        string baseUrl = "", Dictionary<string, string>? settings = null)
    {
        var row = ConfiguracionIntegracion.Crear(
            Guid.NewGuid(), "DocDigital", TipoIntegracion.DocDigital, baseUrl, "");
        if (settings is not null)
            row.ActualizarSettings(settings);
        return row;
    }

    [Fact]
    public void GetDocDigitalSystemUserEmail_Should_Return_DbValue_When_Present()
    {
        var (sut, repo) = CreateSut(new() { ["DocDigital:SystemUserEmail"] = "app@docflow.cl" });
        repo.Setup(r => r.GetByNombreAsync("DocDigital"))
            .ReturnsAsync(DocDigitalRow(settings: new() { ["SystemUserEmail"] = "db@docflow.cl" }));

        sut.GetDocDigitalSystemUserEmail().Should().Be("db@docflow.cl");
    }

    [Fact]
    public void GetDocDigitalSystemUserEmail_Should_Fallback_To_Appsettings_When_Db_Empty()
    {
        var (sut, repo) = CreateSut(new() { ["DocDigital:SystemUserEmail"] = "app@docflow.cl" });
        repo.Setup(r => r.GetByNombreAsync("DocDigital")).ReturnsAsync(DocDigitalRow());

        sut.GetDocDigitalSystemUserEmail().Should().Be("app@docflow.cl");
    }

    [Fact]
    public void GetDocDigitalSystemUserEmail_Should_Return_Empty_When_Missing_Everywhere()
    {
        var (sut, repo) = CreateSut();
        repo.Setup(r => r.GetByNombreAsync("DocDigital")).ReturnsAsync((ConfiguracionIntegracion?)null);

        sut.GetDocDigitalSystemUserEmail().Should().BeEmpty();
    }

    [Fact]
    public void GetDocDigitalPollingIntervalMinutes_Should_Return_DbValue_When_Present()
    {
        var (sut, repo) = CreateSut(new() { ["DocDigital:PollingIntervalMinutes"] = "15" });
        repo.Setup(r => r.GetByNombreAsync("DocDigital"))
            .ReturnsAsync(DocDigitalRow(settings: new() { ["PollingIntervalMinutes"] = "20" }));

        sut.GetDocDigitalPollingIntervalMinutes().Should().Be(20);
    }

    [Fact]
    public void GetDocDigitalPollingIntervalMinutes_Should_Fallback_To_15_When_Missing_Everywhere()
    {
        var (sut, repo) = CreateSut();
        repo.Setup(r => r.GetByNombreAsync("DocDigital")).ReturnsAsync((ConfiguracionIntegracion?)null);

        sut.GetDocDigitalPollingIntervalMinutes().Should().Be(15);
    }

    [Fact]
    public void GetDocDigitalBaseUrl_Should_Prefer_Db_Over_Appsettings()
    {
        var (sut, repo) = CreateSut(new() { ["DocDigital:BaseUrl"] = "https://appsettings.example" });
        repo.Setup(r => r.GetByNombreAsync("DocDigital"))
            .ReturnsAsync(DocDigitalRow(baseUrl: "https://db.example"));

        sut.GetDocDigitalBaseUrl().Should().Be("https://db.example");
    }

    [Fact]
    public void Get_Should_Cache_And_Not_Hit_Repository_Twice()
    {
        var (sut, repo) = CreateSut(new() { ["DocDigital:SystemUserEmail"] = "app@docflow.cl" });
        repo.Setup(r => r.GetByNombreAsync("DocDigital")).ReturnsAsync(DocDigitalRow());

        _ = sut.GetDocDigitalSystemUserEmail();
        _ = sut.GetDocDigitalPollingIntervalMinutes();
        _ = sut.GetDocDigitalBaseUrl();

        repo.Verify(r => r.GetByNombreAsync("DocDigital"), Times.Once);
    }

    private static ConfiguracionIntegracion OnlyOfficeRow(
        string baseUrl = "", Dictionary<string, string>? settings = null)
    {
        var row = ConfiguracionIntegracion.Crear(
            Guid.NewGuid(), "OnlyOffice", TipoIntegracion.OnlyOffice, baseUrl, "");
        if (settings is not null)
            row.ActualizarSettings(settings);
        return row;
    }

    [Fact]
    public void GetOnlyOfficeDocumentServerUrl_Should_Prefer_Db_Over_Appsettings()
    {
        var (sut, repo) = CreateSut(new() { ["OnlyOffice:DocumentServerUrl"] = "http://appsettings:8080" });
        repo.Setup(r => r.GetByNombreAsync("OnlyOffice"))
            .ReturnsAsync(OnlyOfficeRow(baseUrl: "http://db:8080"));

        sut.GetOnlyOfficeDocumentServerUrl().Should().Be("http://db:8080");
    }

    [Fact]
    public void GetOnlyOfficeBackendInternalUrl_Should_Return_DbSetting_When_Present()
    {
        var (sut, repo) = CreateSut(new() { ["OnlyOffice:BackendInternalUrl"] = "http://app:5000" });
        repo.Setup(r => r.GetByNombreAsync("OnlyOffice"))
            .ReturnsAsync(OnlyOfficeRow(settings: new() { ["BackendInternalUrl"] = "http://db:5000" }));

        sut.GetOnlyOfficeBackendInternalUrl().Should().Be("http://db:5000");
    }

    [Fact]
    public void GetOnlyOfficeCallbackUrl_Should_Fallback_To_Appsettings_When_Db_Empty()
    {
        var (sut, repo) = CreateSut(new() { ["OnlyOffice:CallbackUrl"] = "http://app/callback" });
        repo.Setup(r => r.GetByNombreAsync("OnlyOffice")).ReturnsAsync(OnlyOfficeRow());

        sut.GetOnlyOfficeCallbackUrl().Should().Be("http://app/callback");
    }

    [Fact]
    public void GetOnlyOffice_Should_Return_Empty_When_Missing_Everywhere()
    {
        var (sut, repo) = CreateSut();
        repo.Setup(r => r.GetByNombreAsync("OnlyOffice")).ReturnsAsync((ConfiguracionIntegracion?)null);

        sut.GetOnlyOfficeDocumentServerUrl().Should().BeEmpty();
        sut.GetOnlyOfficeBackendInternalUrl().Should().BeEmpty();
    }

    [Fact]
    public void Invalidate_OnlyOffice_Should_Force_Refetch()
    {
        var (sut, repo) = CreateSut();
        repo.SetupSequence(r => r.GetByNombreAsync("OnlyOffice"))
            .ReturnsAsync(OnlyOfficeRow(baseUrl: "http://primero:8080"))
            .ReturnsAsync(OnlyOfficeRow(baseUrl: "http://segundo:8080"));

        sut.GetOnlyOfficeDocumentServerUrl().Should().Be("http://primero:8080");
        sut.Invalidate("OnlyOffice");
        sut.GetOnlyOfficeDocumentServerUrl().Should().Be("http://segundo:8080");
        repo.Verify(r => r.GetByNombreAsync("OnlyOffice"), Times.Exactly(2));
    }

    private static ConfiguracionIntegracion MercadoPublicoRow(
        string baseUrl = "", Dictionary<string, string>? settings = null)
    {
        var row = ConfiguracionIntegracion.Crear(
            Guid.NewGuid(), "MercadoPublico", TipoIntegracion.MercadoPublico, baseUrl, "");
        if (settings is not null)
            row.ActualizarSettings(settings);
        return row;
    }

    [Fact]
    public void GetMercadoPublicoTicket_Should_Return_DbSetting_When_Present()
    {
        var (sut, repo) = CreateSut(new() { ["MercadoPublico:Ticket"] = "APP-TICKET" });
        repo.Setup(r => r.GetByNombreAsync("MercadoPublico"))
            .ReturnsAsync(MercadoPublicoRow(settings: new() { ["Ticket"] = "DB-TICKET" }));

        sut.GetMercadoPublicoTicket().Should().Be("DB-TICKET");
    }

    [Fact]
    public void GetMercadoPublicoTicket_Should_Fallback_To_Appsettings_When_Db_Empty()
    {
        var (sut, repo) = CreateSut(new() { ["MercadoPublico:Ticket"] = "APP-TICKET" });
        repo.Setup(r => r.GetByNombreAsync("MercadoPublico")).ReturnsAsync(MercadoPublicoRow());

        sut.GetMercadoPublicoTicket().Should().Be("APP-TICKET");
    }

    [Fact]
    public void GetMercadoPublico_Should_Return_Empty_When_Missing_Everywhere()
    {
        var (sut, repo) = CreateSut();
        repo.Setup(r => r.GetByNombreAsync("MercadoPublico")).ReturnsAsync((ConfiguracionIntegracion?)null);

        sut.GetMercadoPublicoTicket().Should().BeEmpty();
        sut.GetMercadoPublicoCodigoOrganismo().Should().BeEmpty();
    }

    [Fact]
    public void GetMercadoPublicoBaseUrl_Should_Return_DbValue_When_Present()
    {
        var (sut, repo) = CreateSut(new() { ["MercadoPublico:BaseUrl"] = "https://app.mp.cl" });
        repo.Setup(r => r.GetByNombreAsync("MercadoPublico"))
            .ReturnsAsync(MercadoPublicoRow(baseUrl: "https://db.mp.cl"));

        sut.GetMercadoPublicoBaseUrl().Should().Be("https://db.mp.cl");
    }

    [Fact]
    public void GetMercadoPublicoBaseUrl_Should_Fallback_To_Default_When_Missing_Everywhere()
    {
        var (sut, repo) = CreateSut();
        repo.Setup(r => r.GetByNombreAsync("MercadoPublico")).ReturnsAsync((ConfiguracionIntegracion?)null);

        sut.GetMercadoPublicoBaseUrl().Should().Be("https://api.mercadopublico.cl");
    }

    [Fact]
    public void GetMercadoPublicoCodigoOrganismo_Should_Return_DbSetting_When_Present()
    {
        var (sut, repo) = CreateSut();
        repo.Setup(r => r.GetByNombreAsync("MercadoPublico"))
            .ReturnsAsync(MercadoPublicoRow(settings: new() { ["CodigoOrganismo"] = "6937" }));

        sut.GetMercadoPublicoCodigoOrganismo().Should().Be("6937");
    }

    [Fact]
    public void Invalidate_MercadoPublico_Should_Force_Refetch()
    {
        var (sut, repo) = CreateSut();
        repo.SetupSequence(r => r.GetByNombreAsync("MercadoPublico"))
            .ReturnsAsync(MercadoPublicoRow(settings: new() { ["Ticket"] = "PRIMERO" }))
            .ReturnsAsync(MercadoPublicoRow(settings: new() { ["Ticket"] = "SEGUNDO" }));

        sut.GetMercadoPublicoTicket().Should().Be("PRIMERO");
        sut.Invalidate("MercadoPublico");
        sut.GetMercadoPublicoTicket().Should().Be("SEGUNDO");
        repo.Verify(r => r.GetByNombreAsync("MercadoPublico"), Times.Exactly(2));
    }

    [Fact]
    public void Invalidate_Should_Force_Refetch_From_Db()
    {
        var (sut, repo) = CreateSut();
        repo.SetupSequence(r => r.GetByNombreAsync("DocDigital"))
            .ReturnsAsync(DocDigitalRow(settings: new() { ["SystemUserEmail"] = "primero@docflow.cl" }))
            .ReturnsAsync(DocDigitalRow(settings: new() { ["SystemUserEmail"] = "segundo@docflow.cl" }));

        sut.GetDocDigitalSystemUserEmail().Should().Be("primero@docflow.cl");

        sut.Invalidate("DocDigital");

        sut.GetDocDigitalSystemUserEmail().Should().Be("segundo@docflow.cl");
        repo.Verify(r => r.GetByNombreAsync("DocDigital"), Times.Exactly(2));
    }
}
