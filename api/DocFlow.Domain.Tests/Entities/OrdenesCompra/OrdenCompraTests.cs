using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DocFlow.Domain.Tests.Entities.OrdenesCompra;

public class OrdenCompraTests
{
    private static readonly Guid ProveedorId = Guid.NewGuid();
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly Guid AprobadorId = Guid.NewGuid();

    private static OrdenCompra CrearBorrador() =>
        OrdenCompra.Crear(
            Guid.NewGuid(),
            ProveedorId,
            new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc),
            CreadorId,
            formaPago: "Transferencia 30 días",
            plazoEntrega: "15 días hábiles",
            lugarEntrega: "Bodega central",
            observaciones: "Entrega parcial permitida");

    private static OrdenCompra CrearBorradorConItems()
    {
        var oc = CrearBorrador();
        oc.ReemplazarItems(new[]
        {
            new OrdenCompraItemData("Notebook", 2m, 500000m),
            new OrdenCompraItemData("Mouse", 10m, 5000m),
        });
        return oc;
    }

    private static OrdenCompra CrearPendiente(string numero = "OC-2026-0001")
    {
        var oc = CrearBorradorConItems();
        oc.EnviarAAprobacion(numero);
        return oc;
    }

    // ── Crear ──

    [Fact]
    public void Crear_ShouldStartAsBorrador_WithDefaults()
    {
        var id = Guid.NewGuid();
        var oc = OrdenCompra.Crear(id, ProveedorId, new DateTime(2026, 7, 1), CreadorId);

        oc.Id.Should().Be(id);
        oc.ProveedorId.Should().Be(ProveedorId);
        oc.CreadoPor.Should().Be(CreadorId);
        oc.Estado.Should().Be(EstadoOrdenCompra.Borrador);
        oc.Numero.Should().BeNull();
        oc.Moneda.Should().Be("CLP");
        oc.Neto.Should().Be(0);
        oc.Iva.Should().Be(0);
        oc.Total.Should().Be(0);
        oc.Items.Should().BeEmpty();
        oc.Adjuntos.Should().BeEmpty();
        oc.CreadoEn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        oc.ActualizadoEn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Crear_ShouldNormalizeFechaToUtc_WhenKindIsUnspecified()
    {
        // Npgsql rejects DateTime with Kind=Unspecified on timestamptz columns.
        var oc = OrdenCompra.Crear(Guid.NewGuid(), ProveedorId, new DateTime(2026, 7, 3), CreadorId);

        oc.Fecha.Kind.Should().Be(DateTimeKind.Utc);
        oc.Fecha.Should().Be(new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Crear_ShouldThrow_WhenProveedorIdIsEmpty()
    {
        var act = () => OrdenCompra.Crear(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, CreadorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenCreadoPorIsEmpty()
    {
        var act = () => OrdenCompra.Crear(Guid.NewGuid(), ProveedorId, DateTime.UtcNow, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    // ── Totales / IVA 19% ──

    [Fact]
    public void ReemplazarItems_ShouldComputeNetoIvaAndTotal_WithChileanIva()
    {
        var oc = CrearBorrador();

        oc.ReemplazarItems(new[]
        {
            new OrdenCompraItemData("Notebook", 2m, 500000m), // 1.000.000
            new OrdenCompraItemData("Mouse", 10m, 5000m),     //    50.000
        });

        oc.Neto.Should().Be(1050000m);
        oc.Iva.Should().Be(Math.Round(1050000m * 0.19m, 0)); // 199.500
        oc.Total.Should().Be(1050000m + 199500m);
    }

    [Fact]
    public void ReemplazarItems_ShouldRoundIvaToZeroDecimals()
    {
        var oc = CrearBorrador();

        oc.ReemplazarItems(new[] { new OrdenCompraItemData("Servicio", 1m, 333m) });

        oc.Neto.Should().Be(333m);
        oc.Iva.Should().Be(Math.Round(333m * 0.19m, 0)); // 63,27 → 63
        oc.Total.Should().Be(333m + 63m);
    }

    [Fact]
    public void ReemplazarItems_ShouldRoundIvaMidpointAwayFromZero_NotBankers()
    {
        // Neto 150 → IVA 28.5: SII/práctica chilena espera 29 (half-away-from-zero),
        // no 28 (banker's rounding, el default de Math.Round).
        var oc = CrearBorrador();

        oc.ReemplazarItems(new[] { new OrdenCompraItemData("Servicio", 1m, 150m) });

        oc.Neto.Should().Be(150m);
        oc.Iva.Should().Be(29m);
        oc.Total.Should().Be(179m);
    }

    [Fact]
    public void ReemplazarItems_ShouldRoundTotalLineaToTwoDecimals_SoNetoMatchesPersistedLines()
    {
        // total_linea es numeric(18,2): si no se redondea en memoria, el neto calculado
        // difiere de la suma de líneas tras persistir (descuadre de centavos).
        var oc = CrearBorrador();

        oc.ReemplazarItems(new[] { new OrdenCompraItemData("Granel", 0.3333m, 10.01m) });

        var item = oc.Items.Single();
        item.TotalLinea.Should().Be(3.34m); // 3.336333 → 3.34 (away from zero)
        oc.Neto.Should().Be(3.34m);
    }

    [Fact]
    public void ReemplazarItems_ShouldAssignSequentialLineNumbers()
    {
        var oc = CrearBorrador();

        oc.ReemplazarItems(new[]
        {
            new OrdenCompraItemData("A", 1m, 100m),
            new OrdenCompraItemData("B", 1m, 200m),
            new OrdenCompraItemData("C", 1m, 300m),
        });

        oc.Items.Select(i => i.NumeroLinea).Should().ContainInOrder(1, 2, 3);
        oc.Items.Should().OnlyContain(i => i.OrdenCompraId == oc.Id);
    }

    [Fact]
    public void ReemplazarItems_ShouldReplaceExistingItems_AndRecalculate()
    {
        var oc = CrearBorradorConItems();

        oc.ReemplazarItems(new[] { new OrdenCompraItemData("Único", 1m, 1000m) });

        oc.Items.Should().HaveCount(1);
        oc.Neto.Should().Be(1000m);
        oc.Iva.Should().Be(190m);
        oc.Total.Should().Be(1190m);
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    [InlineData(EstadoOrdenCompra.Anulada)]
    public void ReemplazarItems_ShouldThrow_WhenNotEditable(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.ReemplazarItems(new[] { new OrdenCompraItemData("X", 1m, 1m) });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReemplazarItems_ShouldBeAllowed_WhenRechazada()
    {
        var oc = EnEstado(EstadoOrdenCompra.Rechazada);

        var act = () => oc.ReemplazarItems(new[] { new OrdenCompraItemData("X", 1m, 100m) });

        act.Should().NotThrow();
        oc.Neto.Should().Be(100m);
    }

    // ── ActualizarDatos ──

    [Fact]
    public void ActualizarDatos_ShouldUpdateFields_WhenBorrador()
    {
        var oc = CrearBorrador();
        var nuevoProveedor = Guid.NewGuid();

        oc.ActualizarDatos(nuevoProveedor, new DateTime(2026, 8, 1), "USD",
            "Contado", "48 horas", "Sucursal norte", "Sin observaciones");

        oc.ProveedorId.Should().Be(nuevoProveedor);
        oc.Fecha.Should().Be(new DateTime(2026, 8, 1));
        oc.Moneda.Should().Be("USD");
        oc.FormaPago.Should().Be("Contado");
        oc.PlazoEntrega.Should().Be("48 horas");
        oc.LugarEntrega.Should().Be("Sucursal norte");
        oc.Observaciones.Should().Be("Sin observaciones");
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    [InlineData(EstadoOrdenCompra.Anulada)]
    public void ActualizarDatos_ShouldThrow_WhenNotEditable(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.ActualizarDatos(ProveedorId, DateTime.UtcNow, "CLP", null, null, null, null);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── EnviarAAprobacion ──

    [Fact]
    public void EnviarAAprobacion_ShouldAssignNumero_AndTransitionToPendiente()
    {
        var oc = CrearBorradorConItems();

        oc.EnviarAAprobacion("OC-2026-0042");

        oc.Estado.Should().Be(EstadoOrdenCompra.PendienteAprobacion);
        oc.Numero.Should().Be("OC-2026-0042");
    }

    [Fact]
    public void EnviarAAprobacion_ShouldThrow_WhenNoItems()
    {
        var oc = CrearBorrador();

        var act = () => oc.EnviarAAprobacion("OC-2026-0001");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnviarAAprobacion_ShouldThrow_WhenNumeroMissing_AndNoneAssigned()
    {
        var oc = CrearBorradorConItems();

        var act = () => oc.EnviarAAprobacion("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    [InlineData(EstadoOrdenCompra.Anulada)]
    public void EnviarAAprobacion_ShouldThrow_WhenInvalidSourceState(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.EnviarAAprobacion("OC-2026-0099");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnviarAAprobacion_ShouldKeepOriginalNumero_WhenResubmittedAfterRechazo()
    {
        var oc = CrearPendiente("OC-2026-0007");
        oc.Rechazar(AprobadorId, "Falta detalle");

        oc.EnviarAAprobacion("OC-2026-9999"); // must be ignored — number already assigned

        oc.Estado.Should().Be(EstadoOrdenCompra.PendienteAprobacion);
        oc.Numero.Should().Be("OC-2026-0007");
    }

    [Fact]
    public void EnviarAAprobacion_ShouldResetApprovalFields_WhenResubmitted()
    {
        var oc = CrearPendiente();
        oc.Rechazar(AprobadorId, "Precio fuera de presupuesto");

        oc.EnviarAAprobacion(null);

        oc.AprobadoPor.Should().BeNull();
        oc.AprobadoEn.Should().BeNull();
        oc.ComentarioAprobacion.Should().BeNull();
    }

    // ── Aprobar ──

    [Fact]
    public void Aprobar_ShouldTransitionToAprobada_AndRecordApprover()
    {
        var oc = CrearPendiente();

        oc.Aprobar(AprobadorId, "Todo en orden");

        oc.Estado.Should().Be(EstadoOrdenCompra.Aprobada);
        oc.AprobadoPor.Should().Be(AprobadorId);
        oc.AprobadoEn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        oc.ComentarioAprobacion.Should().Be("Todo en orden");
    }

    [Fact]
    public void Aprobar_ShouldAllowNullComment()
    {
        var oc = CrearPendiente();

        oc.Aprobar(AprobadorId, null);

        oc.Estado.Should().Be(EstadoOrdenCompra.Aprobada);
        oc.ComentarioAprobacion.Should().BeNull();
    }

    [Fact]
    public void Aprobar_ShouldThrow_WhenApproverIsCreator()
    {
        var oc = CrearPendiente();

        var act = () => oc.Aprobar(CreadorId, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*propia*");
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.Borrador)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Rechazada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    [InlineData(EstadoOrdenCompra.Anulada)]
    public void Aprobar_ShouldThrow_WhenNotPendiente(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.Aprobar(AprobadorId, null);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Rechazar ──

    [Fact]
    public void Rechazar_ShouldTransitionToRechazada_AndRecordComment()
    {
        var oc = CrearPendiente();

        oc.Rechazar(AprobadorId, "Presupuesto insuficiente");

        oc.Estado.Should().Be(EstadoOrdenCompra.Rechazada);
        oc.ComentarioAprobacion.Should().Be("Presupuesto insuficiente");
        oc.AprobadoPor.Should().Be(AprobadorId);
        oc.AprobadoEn.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechazar_ShouldThrow_WhenCommentMissing(string? comentario)
    {
        var oc = CrearPendiente();

        var act = () => oc.Rechazar(AprobadorId, comentario!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.Borrador)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Rechazada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    [InlineData(EstadoOrdenCompra.Anulada)]
    public void Rechazar_ShouldThrow_WhenNotPendiente(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.Rechazar(AprobadorId, "Comentario");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── MarcarEnviada ──

    [Fact]
    public void MarcarEnviada_ShouldTransition_FromAprobada()
    {
        var oc = EnEstado(EstadoOrdenCompra.Aprobada);

        oc.MarcarEnviada();

        oc.Estado.Should().Be(EstadoOrdenCompra.Enviada);
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.Borrador)]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    [InlineData(EstadoOrdenCompra.Rechazada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    [InlineData(EstadoOrdenCompra.Anulada)]
    public void MarcarEnviada_ShouldThrow_WhenNotAprobada(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.MarcarEnviada();

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Anular ──

    [Theory]
    [InlineData(EstadoOrdenCompra.Borrador)]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Rechazada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    public void Anular_ShouldTransitionToAnulada_FromAnyNonTerminalState(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        oc.Anular("Compra duplicada");

        oc.Estado.Should().Be(EstadoOrdenCompra.Anulada);
        oc.MotivoAnulacion.Should().Be("Compra duplicada");
    }

    [Fact]
    public void Anular_ShouldThrow_WhenAlreadyAnulada()
    {
        var oc = EnEstado(EstadoOrdenCompra.Anulada);

        var act = () => oc.Anular("Otra vez");

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Anular_ShouldThrow_WhenMotivoMissing(string? motivo)
    {
        var oc = CrearBorrador();

        var act = () => oc.Anular(motivo!);

        act.Should().Throw<ArgumentException>();
    }

    // ── VincularMercadoPublico / DesvincularMercadoPublico ──

    [Theory]
    [InlineData(EstadoOrdenCompra.Borrador)]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Rechazada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    public void VincularMercadoPublico_ShouldSetCodigo_FromAnyNonAnuladaState(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        oc.VincularMercadoPublico("1123-109-SE13");

        oc.CodigoMercadoPublico.Should().Be("1123-109-SE13");
    }

    [Fact]
    public void VincularMercadoPublico_ShouldTrimCodigo()
    {
        var oc = CrearBorradorConItems();

        oc.VincularMercadoPublico("  1123-109-SE13  ");

        oc.CodigoMercadoPublico.Should().Be("1123-109-SE13");
    }

    [Fact]
    public void VincularMercadoPublico_ShouldThrow_WhenAnulada()
    {
        var oc = EnEstado(EstadoOrdenCompra.Anulada);

        var act = () => oc.VincularMercadoPublico("1123-109-SE13");

        act.Should().Throw<InvalidOperationException>();
        oc.CodigoMercadoPublico.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VincularMercadoPublico_ShouldThrow_WhenCodigoMissing(string? codigo)
    {
        var oc = CrearBorradorConItems();

        var act = () => oc.VincularMercadoPublico(codigo!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VincularMercadoPublico_ShouldThrow_WhenCodigoTooLong()
    {
        var oc = CrearBorradorConItems();

        var act = () => oc.VincularMercadoPublico(new string('X', 41));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DesvincularMercadoPublico_ShouldClearCodigo()
    {
        var oc = CrearBorradorConItems();
        oc.VincularMercadoPublico("1123-109-SE13");

        oc.DesvincularMercadoPublico();

        oc.CodigoMercadoPublico.Should().BeNull();
    }

    [Fact]
    public void DesvincularMercadoPublico_ShouldBeIdempotent_WhenNoCodigo()
    {
        var oc = CrearBorradorConItems();

        var act = () => oc.DesvincularMercadoPublico();

        act.Should().NotThrow();
        oc.CodigoMercadoPublico.Should().BeNull();
    }

    // ── Helper: drive the entity to a given state through its public API ──

    // ── Adjuntos: integridad documental ──

    [Theory]
    [InlineData(EstadoOrdenCompra.Borrador)]
    [InlineData(EstadoOrdenCompra.Rechazada)]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    public void ExigirPuedeEliminarAdjuntos_ShouldNotThrow_BeforeApprovalDecision(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.ExigirPuedeEliminarAdjuntos();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    public void ExigirPuedeEliminarAdjuntos_ShouldThrow_AfterApproval(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.ExigirPuedeEliminarAdjuntos();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*eliminar respaldos*");
    }

    [Fact]
    public void ExigirPuedeEliminarAdjuntos_ShouldThrow_WhenAnulada()
    {
        var oc = EnEstado(EstadoOrdenCompra.Anulada);

        var act = () => oc.ExigirPuedeEliminarAdjuntos();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(EstadoOrdenCompra.Borrador)]
    [InlineData(EstadoOrdenCompra.Rechazada)]
    [InlineData(EstadoOrdenCompra.PendienteAprobacion)]
    [InlineData(EstadoOrdenCompra.Aprobada)]
    [InlineData(EstadoOrdenCompra.Enviada)]
    public void ExigirPuedeAgregarAdjuntos_ShouldNotThrow_UnlessAnulada(EstadoOrdenCompra estado)
    {
        var oc = EnEstado(estado);

        var act = () => oc.ExigirPuedeAgregarAdjuntos();

        act.Should().NotThrow();
    }

    [Fact]
    public void ExigirPuedeAgregarAdjuntos_ShouldThrow_WhenAnulada()
    {
        var oc = EnEstado(EstadoOrdenCompra.Anulada);

        var act = () => oc.ExigirPuedeAgregarAdjuntos();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*anulada*");
    }

    private static OrdenCompra EnEstado(EstadoOrdenCompra estado)
    {
        if (estado == EstadoOrdenCompra.Borrador)
            return CrearBorradorConItems();

        var oc = CrearPendiente();

        switch (estado)
        {
            case EstadoOrdenCompra.PendienteAprobacion:
                return oc;
            case EstadoOrdenCompra.Aprobada:
                oc.Aprobar(AprobadorId, null);
                return oc;
            case EstadoOrdenCompra.Rechazada:
                oc.Rechazar(AprobadorId, "Rechazada para prueba");
                return oc;
            case EstadoOrdenCompra.Enviada:
                oc.Aprobar(AprobadorId, null);
                oc.MarcarEnviada();
                return oc;
            case EstadoOrdenCompra.Anulada:
                oc.Anular("Anulada para prueba");
                return oc;
            default:
                throw new ArgumentOutOfRangeException(nameof(estado));
        }
    }
}
