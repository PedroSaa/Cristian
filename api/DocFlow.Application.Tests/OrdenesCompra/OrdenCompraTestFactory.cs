using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.ValueObjects;

namespace DocFlow.Application.Tests.OrdenesCompra;

/// <summary>Shared builders for purchase order handler tests.</summary>
internal static class OrdenCompraTestFactory
{
    public static readonly Guid ProveedorId = Guid.NewGuid();
    public static readonly Guid CreadorId = Guid.NewGuid();
    public static readonly Guid AprobadorId = Guid.NewGuid();

    public static Proveedor Proveedor() =>
        Domain.Entities.Proveedor.Crear(ProveedorId, Rut.Create("12345678-5"), "Acme SA", "Construcción");

    public static OrdenCompra Borrador(Guid? creadoPor = null)
    {
        var oc = OrdenCompra.Crear(
            Guid.NewGuid(), ProveedorId, new DateTime(2026, 7, 1), creadoPor ?? CreadorId);
        oc.ReemplazarItems(new[]
        {
            new OrdenCompraItemData("Notebook", 2m, 500000m),
            new OrdenCompraItemData("Mouse", 10m, 5000m),
        });
        return oc;
    }

    public static OrdenCompra BorradorSinItems(Guid? creadoPor = null) =>
        OrdenCompra.Crear(Guid.NewGuid(), ProveedorId, new DateTime(2026, 7, 1), creadoPor ?? CreadorId);

    public static OrdenCompra Pendiente(Guid? creadoPor = null, string numero = "OC-2026-0001")
    {
        var oc = Borrador(creadoPor);
        oc.EnviarAAprobacion(numero);
        return oc;
    }

    public static OrdenCompra Aprobada(Guid? creadoPor = null)
    {
        var oc = Pendiente(creadoPor);
        oc.Aprobar(AprobadorId, null);
        return oc;
    }

    public static OrdenCompra Enviada(Guid? creadoPor = null)
    {
        var oc = Aprobada(creadoPor);
        oc.MarcarEnviada();
        return oc;
    }

    public static OrdenCompra Anulada(Guid? creadoPor = null)
    {
        var oc = Borrador(creadoPor);
        oc.Anular("Anulada para prueba");
        return oc;
    }
}
