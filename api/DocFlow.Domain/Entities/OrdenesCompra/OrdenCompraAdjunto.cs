namespace DocFlow.Domain.Entities.OrdenesCompra;

public class OrdenCompraAdjunto
{
    public const int NombreArchivoMaxLength = 255;
    public const int ContentTypeMaxLength = 100;

    public Guid Id { get; private set; }
    public Guid OrdenCompraId { get; private set; }
    public string NombreArchivo { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public byte[] Contenido { get; private set; } = [];
    public long Tamano { get; private set; }
    public Guid SubidoPor { get; private set; }
    public DateTime CreadoEn { get; private set; }

    private OrdenCompraAdjunto() { }

    public static OrdenCompraAdjunto Crear(
        Guid id,
        Guid ordenCompraId,
        string nombreArchivo,
        string contentType,
        byte[] contenido,
        Guid subidoPor)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
            throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(nombreArchivo));
        if (nombreArchivo.Trim().Length > NombreArchivoMaxLength)
            throw new ArgumentException($"El nombre del archivo no puede superar los {NombreArchivoMaxLength} caracteres.", nameof(nombreArchivo));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("El tipo de contenido es obligatorio.", nameof(contentType));
        if (contentType.Trim().Length > ContentTypeMaxLength)
            throw new ArgumentException($"El tipo de contenido no puede superar los {ContentTypeMaxLength} caracteres.", nameof(contentType));
        if (contenido is null || contenido.Length == 0)
            throw new ArgumentException("El contenido del adjunto no puede estar vacío.", nameof(contenido));
        if (subidoPor == Guid.Empty)
            throw new ArgumentException("El usuario que sube el adjunto es obligatorio.", nameof(subidoPor));

        return new OrdenCompraAdjunto
        {
            Id = id,
            OrdenCompraId = ordenCompraId,
            NombreArchivo = nombreArchivo.Trim(),
            ContentType = contentType.Trim(),
            Contenido = contenido,
            Tamano = contenido.LongLength,
            SubidoPor = subidoPor,
            CreadoEn = DateTime.UtcNow,
        };
    }
}
