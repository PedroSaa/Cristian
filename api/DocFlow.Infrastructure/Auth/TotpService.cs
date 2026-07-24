using DocFlow.Application.Common.Interfaces;
using OtpNet;

namespace DocFlow.Infrastructure.Auth;

public class TotpService : ITotpService
{
    private readonly ISecurityPolicyService _securityPolicy;

    public TotpService(ISecurityPolicyService securityPolicy)
    {
        _securityPolicy = securityPolicy;
    }

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateProvisioningUri(string secret, string email)
    {
        return $"otpauth://totp/DocFlow:{email}?secret={secret}&issuer=DocFlow";
    }

    public bool ValidateCode(string secret, string code)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        var windowSeconds = _securityPolicy.GetTotpWindowSeconds();
        // Clamp to minimum 90s so a persisted too-strict value does not
        // cause false negatives (clock drift tolerance floor).
        if (windowSeconds < 90) windowSeconds = 90;
        var steps = Math.Max(1, (int)Math.Ceiling(windowSeconds / 30.0));

        return totp.VerifyTotp(code, out _, new VerificationWindow(steps, steps));
    }
}
