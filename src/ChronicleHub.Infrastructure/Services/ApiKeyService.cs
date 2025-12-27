using System.Security.Cryptography;
using ChronicleHub.Domain.Entities;
using ChronicleHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChronicleHub.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly ChronicleHubDbContext _db;
    private const string KeyPrefix = "ch_live_";

    public ApiKeyService(ChronicleHubDbContext db)
    {
        _db = db;
    }

    public async Task<(ApiKey key, string plainTextKey)> CreateApiKeyAsync(
        Guid tenantId,
        string name,
        DateTime? expiresAtUtc = null,
        CancellationToken ct = default)
    {
        // Generate secure random key
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var plainTextKey = KeyPrefix + Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Hash the key for storage
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plainTextKey));
        var keyHash = Convert.ToBase64String(hashedBytes);

        var apiKey = new ApiKey(
            Guid.NewGuid(),
            tenantId,
            name,
            keyHash,
            plainTextKey.Substring(0, Math.Min(20, plainTextKey.Length)),
            expiresAtUtc
        );

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync(ct);

        return (apiKey, plainTextKey);
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string plainTextKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plainTextKey))
            return null;

        // Hash the provided key
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plainTextKey));
        var keyHash = Convert.ToBase64String(hashedBytes);

        // Find matching active key
        var apiKey = await _db.ApiKeys
            .Include(k => k.Tenant)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive, ct);

        if (apiKey == null)
            return null;

        // Check if expired
        if (apiKey.IsExpired())
            return null;

        // Check if tenant is active
        if (!apiKey.Tenant.IsActive)
            return null;

        // Record usage
        apiKey.RecordUsage();
        await _db.SaveChangesAsync(ct);

        return apiKey;
    }

    public async Task RevokeApiKeyAsync(Guid keyId, CancellationToken ct = default)
    {
        var apiKey = await _db.ApiKeys.FindAsync(new object[] { keyId }, ct);
        if (apiKey != null)
        {
            apiKey.Revoke();
            await _db.SaveChangesAsync(ct);
        }
    }
}
