namespace ChronicleHub.Application.DTOs.Auth;

public record AuthResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    UserInfo? User,
    TenantInfo? Tenant,
    string? ErrorMessage = null
);

public record UserInfo(
    string Id,
    string Email,
    string? FirstName,
    string? LastName
);

public record TenantInfo(
    Guid Id,
    string Name,
    string Slug,
    string Role
);
