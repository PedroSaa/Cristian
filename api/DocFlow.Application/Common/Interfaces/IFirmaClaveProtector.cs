namespace DocFlow.Application.Common.Interfaces;

/// <summary>
/// Encrypts and decrypts the user signature PIN/password so it is never stored in plaintext at rest.
/// Dedicated to the signature feature: it uses the same authenticated-encryption mechanism as
/// <see cref="IMfaSecretProtector"/> but with its own key, so signature secrets are not coupled to MFA.
/// </summary>
public interface IFirmaClaveProtector
{
    /// <summary>Encrypts a plaintext signature PIN for storage. Throws if no key is configured.</summary>
    string Protect(string plaintext);

    /// <summary>Decrypts a stored signature PIN back to plaintext. Throws if tampered or no key.</summary>
    string Unprotect(string ciphertext);
}
