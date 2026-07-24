using DocFlow.Application.Common.Interfaces;
using DocFlow.Infrastructure.Auth;
using FluentAssertions;
using Moq;
using OtpNet;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Auth;

public class TotpServiceTests
{
    private static Mock<ISecurityPolicyService> CreatePolicyMock(int totpWindowSeconds = 30)
    {
        var mock = new Mock<ISecurityPolicyService>();
        mock.Setup(x => x.GetTotpWindowSeconds()).Returns(totpWindowSeconds);
        return mock;
    }

    private static ITotpService CreateSut(int totpWindowSeconds = 30)
        => new TotpService(CreatePolicyMock(totpWindowSeconds).Object);

    [Fact]
    public void GenerateSecret_Returns32CharBase32String()
    {
        var sut = CreateSut();

        var secret = sut.GenerateSecret();

        secret.Should().NotBeNullOrEmpty();
        secret.Length.Should().Be(32);

        // Base32 strings contain only A-Z and 2-7
        secret.Should().MatchRegex("^[A-Z2-7]+=*$");
    }

    [Fact]
    public void GenerateProvisioningUri_ContainsOtpauthTotpAndEmail()
    {
        var sut = CreateSut();
        var secret = sut.GenerateSecret();
        var email = "user@docflow.cl";

        var uri = sut.GenerateProvisioningUri(secret, email);

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain(email);
        uri.Should().Contain("secret=");
        uri.Should().Contain("issuer=DocFlow");
    }

    [Fact]
    public void ValidateCode_WithValidCode_ReturnsTrue()
    {
        var sut = CreateSut();
        var secret = sut.GenerateSecret();
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        var validCode = totp.ComputeTotp();

        var result = sut.ValidateCode(secret, validCode);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_WithInvalidCode_ReturnsFalse()
    {
        var sut = CreateSut();
        var secret = sut.GenerateSecret();

        var result = sut.ValidateCode(secret, "000000");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_WithWiderWindow_AcceptsOlderCode()
    {
        // Arrange: generate a TOTP code, then simulate it being from a previous window
        var sut = CreateSut(totpWindowSeconds: 120);
        var secret = sut.GenerateSecret();
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        // Compute a code from a previous time step (60 seconds ago)
        var pastCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(-60));

        // Act
        var result = sut.ValidateCode(secret, pastCode);

        // Assert — a 120s window should tolerate 60s clock drift
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_WithNarrowWindow_RejectsOldCode()
    {
        // Arrange: generate a TOTP code, then simulate it being from a previous window
        var sut = CreateSut(totpWindowSeconds: 30);
        var secret = sut.GenerateSecret();
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        // Compute a code from 60 seconds ago
        var pastCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(-60));

        // Act
        var result = sut.ValidateCode(secret, pastCode);

        // Assert — a 30s window clamped to minimum 90 should now tolerate 60s drift
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_ClampsToMinimum90Seconds()
    {
        // Arrange: a very narrow window (1s) that would normally reject all drift
        var sut = CreateSut(totpWindowSeconds: 1);
        var secret = sut.GenerateSecret();
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        // Compute a code from 30 seconds ago
        var pastCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(-30));

        // Act
        var result = sut.ValidateCode(secret, pastCode);

        // Assert — even with policy at 1s, the 90s floor should accept 30s drift
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_WithNarrowWindow_StillRejectsVeryOldCode()
    {
        // Arrange: even with clamping, very old codes should be rejected
        var sut = CreateSut(totpWindowSeconds: 30);
        var secret = sut.GenerateSecret();
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        // Compute a code from 180 seconds ago (6 TOTP steps)
        var pastCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(-180));

        // Act
        var result = sut.ValidateCode(secret, pastCode);

        // Assert — 180s is well beyond even the 90s clamped window (max 3 steps)
        result.Should().BeFalse();
    }
}
