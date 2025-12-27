using ChronicleHub.Domain.Identity;

namespace ChronicleHub.Infrastructure.Services;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user, Guid tenantId, string role);
    string GenerateRefreshToken();
    string HashToken(string token);
    bool VerifyTokenHash(string token, string hash);
}
