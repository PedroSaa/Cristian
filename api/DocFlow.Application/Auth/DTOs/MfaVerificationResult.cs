namespace DocFlow.Application.Auth.DTOs;

public record MfaVerificationResult(bool Success, string? Error);
