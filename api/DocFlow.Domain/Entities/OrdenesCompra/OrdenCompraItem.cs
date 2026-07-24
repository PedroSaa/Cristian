namespace DocFlow.Domain.Entities.OrdenesCompra;

public class OrdenCompraItem
{
    public const int DescripcionMaxLength = 300;

    public Guid Id { get; private set; }
    public Guid OrdenCompraId { get; private set; }
    public int NumeroLinea { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal TotalLinea { get; private set; }

    private OrdenCompraItem() { }

    public static OrdenCompraItem Crear(
        Guid id,
        Guid ordenCompraId,
        int numeroLinea,
        string descripcion,
        decimal cantidad,
        decimal precioUnitario)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ArgumentException("La descripción del ítem es obligatoria.", nameof(descripcion));
        if (descripcion.Trim().Length > DescripcionMaxLength)
            throw new ArgumentException($"La descripción no puede superar los {DescripcionMaxLength} caracteres.", nameof(descripcion));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        if (precioUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio unitario no puede ser negativo.");
        if (numeroLinea <= 0)
            throw new ArgumentOutOfRangeException(nameof(numeroLinea), "El número de línea debe ser mayor que cero.");

        return new OrdenCompraItem
        {
            Id = id,
            OrdenCompraId = ordenCompraId,
            NumeroLinea = numeroLinea,
            Descripcion = descripcion.Trim(),
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            // Redondeado a 2 decimales igual que la columna numeric(18,2): si no,
            // el neto en memoria difiere de la suma de líneas persistidas.
            TotalLinea = Math.Round(cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero),
        };
    }
}
