using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Reflection;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Auth;

public class JwtProviderTests
{
    private const string MfaCompletedClaimType = "mfa_completed";

    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "SuperSecretKeyForTesting1234567890123456",
                ["Jwt:Issuer"] = "DocFlow-Test",
                ["Jwt:Audience"] = "DocFlow-Client-Test"
            })
            .Build();
    }

    private static Mock<ISecurityPolicyService> CreatePolicyMock(int jwtExpiryMinutes = 15)
    {
        var mock = new Mock<ISecurityPolicyService>();
        mock.Setup(x => x.GetJwtExpirationMinutes()).Returns(jwtExpiryMinutes);
        return mock;
    }

    private static JwtProvider CreateProvider(int jwtExpiryMinutes = 15)
    {
        return new JwtProvider(CreateConfig(), CreatePolicyMock(jwtExpiryMinutes).Object);
    }

    private static (SeUsuari Usuari, SePersonal Personal) CreateUser(string fullName, string email, string roleName = "Administrador")
    {
        var usucod = new string(email.Split('@', 2)[0].Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var personal = SePersonal.Crear(usucod, fullName, correo: email);
        var usuari = SeUsuari.Crear(Guid.NewGuid(), usucod, "$2b$stored-hash");
        var role = new Rol(Guid.NewGuid(), roleName, roleName);

        usuari.VincularPersonal(personal);
        usuari.ActualizarAcceso(rolId: role.Id);
        typeof(SeUsuari).GetProperty(nameof(SeUsuari.Rol), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(usuari, role);

        return (usuari, personal);
    }

    [Fact]
    public void GenerateTokens_WhenMfaCompletedTrue_IncludesMfaCompletedTrueClaim()
    {
        // Arrange
        var provider = CreateProvider();
        var (usuari, personal) = CreateUser("Test User", "test@docflow.cl");

        // Act
        var (accessToken, _, _) = provider.GenerateTokens(usuari, personal, mfaCompleted: true);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        jwt.Claims.Should().Contain(c => c.Type == MfaCompletedClaimType && c.Value == "true");
    }

    [Fact]
    public void GenerateTokens_WhenMfaCompletedFalse_IncludesMfaCompletedFalseClaim()
    {
        // Arrange
        var provider = CreateProvider();
        var (usuari, personal) = CreateUser("Test User", "test@docflow.cl");

        // Act
        var (accessToken, _, _) = provider.GenerateTokens(usuari, personal, mfaCompleted: false);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        jwt.Claims.Should().Contain(c => c.Type == MfaCompletedClaimType && c.Value == "false");
    }

    [Fact]
    public void GenerateTokens_WhenMfaCompletedDefault_IncludesMfaCompletedFalseClaim()
    {
        // Arrange
        var provider = CreateProvider();
        var (usuari, personal) = CreateUser("Test User", "test@docflow.cl");

        // Act
        var (accessToken, _, _) = provider.GenerateTokens(usuari, personal);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        jwt.Claims.Should().Contain(c => c.Type == MfaCompletedClaimType && c.Value == "false");
    }

    [Fact]
    public void GenerateTokens_WhenMfaCompletedTrue_StillIncludesStandardClaims()
    {
        // Arrange
        var provider = CreateProvider();
        var (usuari, personal) = CreateUser("Admin User", "admin@docflow.cl");

        // Act
        var (accessToken, _, _) = provider.GenerateTokens(usuari, personal, mfaCompleted: true);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == usuari.UsuarioId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "admin@docflow.cl");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Administrador");
        jwt.Claims.Should().Contain(c => c.Type == "nombre" && c.Value == "Admin User");
    }

    [Fact]
    public void GenerateTokens_RefreshToken_IsOpaqueAndDoesNotEmbedUserIdBytes()
    {
        var provider = CreateProvider();
        var (usuari, personal) = CreateUser("Opaque User", "opaque@docflow.cl");

        var (_, refreshToken, _) = provider.GenerateTokens(usuari, personal);

        var rawBytes = Convert.FromBase64String(refreshToken);
        rawBytes.Length.Should().BeGreaterThanOrEqualTo(16);
        rawBytes[..16].Should().NotEqual(usuari.UsuarioId.ToByteArray());
    }

    [Fact]
    public void GenerateTokens_IncludesAuthVersionClaimFromUsuari()
    {
        var provider = CreateProvider();
        var (usuari, personal) = CreateUser("Versioned User", "versioned@docflow.cl");
        usuari.RevokeAuthSessions();

        var (accessToken, _, _) = provider.GenerateTokens(usuari, personal);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        jwt.Claims.Should().Contain(c => c.Type == "auth_version" && c.Value == "1");
    }

    [Fact]
    public void GetAuthSessionVersion_FromPrincipal_UsesLegacyFallbackWhenMissingClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "Bearer"));

        JwtProvider.GetAuthSessionVersion(principal).Should().Be(0);
    }

    [Fact]
    public void GenerateTokens_ExpiryFromSecurityPolicyService()
    {
        // Arrange
        var provider = CreateProvider(jwtExpiryMinutes: 120);
        var (usuari, personal) = CreateUser("Test User", "test@docflow.cl");

        // Act
        var (_, _, expiresAt) = provider.GenerateTokens(usuari, personal);

        // Assert
        var expectedExpiry = DateTime.UtcNow.AddMinutes(120);
        expiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateTokens_ExpiryRespectsDifferentConfigValues()
    {
        // Arrange
        var provider = CreateProvider(jwtExpiryMinutes: 60);
        var (usuari, personal) = CreateUser("Test User", "test@docflow.cl");

        // Act
        var (_, _, expiresAt) = provider.GenerateTokens(usuari, personal);

        // Assert
        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);
        expiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RefreshTokenExpirationDays_ReadsFromSecurityPolicy()
    {
        // Arrange — the refresh token TTL is no longer hardcoded; it comes from the policy.
        var policy = new Mock<ISecurityPolicyService>();
        policy.Setup(x => x.GetRefreshTokenExpirationDays()).Returns(14);
        var provider = new JwtProvider(CreateConfig(), policy.Object);

        // Act + Assert
        provider.RefreshTokenExpirationDays.Should().Be(14);
        policy.Verify(x => x.GetRefreshTokenExpirationDays(), Times.AtLeastOnce);
    }
}
