namespace DocFlow.Domain.Entities;

/// <summary>
/// User signature configuration mirrored from the legacy GeneXus table SEUSUFIRMADOR:
/// one signature per user = signature image + optional encrypted PIN/password + optional acronym (sigla).
/// This entity only holds configuration; PDF stamping is out of scope.
/// The password is stored ALREADY ENCRYPTED by the Application layer (the domain never encrypts).
/// </summary>
public class FirmaUsuario
{
    public const int SiglaMaxLength = 50;
    public const int ContentTypeMaxLength = 100;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public byte[] ImagenFirma { get; private set; } = [];
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Encrypted signature PIN/password. Null when the user configured no PIN. Never stored in plaintext.</summary>
    public string? ClaveCifrada { get; private set; }

    public string? Sigla { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    private FirmaUsuario() { }

    public static FirmaUsuario Crear(
        Guid id,
        Guid usuarioId,
        byte[] imagenFirma,
        string contentType,
        string? claveCifrada = null,
        string? sigla = null)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));

        ValidarImagen(imagenFirma);
        ValidarContentType(contentType);
        var siglaNormalizada = NormalizarSigla(sigla);

        var now = DateTime.UtcNow;

        return new FirmaUsuario
        {
            Id = id,
            UsuarioId = usuarioId,
            ImagenFirma = imagenFirma,
            ContentType = contentType.Trim(),
            ClaveCifrada = string.IsNullOrWhiteSpace(claveCifrada) ? null : claveCifrada,
            Sigla = siglaNormalizada,
            CreadoEn = now,
            ActualizadoEn = now,
        };
    }

    /// <summary>Replaces the signature payload (image + content type + encrypted PIN + acronym), keeping the same row.</summary>
    public void Actualizar(
        byte[] imagenFirma,
        string contentType,
        string? claveCifrada = null,
        string? sigla = null)
    {
        ValidarImagen(imagenFirma);
        ValidarContentType(contentType);

        ImagenFirma = imagenFirma;
        ContentType = contentType.Trim();
        ClaveCifrada = string.IsNullOrWhiteSpace(claveCifrada) ? null : claveCifrada;
        Sigla = NormalizarSigla(sigla);
        ActualizadoEn = DateTime.UtcNow;
    }

    private static void ValidarImagen(byte[] imagenFirma)
    {
        if (imagenFirma is null || imagenFirma.Length == 0)
            throw new ArgumentException("La imagen de la firma es obligatoria.", nameof(imagenFirma));
    }

    private static void ValidarContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("El tipo de contenido de la firma es obligatorio.", nameof(contentType));
        if (contentType.Trim().Length > ContentTypeMaxLength)
            throw new ArgumentException($"El tipo de contenido no puede superar los {ContentTypeMaxLength} caracteres.", nameof(contentType));
    }

    private static string? NormalizarSigla(string? sigla)
    {
        if (string.IsNullOrWhiteSpace(sigla))
            return null;

        var trimmed = sigla.Trim();
        if (trimmed.Length > SiglaMaxLength)
            throw new ArgumentException($"La sigla no puede superar los {SiglaMaxLength} caracteres.", nameof(sigla));

        return trimmed;
    }
}
