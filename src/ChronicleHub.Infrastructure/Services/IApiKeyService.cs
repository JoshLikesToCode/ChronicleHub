using ChronicleHub.Domain.Entities;

namespace ChronicleHub.Infrastructure.Services;

public interface IApiKeyService
{
    Task<(ApiKey key, string plainTextKey)> CreateApiKeyAsync(Guid tenantId, string name, DateTime? expiresAtUtc = null, CancellationToken ct = default);
    Task<ApiKey?> ValidateApiKeyAsync(string plainTextKey, CancellationToken ct = default);
    Task RevokeApiKeyAsync(Guid keyId, CancellationToken ct = default);
}
