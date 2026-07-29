using System.Security.Cryptography;
using System.Text;
using DocFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocFlow.Infrastructure.Auth;

/// <summary>
/// Protects the user signature PIN at rest using AES-GCM (authenticated encryption).
/// The 256-bit key is derived (SHA-256) from <c>Security:FirmaEncryptionKey</c> in configuration.
/// Stored format: base64( nonce[12] | tag[16] | ciphertext ). A random nonce per call means
/// the same PIN never yields the same ciphertext.
/// This mirrors the mechanism of <see cref="MfaSecretProtector"/> but keeps its own key so
/// signature secrets are not coupled to MFA. If no key is configured the service still constructs
/// (so the app boots), but Protect/Unprotect throw — using it without a key is a configuration error.
/// </summary>
public sealed class FirmaClaveProtector : IFirmaClaveProtector
{
    private const int NonceSize = 12; // AES-GCM standard nonce
    private const int TagSize = 16;   // AES-GCM authentication tag

    private readonly byte[]? _key;

    public FirmaClaveProtector(IConfiguration configuration)
    {
        var configured = configuration["Security:FirmaEncryptionKey"];
        _key = string.IsNullOrWhiteSpace(configured)
            ? null
            : SHA256.HashData(Encoding.UTF8.GetBytes(configured));
    }

    public string Protect(string plaintext)
    {
        var key = RequireKey();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipher = new byte[plainBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var combined = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, combined, NonceSize + TagSize, cipher.Length);

        return Convert.ToBase64String(combined);
    }

    public string Unprotect(string ciphertext)
    {
        var key = RequireKey();
        var combined = Convert.FromBase64String(ciphertext);
        if (combined.Length < NonceSize + TagSize)
            throw new CryptographicException("La clave de firma cifrada tiene un formato inválido.");

        var nonce = combined.AsSpan(0, NonceSize);
        var tag = combined.AsSpan(NonceSize, TagSize);
        var cipher = combined.AsSpan(NonceSize + TagSize);
        var plainBytes = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] RequireKey()
        => _key ?? throw new InvalidOperationException(
            "Security:FirmaEncryptionKey no está configurada; no se puede cifrar ni descifrar la clave de firma.");
}
