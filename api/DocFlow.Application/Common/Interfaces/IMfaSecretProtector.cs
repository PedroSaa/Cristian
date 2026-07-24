namespace DocFlow.Application.Common.Interfaces;

/// <summary>
/// Encrypts and decrypts the TOTP MFA secret so it is never stored in plaintext at rest.
/// Implemented with authenticated encryption (AES-GCM) keyed from configuration.
/// </summary>
public interface IMfaSecretProtector
{
    /// <summary>Encrypts a plaintext MFA secret for storage. Throws if no key is configured.</summary>
    string Protect(string plaintext);

    /// <summary>Decrypts a stored MFA secret back to plaintext. Throws if tampered or no key.</summary>
    string Unprotect(string ciphertext);
}
