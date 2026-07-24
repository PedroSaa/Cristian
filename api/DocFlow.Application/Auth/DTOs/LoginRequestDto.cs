namespace DocFlow.Application.Auth.DTOs;

public record LoginRequestDto(string Identifier, string Password, string? MfaCode = null);
