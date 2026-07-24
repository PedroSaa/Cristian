using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Enums;
using DocFlow.Domain.ValueObjects;
using DocFlow.Infrastructure.Data;
using DocFlow.Infrastructure.Repositories.OrdenesCompra;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Repositories.OrdenesCompra;

public class OrdenCompraRepositoryTests
{
    private static readonly Guid CreadorId = Guid.NewGuid();

    private static DocFlowDbContext CreateContext()
        => new(new DbContextOptionsBuilder<DocFlowDbContext>()
            .UseInMemoryDatabase($"OrdenCompraRepoTest_{Guid.NewGuid()}")
            .Options);

    private static Proveedor SeedProveedor(DocFlowDbContext db, string nombre = "Acme SA")
    {
        var proveedor = Proveedor.Crear(Guid.NewGuid(), Rut.Create("12345678-5"), nombre, "Construcción");
        db.Proveedores.Add(proveedor);
        db.SaveChanges();
        return proveedor;
    }

    private static OrdenCompra NuevaOrden(Guid proveedorId, string? numero = null, string? observaciones = null)
    {
        var oc = OrdenCompra.Crear(Guid.NewGuid(), proveedorId, new DateTime(2026, 7, 1), CreadorId,
            observaciones: observaciones);
        oc.ReemplazarItems(new[] { new OrdenCompraItemData("Item", 1m, 1000m) });
        if (numero is not null)
            oc.EnviarAAprobacion(numero);
        return oc;
    }

    [Fact]
    public async Task AddAndGetById_ShouldRoundTrip_WithItems()
    {
        using var db = CreateContext();
        var proveedor = SeedProveedor(db);
        var repo = new OrdenCompraRepository(db);
        var oc = NuevaOrden(proveedor.Id);

        await repo.AddAsync(oc);
        var loaded = await repo.GetByIdAsync(oc.Id);

        loaded.Should().NotBeNull();
        loaded!.Items.Should().HaveCount(1);
        loaded.Neto.Should().Be(1000m);
        loaded.Iva.Should().Be(190m);
        loaded.Total.Should().Be(1190m);
    }

    [Fact]
    public async Task Update_ShouldPersistReplacedItems()
    {
        using var db = CreateContext();
        var proveedor = SeedProveedor(db);
        var repo = new OrdenCompraRepository(db);
        var oc = NuevaOrden(proveedor.Id);
        await repo.AddAsync(oc);

        var tracked = await repo.GetByIdAsync(oc.Id);
        tracked!.ReemplazarItems(new[]
        {
            new OrdenCompraItemData("Nuevo A", 2m, 100m),
            new OrdenCompraItemData("Nuevo B", 3m, 200m),
        });
        await repo.UpdateAsync(tracked);

        var reloaded = await repo.GetByIdAsync(oc.Id);
        reloaded!.Items.Should().HaveCount(2);
        db.OrdenesCompraItems.Count(i => i.OrdenCompraId == oc.Id).Should().Be(2);
        reloaded.Total.Should().Be(800m + 152m);
    }

    [Fact]
    public async Task GetList_ShouldJoinProveedorNombre_AndPaginate()
    {
        using var db = CreateContext();
        var proveedor = SeedProveedor(db, "Constructora Sur");
        var repo = new OrdenCompraRepository(db);
        for (var i = 1; i <= 3; i++)
            await repo.AddAsync(NuevaOrden(proveedor.Id, numero: $"OC-2026-000{i}"));

        var (items, total) = await repo.GetListAsync(page: 1, pageSize: 2);

        total.Should().Be(3);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(r => r.ProveedorNombre == "Constructora Sur");
    }

    [Fact]
    public async Task GetList_ShouldFilterByEstadoAndProveedorAndSearch()
    {
        using var db = CreateContext();
        var proveedor = SeedProveedor(db);
        var repo = new OrdenCompraRepository(db);

        var borrador = NuevaOrden(proveedor.Id, observaciones: "urgente compra invierno");
        var pendiente = NuevaOrden(proveedor.Id, numero: "OC-2026-0042");
        await repo.AddAsync(borrador);
        await repo.AddAsync(pendiente);

        var (porEstado, _) = await repo.GetListAsync(estado: EstadoOrdenCompra.PendienteAprobacion);
        porEstado.Should().ContainSingle(r => r.Id == pendiente.Id);

        var (porNumero, _) = await repo.GetListAsync(search: "0042");
        porNumero.Should().ContainSingle(r => r.Id == pendiente.Id);

        var (porObservaciones, _) = await repo.GetListAsync(search: "INVIERNO");
        porObservaciones.Should().ContainSingle(r => r.Id == borrador.Id);

        var (porProveedor, totalProveedor) = await repo.GetListAsync(proveedorId: proveedor.Id);
        totalProveedor.Should().Be(2);

        var (otroProveedor, totalOtro) = await repo.GetListAsync(proveedorId: Guid.NewGuid());
        totalOtro.Should().Be(0);
        otroProveedor.Should().BeEmpty();
    }

    [Fact]
    public async Task Adjuntos_ShouldAddReadAndRemove_WithMetadataProjection()
    {
        using var db = CreateContext();
        var proveedor = SeedProveedor(db);
        var repo = new OrdenCompraRepository(db);
        var oc = NuevaOrden(proveedor.Id);
        await repo.AddAsync(oc);

        var adjunto = OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), oc.Id, "espec.pdf", "application/pdf", [1, 2, 3], CreadorId);
        await repo.AddAdjuntoAsync(adjunto);

        var metadata = await repo.GetAdjuntosMetadataAsync(oc.Id);
        metadata.Should().ContainSingle(m =>
            m.Id == adjunto.Id && m.NombreArchivo == "espec.pdf" && m.Tamano == 3);

        var loaded = await repo.GetAdjuntoAsync(oc.Id, adjunto.Id);
        loaded.Should().NotBeNull();
        loaded!.Contenido.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });

        await repo.RemoveAdjuntoAsync(loaded);
        (await repo.GetAdjuntosMetadataAsync(oc.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdjunto_ShouldReturnNull_WhenAdjuntoBelongsToAnotherOrder()
    {
        using var db = CreateContext();
        var proveedor = SeedProveedor(db);
        var repo = new OrdenCompraRepository(db);
        var oc1 = NuevaOrden(proveedor.Id);
        var oc2 = NuevaOrden(proveedor.Id);
        await repo.AddAsync(oc1);
        await repo.AddAsync(oc2);

        var adjunto = OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), oc1.Id, "a.pdf", "application/pdf", [1], CreadorId);
        await repo.AddAdjuntoAsync(adjunto);

        (await repo.GetAdjuntoAsync(oc2.Id, adjunto.Id)).Should().BeNull();
    }
}
