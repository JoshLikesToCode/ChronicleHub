using ChronicleHub.Domain.Entities;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Entities;

public class ApiKeyTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateApiKey()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var name = "Production API Key";
        var keyHash = "hashed-key-value";
        var keyPrefix = "ch_live_abc";
        var expiresAt = DateTime.UtcNow.AddDays(365);

        // Act
        var sut = new ApiKey(id, tenantId, name, keyHash, keyPrefix, expiresAt);

        // Assert
        sut.Id.Should().Be(id);
        sut.TenantId.Should().Be(tenantId);
        sut.Name.Should().Be(name);
        sut.KeyHash.Should().Be(keyHash);
        sut.KeyPrefix.Should().Be(keyPrefix);
        sut.IsActive.Should().BeTrue();
        sut.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        sut.ExpiresAtUtc.Should().Be(expiresAt);
        sut.LastUsedAtUtc.Should().BeNull();
        sut.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldSetCreatedAtUtc_ToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));

        var afterCreation = DateTime.UtcNow;

        // Assert
        sut.CreatedAtUtc.Should().BeOnOrAfter(beforeCreation);
        sut.CreatedAtUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_ShouldSetIsActive_ToTrue()
    {
        // Arrange & Act
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));

        // Assert
        sut.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RecordUsage_ShouldUpdateLastUsedAtUtc()
    {
        // Arrange
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));
        var beforeUsage = DateTime.UtcNow;

        // Act
        sut.RecordUsage();

        var afterUsage = DateTime.UtcNow;

        // Assert
        sut.LastUsedAtUtc.Should().NotBeNull();
        sut.LastUsedAtUtc.Should().BeOnOrAfter(beforeUsage);
        sut.LastUsedAtUtc.Should().BeOnOrBefore(afterUsage);
    }

    [Fact]
    public void RecordUsage_CalledMultipleTimes_ShouldUpdateToLatestUsage()
    {
        // Arrange
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));

        // Act
        sut.RecordUsage();
        var firstUsage = sut.LastUsedAtUtc;

        System.Threading.Thread.Sleep(10); // Small delay to ensure different timestamps

        sut.RecordUsage();
        var secondUsage = sut.LastUsedAtUtc;

        // Assert
        secondUsage.Should().BeAfter(firstUsage!.Value);
    }

    [Fact]
    public void Revoke_WhenActive_ShouldRevokeApiKey()
    {
        // Arrange
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));
        var beforeRevocation = DateTime.UtcNow;

        // Act
        sut.Revoke();

        var afterRevocation = DateTime.UtcNow;

        // Assert
        sut.IsActive.Should().BeFalse();
        sut.RevokedAtUtc.Should().NotBeNull();
        sut.RevokedAtUtc.Should().BeOnOrAfter(beforeRevocation);
        sut.RevokedAtUtc.Should().BeOnOrBefore(afterRevocation);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldRemainRevoked()
    {
        // Arrange
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));
        sut.Revoke();
        var firstRevocationTime = sut.RevokedAtUtc;

        // Act
        sut.Revoke();

        // Assert
        sut.IsActive.Should().BeFalse();
        sut.RevokedAtUtc.Should().Be(firstRevocationTime);
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));

        // Act
        var result = sut.IsExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        // Arrange
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(-1)); // Expired yesterday

        // Act
        var result = sut.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExactlyAtExpiryTime_ShouldReturnTrue()
    {
        // Arrange
        var expiryTime = DateTime.UtcNow;
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            expiryTime);

        System.Threading.Thread.Sleep(10); // Small delay to ensure we're past expiry

        // Act
        var result = sut.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Properties_ShouldHavePrivateSetters()
    {
        // Arrange & Act
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));

        // Assert - Verify properties are immutable from outside
        var idProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.Id));
        var tenantIdProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.TenantId));
        var nameProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.Name));
        var keyHashProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.KeyHash));
        var keyPrefixProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.KeyPrefix));
        var isActiveProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.IsActive));
        var createdAtUtcProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.CreatedAtUtc));
        var expiresAtUtcProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.ExpiresAtUtc));
        var lastUsedAtUtcProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.LastUsedAtUtc));
        var revokedAtUtcProperty = typeof(ApiKey).GetProperty(nameof(ApiKey.RevokedAtUtc));

        idProperty!.SetMethod.Should().NotBeNull();
        idProperty.SetMethod!.IsPrivate.Should().BeTrue();

        tenantIdProperty!.SetMethod.Should().NotBeNull();
        tenantIdProperty.SetMethod!.IsPrivate.Should().BeTrue();

        nameProperty!.SetMethod.Should().NotBeNull();
        nameProperty.SetMethod!.IsPrivate.Should().BeTrue();

        keyHashProperty!.SetMethod.Should().NotBeNull();
        keyHashProperty.SetMethod!.IsPrivate.Should().BeTrue();

        keyPrefixProperty!.SetMethod.Should().NotBeNull();
        keyPrefixProperty.SetMethod!.IsPrivate.Should().BeTrue();

        isActiveProperty!.SetMethod.Should().NotBeNull();
        isActiveProperty.SetMethod!.IsPrivate.Should().BeTrue();

        createdAtUtcProperty!.SetMethod.Should().NotBeNull();
        createdAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        expiresAtUtcProperty!.SetMethod.Should().NotBeNull();
        expiresAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        lastUsedAtUtcProperty!.SetMethod.Should().NotBeNull();
        lastUsedAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        revokedAtUtcProperty!.SetMethod.Should().NotBeNull();
        revokedAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceName_ShouldStillCreateApiKey(string name)
    {
        // Arrange & Act
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));

        // Assert
        sut.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithPastExpiryDate_ShouldCreateExpiredKey()
    {
        // Arrange
        var pastExpiry = DateTime.UtcNow.AddDays(-10);

        // Act
        var sut = new ApiKey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Expired Key",
            "hash",
            "ch_live_test",
            pastExpiry);

        // Assert
        sut.ExpiresAtUtc.Should().Be(pastExpiry);
        sut.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyGuidIds_ShouldAcceptEmptyGuids()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var sut = new ApiKey(
            emptyGuid,
            emptyGuid,
            "Test Key",
            "hash",
            "ch_live_test",
            DateTime.UtcNow.AddDays(30));

        // Assert
        sut.Id.Should().Be(Guid.Empty);
        sut.TenantId.Should().Be(Guid.Empty);
    }
}
