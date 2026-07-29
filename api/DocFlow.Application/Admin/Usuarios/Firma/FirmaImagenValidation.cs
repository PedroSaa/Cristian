namespace DocFlow.Application.Admin.Usuarios.Firma;

/// <summary>
/// Signature image constraints: PNG or JPEG only, verified by declared content type AND magic bytes
/// (a client-controlled content type alone is not trusted), with a 2 MB size cap.
/// </summary>
public static class FirmaImagenValidation
{
    public const long MaxImageBytes = 2 * 1024 * 1024;
    public const int MaxImageMegabytes = 2;

    public static bool IsAllowedContentType(string? contentType)
        => string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase)
        || string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the byte content's magic bytes match the declared PNG/JPEG content type.</summary>
    public static bool HasMatchingSignature(byte[]? content, string? contentType)
    {
        if (content is null || content.Length == 0)
            return false;

        if (string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
            return HasPrefix(content, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        if (string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
            return HasPrefix(content, [0xFF, 0xD8, 0xFF]);

        return false;
    }

    private static bool HasPrefix(byte[] content, byte[] signature)
        => content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature);
}
