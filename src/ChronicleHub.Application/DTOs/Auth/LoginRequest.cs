namespace ChronicleHub.Application.DTOs.Auth;

public record LoginRequest(
    string Email,
    string Password,
    Guid? TenantId = null
);
