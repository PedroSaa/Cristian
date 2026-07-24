using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DocFlow.Infrastructure.Auth;

public class JwtProvider : IJwtProvider
{
    public const string AuthVersionClaimType = "auth_version";

    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly ISecurityPolicyService _securityPolicy;

    public JwtProvider(IConfiguration configuration, ISecurityPolicyService securityPolicy)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret no está configurado en appsettings.json.");
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _securityPolicy = securityPolicy;
    }

    private readonly int _mfaTokenExpirationMinutes = 5;

    public int RefreshTokenExpirationDays => _securityPolicy.GetRefreshTokenExpirationDays();

    public (string accessToken, string refreshToken, DateTime expiresAt) GenerateTokens(SeUsuari usuario, SePersonal personal, bool mfaCompleted = false)
        => GenerateTokensInternal(
            usuario.UsuarioId,
            personal.Correo,
            BuildFullName(personal),
            usuario.Rol?.Nombre ?? string.Empty,
            usuario.AuthSessionVersion,
            mfaCompleted);

    public string GenerateMfaToken(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("purpose", "mfa"),
        };

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_mfaTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateMfaToken(string mfaToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            var principal = handler.ValidateToken(mfaToken, parameters, out _);

            var purposeClaim = principal.FindFirst("purpose")?.Value;
            if (purposeClaim != "mfa")
                return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }
        catch
        {
            return null;
        }
    }

    private (string accessToken, string refreshToken, DateTime expiresAt) GenerateTokensInternal(
        Guid userId,
        string email,
        string nombre,
        string rol,
        int authSessionVersion,
        bool mfaCompleted)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_securityPolicy.GetJwtExpirationMinutes());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, rol),
            new Claim("nombre", nombre),
            new Claim(AuthVersionClaimType, authSessionVersion.ToString()),
            new Claim("mfa_completed", mfaCompleted ? "true" : "false"),
        };

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return (accessToken, refreshToken, expiresAt);
    }

    private static string BuildFullName(SePersonal personal)
        => string.Join(" ", new[] { personal.Nombres, personal.ApellidoPaterno, personal.ApellidoMaterno }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    public static int GetAuthSessionVersion(ClaimsPrincipal principal)
    {
        var claimValue = principal.FindFirst(AuthVersionClaimType)?.Value;
        return int.TryParse(claimValue, out var version) ? version : 0;
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        RandomNumberGenerator.Fill(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }
}
