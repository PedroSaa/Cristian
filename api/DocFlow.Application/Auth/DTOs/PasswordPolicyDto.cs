namespace DocFlow.Application.Auth.DTOs;

/// <summary>
/// Effective password policy exposed to the client so the UI can validate live
/// in sync with what the backend actually enforces. Lowercase and digit are a fixed
/// floor (always required); the rest are admin-configurable.
/// </summary>
public record PasswordPolicyDto(
    int MinLength,
    bool RequireUppercase,
    bool RequireLowercase,
    bool RequireDigit,
    bool RequireSpecial);
