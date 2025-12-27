using ChronicleHub.Domain.Entities;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateRefreshToken()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user-123";
        var tokenHash = "hashed-token-value";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var createdByIp = "192.168.1.1";

        // Act
        var sut = new RefreshToken(id, userId, tokenHash, expiresAt, createdByIp);

        // Assert
        sut.Id.Should().Be(id);
        sut.UserId.Should().Be(userId);
        sut.TokenHash.Should().Be(tokenHash);
        sut.ExpiresAtUtc.Should().Be(expiresAt);
        sut.CreatedByIp.Should().Be(createdByIp);
        sut.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        sut.RevokedAtUtc.Should().BeNull();
        sut.RevokedByIp.Should().BeNull();
        sut.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldSetCreatedAtUtc_ToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");

        var afterCreation = DateTime.UtcNow;

        // Assert
        sut.CreatedAtUtc.Should().BeOnOrAfter(beforeCreation);
        sut.CreatedAtUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");

        // Act
        var result = sut.IsExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(-1), // Expired yesterday
            "192.168.1.1");

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
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            expiryTime,
            "192.168.1.1");

        System.Threading.Thread.Sleep(10); // Small delay to ensure we're past expiry

        // Act
        var result = sut.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenNotExpiredAndNotRevoked_ShouldReturnTrue()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");

        // Act
        var result = sut.IsActive();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(-1),
            "192.168.1.1");

        // Act
        var result = sut.IsActive();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");
        sut.Revoke("192.168.1.2", null);

        // Act
        var result = sut.IsActive();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithoutReplacementToken_ShouldRevokeToken()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");
        var revokedByIp = "192.168.1.2";
        var beforeRevocation = DateTime.UtcNow;

        // Act
        sut.Revoke(revokedByIp, null);

        var afterRevocation = DateTime.UtcNow;

        // Assert
        sut.RevokedAtUtc.Should().NotBeNull();
        sut.RevokedAtUtc.Should().BeOnOrAfter(beforeRevocation);
        sut.RevokedAtUtc.Should().BeOnOrBefore(afterRevocation);
        sut.RevokedByIp.Should().Be(revokedByIp);
        sut.ReplacedByTokenHash.Should().BeNull();
        sut.IsActive().Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithReplacementToken_ShouldRevokeAndRecordReplacement()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "old-hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");
        var revokedByIp = "192.168.1.1";
        var replacementHash = "new-hash";
        var beforeRevocation = DateTime.UtcNow;

        // Act
        sut.Revoke(revokedByIp, replacementHash);

        var afterRevocation = DateTime.UtcNow;

        // Assert
        sut.RevokedAtUtc.Should().NotBeNull();
        sut.RevokedAtUtc.Should().BeOnOrAfter(beforeRevocation);
        sut.RevokedAtUtc.Should().BeOnOrBefore(afterRevocation);
        sut.RevokedByIp.Should().Be(revokedByIp);
        sut.ReplacedByTokenHash.Should().Be(replacementHash);
        sut.IsActive().Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldRemainRevoked()
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");
        sut.Revoke("192.168.1.2", null);
        var firstRevocationTime = sut.RevokedAtUtc;
        var firstRevokedByIp = sut.RevokedByIp;

        // Act
        sut.Revoke("192.168.1.3", "replacement-hash");

        // Assert
        sut.RevokedAtUtc.Should().Be(firstRevocationTime);
        sut.RevokedByIp.Should().Be(firstRevokedByIp);
        sut.ReplacedByTokenHash.Should().BeNull();
        sut.IsActive().Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldHavePrivateSetters()
    {
        // Arrange & Act
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");

        // Assert - Verify properties are immutable from outside
        var idProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.Id));
        var userIdProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.UserId));
        var tokenHashProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.TokenHash));
        var expiresAtUtcProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.ExpiresAtUtc));
        var createdAtUtcProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.CreatedAtUtc));
        var revokedAtUtcProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.RevokedAtUtc));
        var revokedByIpProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.RevokedByIp));
        var replacedByTokenHashProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.ReplacedByTokenHash));
        var createdByIpProperty = typeof(RefreshToken).GetProperty(nameof(RefreshToken.CreatedByIp));

        idProperty!.SetMethod.Should().NotBeNull();
        idProperty.SetMethod!.IsPrivate.Should().BeTrue();

        userIdProperty!.SetMethod.Should().NotBeNull();
        userIdProperty.SetMethod!.IsPrivate.Should().BeTrue();

        tokenHashProperty!.SetMethod.Should().NotBeNull();
        tokenHashProperty.SetMethod!.IsPrivate.Should().BeTrue();

        expiresAtUtcProperty!.SetMethod.Should().NotBeNull();
        expiresAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        createdAtUtcProperty!.SetMethod.Should().NotBeNull();
        createdAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        revokedAtUtcProperty!.SetMethod.Should().NotBeNull();
        revokedAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        revokedByIpProperty!.SetMethod.Should().NotBeNull();
        revokedByIpProperty.SetMethod!.IsPrivate.Should().BeTrue();

        replacedByTokenHashProperty!.SetMethod.Should().NotBeNull();
        replacedByTokenHashProperty.SetMethod!.IsPrivate.Should().BeTrue();

        createdByIpProperty!.SetMethod.Should().NotBeNull();
        createdByIpProperty.SetMethod!.IsPrivate.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceUserId_ShouldStillCreateRefreshToken(string userId)
    {
        // Arrange & Act
        var sut = new RefreshToken(
            Guid.NewGuid(),
            userId,
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");

        // Assert
        sut.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNullIpAddress_ShouldStillCreateRefreshToken(string? ipAddress)
    {
        // Arrange & Act
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            ipAddress);

        // Assert
        sut.CreatedByIp.Should().Be(ipAddress);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldAcceptEmptyGuid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var sut = new RefreshToken(
            emptyGuid,
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");

        // Assert
        sut.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Constructor_WithPastExpiryDate_ShouldCreateExpiredToken()
    {
        // Arrange
        var pastExpiry = DateTime.UtcNow.AddDays(-10);

        // Act
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            pastExpiry,
            "192.168.1.1");

        // Assert
        sut.ExpiresAtUtc.Should().Be(pastExpiry);
        sut.IsExpired().Should().BeTrue();
        sut.IsActive().Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Revoke_WithEmptyOrNullIpAddress_ShouldStillRevoke(string? ipAddress)
    {
        // Arrange
        var sut = new RefreshToken(
            Guid.NewGuid(),
            "user-123",
            "hash",
            DateTime.UtcNow.AddDays(7),
            "192.168.1.1");

        // Act
        sut.Revoke(ipAddress, null);

        // Assert
        sut.RevokedByIp.Should().Be(ipAddress);
        sut.IsActive().Should().BeFalse();
    }
}
