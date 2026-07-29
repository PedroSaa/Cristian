using System.Threading.Channels;
using DocFlow.Application.Admin.Respaldos.Commands.RestoreRespaldo;
using DocFlow.Application.Admin.Auditoria.Interfaces;
using DocFlow.Application.Admin.Auditoria.Services;
using DocFlow.Application.Admin.Respaldos.Interfaces;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Documentos.Interfaces;
using DocFlow.Application.Integraciones;
using DocFlow.Application.Reportes.Interfaces;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.RRHH;
using DocFlow.Infrastructure.Auth;
using DocFlow.Infrastructure.BackgroundJobs;
using DocFlow.Infrastructure.Configuration;
using DocFlow.Infrastructure.FileStorage;
using DocFlow.Infrastructure.Repositories;
using DocFlow.Infrastructure.Services;
using DocFlow.Infrastructure.Services.Integraciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DocFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();

        // Auth — Infrastructure
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISecurityPolicyService, SecurityPolicyService>();
        services.AddSingleton<IMfaSecretProtector, MfaSecretProtector>();
        services.AddSingleton<IFirmaClaveProtector, FirmaClaveProtector>();
        services.AddSingleton<IIntegracionConfigService, IntegracionConfigService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ISePersonalRepository, SePersonalRepository>();
        services.AddScoped<ISeUsuariRepository, SeUsuariRepository>();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddHttpContextAccessor();

        // Admin — Infrastructure
        services.AddScoped<IUsuarioAdminRepository, UsuarioAdminRepository>();
        services.AddScoped<IDepartamentoRepository, DepartamentoRepository>();
        services.AddScoped<ICatalogoCategoriaRepository, CatalogoCategoriaRepository>();
        services.AddScoped<ICatalogoSubcategoriaRepository, CatalogoSubcategoriaRepository>();
        services.AddScoped<ISeClasegRepository, SeClasegRepository>();
        services.AddScoped<ISeFormaEnvioRepository, SeFormaEnvioRepository>();
        services.AddScoped<ISeTiptarRepository, SeTiptarRepository>();
        services.AddScoped<ISeFordocRepository, SeFordocRepository>();
        services.AddScoped<ISeForplaRepository, SeForplaRepository>();
        services.AddScoped<ISeForplaMedidaRepository, SeForplaMedidaRepository>();
        services.AddScoped<IPlantillaFlujoRepository, PlantillaFlujoRepository>();
        services.AddScoped<IResponsableFlujoNombreResolver, ResponsableFlujoNombreResolver>();
        services.AddScoped<ISeCorforRepository, SeCorforRepository>();
        services.AddScoped<ISeremTipoRepository, SeremTipoRepository>();
        services.AddScoped<ISeremRepository, SeremRepository>();
        services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
        services.AddSingleton<IBrandingLogoStorageService, LocalBrandingLogoStorageService>();
        services.AddScoped<IIntegracionRepository, IntegracionRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IFirmaUsuarioRepository, FirmaUsuarioRepository>();
        services.AddScoped<IRespaldoRepository, RespaldoRepository>();

        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IBandejaRepository, BandejaRepository>();
        services.AddScoped<ICatalogosRepository, CatalogosRepository>();
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        services.AddScoped<IMarcajeRepository, MarcajeRepository>();
        services.AddScoped<IExpedienteRepository, ExpedienteRepository>();
        services.AddScoped<IArchivadorRepository, ArchivadorRepository>();

        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IPdfExportService, PdfExportService>();

        services.AddScoped<IDocumentoOnlineRepository, DocumentoOnlineRepository>();
        services.AddSingleton<IPlantillaResolver, PlantillaResolver>();

        services.AddSingleton<IOnlyOfficeJwtService, OnlyOfficeJwtService>();
        services.AddHttpClient<IOnlyOfficeDocumentService, OnlyOfficeDocumentService>();

        // Proveedores — Infrastructure
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IChileProveedorService, ChileProveedorService>();

        // Facturas DTE — Infrastructure
        services.AddScoped<IFacturaRepository, FacturaRepository>();
        services.AddScoped<ISiiDteInboxService, StubSiiDteInboxService>();
        services.AddScoped<IDteXmlParser, StubDteXmlParser>();
        services.AddScoped<IMercadoPublicoMatcher, StubMercadoPublicoMatcher>();

        // OIRS — Infrastructure
        services.AddScoped<IOirsRepository, OirsRepository>();
        services.AddScoped<IOirsNotificationDispatcher, StubOirsNotificationDispatcher>();
        services.AddScoped<IPortalCiudadanoOirsGateway, PortalCiudadanoOirsGateway>();

        // ClaveÚnica — Infrastructure
        services.Configure<Application.Integraciones.ClaveUnicaOptions>(
            configuration.GetSection(Application.Integraciones.ClaveUnicaOptions.SectionName));
        services.AddScoped<IClaveUnicaService, ClaveUnicaService>();

        // DocDigital — Infrastructure
        services.AddScoped<IDocDigitalDocumentoRepository, DocDigitalDocumentoRepository>();
        services.AddSingleton<IDocDigitalAuthService, DocDigitalAuthService>();
        services.AddScoped<IDocDigitalService, DocDigitalService>();
        services.AddSingleton<IPollingDelayProvider>(SystemPollingDelayProvider.Instance);

        services.AddHostedService<DocDigitalPollingService>();

        // Email corporativo — Infrastructure
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IEmailClassificationService, EmailClassificationService>();
        services.AddScoped<StubEmailClientService>();
        services.AddScoped<EmailClientService>();
        services.AddScoped<IEmailClientService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            return options.StubMode
                ? sp.GetRequiredService<StubEmailClientService>()
                : sp.GetRequiredService<EmailClientService>();
        });
        services.AddHostedService<EmailPollingService>();

        // Admin Respaldos — Infrastructure
        services.Configure<BackupSettings>(
            configuration.GetSection(BackupSettings.SectionName));
        services.PostConfigure<BackupSettings>(settings =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            try
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                settings.DatabaseName = builder.Database;
                settings.PgHost = builder.Host;
                if (builder.Port > 0) settings.PgPort = builder.Port;
                settings.PgUsername = builder.Username;
                settings.PgPassword = builder.Password;
            }
            catch
            {
                // Leave any configured value untouched if parsing fails.
            }
        });
        services.AddSingleton(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BackupSettings>>().Value);

        services.AddSingleton<Channel<Guid>>(
            _ => Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }));
        services.AddSingleton<ChannelWriter<Guid>>(sp =>
            sp.GetRequiredService<Channel<Guid>>().Writer);
        services.AddSingleton<ChannelReader<Guid>>(sp =>
            sp.GetRequiredService<Channel<Guid>>().Reader);

        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddScoped<IBackupEngine, BackupEngine>();
        services.AddScoped<IRetentionService, RetentionService>();
        services.AddHostedService<RespaldoBackgroundService>();
        services.AddHostedService<RespaldoSchedulerService>();

        // Admin Respaldos — Restore
        services.AddScoped<IRestoreLogRepository, RestoreLogRepository>();
        services.AddScoped<IRestoreEngine, RestoreEngine>();
        services.AddScoped<IBackupNotificationService, BackupNotificationService>();

        services.AddSingleton<Channel<RestoreRequest>>(
            _ => Channel.CreateUnbounded<RestoreRequest>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }));
        services.AddSingleton<ChannelWriter<RestoreRequest>>(sp =>
            sp.GetRequiredService<Channel<RestoreRequest>>().Writer);
        services.AddSingleton<ChannelReader<RestoreRequest>>(sp =>
            sp.GetRequiredService<Channel<RestoreRequest>>().Reader);

        services.AddHostedService<RestoreBackgroundService>();

        // Integraciones — connection testers
        services.AddHttpClient("integraciones-test");
        services.AddScoped<IIntegracionTester, DocDigitalTester>();
        services.AddScoped<IIntegracionTester, FirmaGobTester>();
        services.AddScoped<IIntegracionTester, SiiTester>();
        services.AddScoped<IIntegracionTester, EmailTester>();

        // Firma Electrónica — Infrastructure
        services.AddScoped<IFirmaGobService, StubFirmaGobService>();
        services.AddScoped<IFirmaTicketRepository, FirmaTicketRepository>();

        // Reportes — Infrastructure
        services.AddScoped<IReporteRepository, ReporteRepository>();
        services.AddScoped<IReportDataContext, Data.ReportDataContext>();
        services.AddScoped<IReporteQueryService, Application.Reportes.Services.ReporteQueryService>();

        services.AddKeyedScoped<IReportGenerator, ExcelReportGenerator>(nameof(FormatoExportacion.Excel));
        services.AddKeyedScoped<IReportGenerator, PdfReportGenerator>(nameof(FormatoExportacion.PDF));
        services.AddKeyedScoped<IReportGenerator, HtmlReportGenerator>(nameof(FormatoExportacion.Pantalla));

        // RRHH — Infrastructure
        services.AddScoped<Domain.Interfaces.RRHH.IPermisoRepository, PermisoRepository>();
        services.AddScoped<IHorasExtraRepository, HorasExtraRepository>();

        // RBAC — Permisos granulares
        services.AddScoped<Domain.Interfaces.IPermisoRepository, RbacPermisoRepository>();
        // Singleton: el cache de permisos (TTL) solo sirve si la instancia vive entre requests.
        // En el miss abre un scope para resolver los repos scoped (patrón SecurityPolicyService).
        services.AddSingleton<IPermissionService, PermissionService>();

        // Auditoría — CSV export
        services.AddScoped<IAuditoriaCsvService, AuditoriaCsvService>();

        // Auditoría — Service (captura IP/UA automáticamente)
        services.AddScoped<IAuditoriaService, AuditoriaService>();

        // Numeración — Contador atómico
        services.AddScoped<ICounterService, CounterService>();

        // Numeración — Plantillas CRUD
        services.AddScoped<IPlantillaService, PlantillaService>();

        // Órdenes de Compra — Infrastructure
        services.AddScoped<Domain.Interfaces.OrdenesCompra.IOrdenCompraRepository, Repositories.OrdenesCompra.OrdenCompraRepository>();
        services.AddScoped<Domain.Interfaces.OrdenesCompra.IOrdenCompraNumeracionService, Services.OrdenesCompra.OrdenCompraNumeracionService>();
        services.AddScoped<Application.OrdenesCompra.Interfaces.IOrdenCompraPdfService, Services.OrdenesCompra.OrdenCompraPdfService>();
        services.AddHttpClient<Application.OrdenesCompra.Interfaces.IMercadoPublicoService, Services.OrdenesCompra.MercadoPublicoService>();

        return services;
    }
}
