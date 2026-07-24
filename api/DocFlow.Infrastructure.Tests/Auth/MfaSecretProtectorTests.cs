using System.Security.Cryptography;
using DocFlow.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Auth;

public class MfaSecretProtectorTests
{
    private const string TestKey = "unit-test-mfa-encryption-key-please-change-0123456789";

    private static MfaSecretProtector CreateSut(string? key = TestKey)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:MfaEncryptionKey"] = key,
            })
            .Build();
        return new MfaSecretProtector(config);
    }

    [Fact]
    public void Unprotect_OfProtectedValue_ReturnsOriginal()
    {
        var sut = CreateSut();
        const string secret = "JBSWY3DPEHPK3PXP"; // sample base32 TOTP secret

        var roundTripped = sut.Unprotect(sut.Protect(secret));

        roundTripped.Should().Be(secret);
    }

    [Fact]
    public void Protect_OutputDiffersFromPlaintext()
    {
        var sut = CreateSut();
        const string secret = "JBSWY3DPEHPK3PXP";

        var protectedValue = sut.Protect(secret);

        protectedValue.Should().NotBe(secret);
        protectedValue.Should().NotContain(secret);
    }

    [Fact]
    public void Protect_SameInputTwice_ProducesDifferentCiphertext()
    {
        var sut = CreateSut();
        const string secret = "JBSWY3DPEHPK3PXP";

        var first = sut.Protect(secret);
        var second = sut.Protect(secret);

        // Random nonce per call → different ciphertext, but both decrypt back.
        first.Should().NotBe(second);
        sut.Unprotect(first).Should().Be(secret);
        sut.Unprotect(second).Should().Be(secret);
    }

    [Fact]
    public void Unprotect_TamperedCiphertext_Throws()
    {
        var sut = CreateSut();
        var protectedValue = sut.Protect("JBSWY3DPEHPK3PXP");
        var bytes = Convert.FromBase64String(protectedValue);
        bytes[^1] ^= 0xFF; // flip the last byte → auth tag check must fail
        var tampered = Convert.ToBase64String(bytes);

        var act = () => sut.Unprotect(tampered);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Unprotect_Garbage_Throws()
    {
        var sut = CreateSut();

        var act = () => sut.Unprotect("not-valid-base64-or-cipher!!!");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Protect_WhenKeyNotConfigured_Throws()
    {
        var sut = CreateSut(key: null);

        var act = () => sut.Protect("JBSWY3DPEHPK3PXP");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DifferentKeys_CannotDecryptEachOther()
    {
        var a = CreateSut("key-alpha-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var b = CreateSut("key-beta-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

        var protectedByA = a.Protect("JBSWY3DPEHPK3PXP");

        var act = () => b.Unprotect(protectedByA);
        act.Should().Throw<CryptographicException>();
    }
}
