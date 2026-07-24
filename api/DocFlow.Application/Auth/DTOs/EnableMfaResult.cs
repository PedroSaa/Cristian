namespace DocFlow.Application.Auth.DTOs;

public record EnableMfaResult(string ProvisioningUri, string SecretKey);
