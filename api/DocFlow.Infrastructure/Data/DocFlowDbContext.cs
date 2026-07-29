using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.NumeracionesDocumento;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Entities.RRHH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DocFlow.Infrastructure.Data;

public class DocFlowDbContext : DbContext
{
    public DocFlowDbContext(DbContextOptions<DocFlowDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Documento.HasQueryFilter(d => !d.EstaEliminado) causa el warning informativo
        // PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning
        // en entidades hijas con navegación requerida. El filtro es correcto — las
        // relaciones requeridas pueden devolver null cuando el padre está filtrado,
        // y el código ya maneja esa posibilidad (ej: DocumentoRepository usa
        // .IgnoreQueryFilters() para consultas administrativas).
        // Las migraciones legacy se mantienen manuales, así que también se ignora
        // el warning de pending model changes para evitar abortar el arranque.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)
             .Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<ConfiguracionSistema> ConfiguracionesSistema => Set<ConfiguracionSistema>();
    public DbSet<ConfiguracionIntegracion> ConfiguracionesIntegracion => Set<ConfiguracionIntegracion>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<Respaldo> Respaldos => Set<Respaldo>();
    public DbSet<RespaldoConfig> RespaldosConfig => Set<RespaldoConfig>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<SePersonal> SePersonales => Set<SePersonal>();
    public DbSet<SeUsuari> SeUsuaris => Set<SeUsuari>();
    public DbSet<FirmaUsuario> FirmasUsuario => Set<FirmaUsuario>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<CatalogoCategoria> CatalogoCategorias => Set<CatalogoCategoria>();
    public DbSet<CatalogoSubcategoria> CatalogoSubcategorias => Set<CatalogoSubcategoria>();
    public DbSet<SeClaseg> SeClaseg => Set<SeClaseg>();
    public DbSet<SeFormaEnvio> SeFormaEnvios => Set<SeFormaEnvio>();
    public DbSet<SeTiptar> SeTiptar => Set<SeTiptar>();
    public DbSet<SeFordoc> SeFordoc => Set<SeFordoc>();
    public DbSet<SeForpla> SeForpla => Set<SeForpla>();
    public DbSet<SeForplaMedida> SeForplaMedidas => Set<SeForplaMedida>();
    public DbSet<PlantillaFlujoPaso> PlantillaFlujoPasos => Set<PlantillaFlujoPaso>();
    public DbSet<SeCorfor> SeCorfor => Set<SeCorfor>();
    public DbSet<SeremTipo> SeremTipos => Set<SeremTipo>();
    public DbSet<Serem> Serems => Set<Serem>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<TareaRespuesta> TareaRespuestas => Set<TareaRespuesta>();
    public DbSet<Adjunto> Adjuntos => Set<Adjunto>();
    public DbSet<Referencia> Referencias => Set<Referencia>();
    public DbSet<Observacion> Observaciones => Set<Observacion>();
    public DbSet<Contenido> Contenidos => Set<Contenido>();
    public DbSet<DocumentoOnline> DocumentosOnline => Set<DocumentoOnline>();
    public DbSet<VersionDocumentoOnline> VersionesDocumentoOnline => Set<VersionDocumentoOnline>();
    public DbSet<Reporte> Reportes => Set<Reporte>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<DocDigitalDocumento> DocDigitalDocumentos => Set<DocDigitalDocumento>();
    public DbSet<EmailCuenta> EmailCuentas => Set<EmailCuenta>();
    public DbSet<EmailMensaje> EmailMensajes => Set<EmailMensaje>();
    public DbSet<EmailClassificationRule> EmailClassificationRules => Set<EmailClassificationRule>();
    public DbSet<EmailPollingLog> EmailPollingLogs => Set<EmailPollingLog>();
    public DbSet<SolicitudOirs> SolicitudesOirs => Set<SolicitudOirs>();
    public DbSet<FirmaTicket> FirmaTickets => Set<FirmaTicket>();

    // RRHH
    public DbSet<Domain.Entities.RRHH.Permiso> Permisos => Set<Domain.Entities.RRHH.Permiso>();
    public DbSet<HorasExtra> HorasExtras => Set<HorasExtra>();

    // RBAC — Permisos granulares
    public DbSet<Domain.Entities.Permiso> CatalogoPermisos => Set<Domain.Entities.Permiso>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();

    // Marcaje
    public DbSet<Marcaje> Marcajes => Set<Marcaje>();

    // Expedientes
    public DbSet<Expediente> Expedientes => Set<Expediente>();
    public DbSet<ExpedienteDocumento> ExpedienteDocumentos => Set<ExpedienteDocumento>();
    public DbSet<Hito> Hitos => Set<Hito>();
    public DbSet<BitacoraExpediente> BitacoraExpedientes => Set<BitacoraExpediente>();
    public DbSet<TareaExpediente> TareaExpedientes => Set<TareaExpediente>();

    // Archivadores
    public DbSet<Archivador> Archivadores => Set<Archivador>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    // Catálogos
    public DbSet<FormatoDocumento> FormatosDocumento => Set<FormatoDocumento>();
    public DbSet<EstadoCatalogo> EstadosDocumento => Set<EstadoCatalogo>();
    public DbSet<CategoriaDocumento> CategoriasDocumento => Set<CategoriaDocumento>();

    // Numeración
    public DbSet<ContadorNumeracion> ContadoresNumeracion => Set<ContadorNumeracion>();
    public DbSet<PlantillaNumeracion> PlantillasNumeracion => Set<PlantillaNumeracion>();

    // Órdenes de Compra
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraItem> OrdenesCompraItems => Set<OrdenCompraItem>();
    public DbSet<OrdenCompraAdjunto> OrdenesCompraAdjuntos => Set<OrdenCompraAdjunto>();

    // Roles
    public DbSet<Rol> Roles => Set<Rol>();

    // Restore
    public DbSet<RestoreLog> RestoreLogs => Set<RestoreLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("docflow");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocFlowDbContext).Assembly);
    }
}
