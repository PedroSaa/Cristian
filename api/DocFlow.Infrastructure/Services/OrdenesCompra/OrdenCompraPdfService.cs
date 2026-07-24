using System.Globalization;
using DocFlow.Application.OrdenesCompra.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DocFlow.Infrastructure.Services.OrdenesCompra;

/// <summary>
/// Module-owned QuestPDF layout for purchase orders: header with number/date/state,
/// provider block, delivery/payment block, items table, totals (Neto/IVA 19%/Total),
/// observations and an approval footer when the order is approved.
/// </summary>
public class OrdenCompraPdfService : IOrdenCompraPdfService
{
    static OrdenCompraPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly CultureInfo Chile = CultureInfo.GetCultureInfo("es-CL");

    public byte[] Generar(OrdenCompraPdfData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, OrdenCompraPdfData data)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("ORDEN DE COMPRA").Bold().FontSize(18);
                    left.Item().Text(data.Numero ?? "(borrador)").FontSize(13).SemiBold();
                });

                row.ConstantItem(200).AlignRight().Column(right =>
                {
                    right.Item().Text($"Fecha: {data.Fecha.ToString("dd-MM-yyyy", Chile)}");
                    right.Item().Text($"Estado: {data.Estado}").SemiBold();
                    right.Item().Text($"Moneda: {data.Moneda}");
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private static void ComposeContent(IContainer container, OrdenCompraPdfData data)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Spacing(10);

            // Provider block
            col.Item().Element(c => ComposeSection(c, "Proveedor", block =>
            {
                block.Item().Text(data.ProveedorNombre).SemiBold();
                block.Item().Text($"RUT: {data.ProveedorRut}");
                if (!string.IsNullOrWhiteSpace(data.ProveedorContacto))
                    block.Item().Text($"Contacto: {data.ProveedorContacto}");
                if (!string.IsNullOrWhiteSpace(data.ProveedorEmail))
                    block.Item().Text($"Email: {data.ProveedorEmail}");
                if (!string.IsNullOrWhiteSpace(data.ProveedorTelefono))
                    block.Item().Text($"Teléfono: {data.ProveedorTelefono}");
                if (!string.IsNullOrWhiteSpace(data.ProveedorDireccion))
                    block.Item().Text($"Dirección: {data.ProveedorDireccion}");
            }));

            // Delivery / payment block
            col.Item().Element(c => ComposeSection(c, "Condiciones", block =>
            {
                block.Item().Text($"Forma de pago: {data.FormaPago ?? "-"}");
                block.Item().Text($"Plazo de entrega: {data.PlazoEntrega ?? "-"}");
                block.Item().Text($"Lugar de entrega: {data.LugarEntrega ?? "-"}");
            }));

            // Items table
            col.Item().Element(c => ComposeItemsTable(c, data));

            // Totals
            col.Item().AlignRight().Element(c => ComposeTotals(c, data));

            // Observations
            if (!string.IsNullOrWhiteSpace(data.Observaciones))
            {
                col.Item().Element(c => ComposeSection(c, "Observaciones", block =>
                {
                    block.Item().Text(data.Observaciones);
                }));
            }

            // Approval footer
            if (data.AprobadoEn is not null)
            {
                col.Item().PaddingTop(10).Element(c => ComposeSection(c, "Aprobación", block =>
                {
                    block.Item().Text(
                        $"Aprobada por {(string.IsNullOrWhiteSpace(data.AprobadorNombre) ? "-" : data.AprobadorNombre)} " +
                        $"el {data.AprobadoEn.Value.ToString("dd-MM-yyyy HH:mm", Chile)}");
                    if (!string.IsNullOrWhiteSpace(data.ComentarioAprobacion))
                        block.Item().Text($"Comentario: {data.ComentarioAprobacion}");
                }));
            }
        });
    }

    private static void ComposeSection(IContainer container, string titulo, Action<ColumnDescriptor> body)
    {
        container.Column(col =>
        {
            col.Item().Text(titulo).Bold().FontSize(11);
            col.Item().PaddingTop(2).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(body);
        });
    }

    private static void ComposeItemsTable(IContainer container, OrdenCompraPdfData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(35);    // Línea
                cols.RelativeColumn(4f);    // Descripción
                cols.RelativeColumn(1.2f);  // Cantidad
                cols.RelativeColumn(1.6f);  // Precio unitario
                cols.RelativeColumn(1.6f);  // Total línea
            });

            table.Header(header =>
            {
                static IContainer HeaderCell(IContainer c) =>
                    c.Background(Colors.Grey.Lighten2).Padding(4);

                header.Cell().Element(HeaderCell).Text("N°").Bold();
                header.Cell().Element(HeaderCell).Text("Descripción").Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("Cantidad").Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("Precio unit.").Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("Total").Bold();
            });

            var alternate = false;
            foreach (var item in data.Items)
            {
                var bg = alternate ? Colors.Grey.Lighten4 : Colors.White;
                alternate = !alternate;

                table.Cell().Background(bg).Padding(3).Text(item.NumeroLinea.ToString(Chile));
                table.Cell().Background(bg).Padding(3).Text(item.Descripcion);
                table.Cell().Background(bg).Padding(3).AlignRight().Text(FormatCantidad(item.Cantidad));
                table.Cell().Background(bg).Padding(3).AlignRight().Text(FormatMonto(item.PrecioUnitario));
                table.Cell().Background(bg).Padding(3).AlignRight().Text(FormatMonto(item.TotalLinea));
            }
        });
    }

    private static void ComposeTotals(IContainer container, OrdenCompraPdfData data)
    {
        container.Width(220).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Neto");
                row.ConstantItem(120).AlignRight().Text(FormatMonto(data.Neto));
            });
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("IVA (19%)");
                row.ConstantItem(120).AlignRight().Text(FormatMonto(data.Iva));
            });
            col.Item().PaddingTop(2).BorderTop(1).BorderColor(Colors.Grey.Medium).Row(row =>
            {
                row.RelativeItem().Text("Total").Bold();
                row.ConstantItem(120).AlignRight().Text(FormatMonto(data.Total)).Bold();
            });
        });
    }

    private static string FormatMonto(decimal valor)
        => valor.ToString("#,##0.##", Chile);

    private static string FormatCantidad(decimal valor)
        => valor.ToString("#,##0.####", Chile);
}
