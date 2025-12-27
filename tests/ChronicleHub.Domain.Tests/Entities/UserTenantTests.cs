using ChronicleHub.Domain.Constants;
using ChronicleHub.Domain.Entities;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Entities;

public class UserTenantTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateUserTenant()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user-123";
        var tenantId = Guid.NewGuid();
        var role = Roles.Admin;

        // Act
        var sut = new UserTenant(id, userId, tenantId, role);

        // Assert
        sut.Id.Should().Be(id);
        sut.UserId.Should().Be(userId);
        sut.TenantId.Should().Be(tenantId);
        sut.Role.Should().Be(role);
        sut.JoinedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_ShouldSetJoinedAtUtc_ToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var sut = new UserTenant(Guid.NewGuid(), "user-123", Guid.NewGuid(), Roles.Member);

        var afterCreation = DateTime.UtcNow;

        // Assert
        sut.JoinedAtUtc.Should().BeOnOrAfter(beforeCreation);
        sut.JoinedAtUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Member)]
    public void Constructor_WithValidRole_ShouldAcceptRole(string role)
    {
        // Arrange & Act
        var sut = new UserTenant(Guid.NewGuid(), "user-123", Guid.NewGuid(), role);

        // Assert
        sut.Role.Should().Be(role);
    }

    [Fact]
    public void UpdateRole_WithNewRole_ShouldUpdateRole()
    {
        // Arrange
        var sut = new UserTenant(Guid.NewGuid(), "user-123", Guid.NewGuid(), Roles.Member);

        // Act
        sut.UpdateRole(Roles.Admin);

        // Assert
        sut.Role.Should().Be(Roles.Admin);
    }

    [Fact]
    public void UpdateRole_MultipleTimes_ShouldUpdateToLatestRole()
    {
        // Arrange
        var sut = new UserTenant(Guid.NewGuid(), "user-123", Guid.NewGuid(), Roles.Member);

        // Act
        sut.UpdateRole(Roles.Admin);
        sut.UpdateRole(Roles.Owner);

        // Assert
        sut.Role.Should().Be(Roles.Owner);
    }

    [Fact]
    public void UpdateRole_ToSameRole_ShouldRemainUnchanged()
    {
        // Arrange
        var sut = new UserTenant(Guid.NewGuid(), "user-123", Guid.NewGuid(), Roles.Admin);

        // Act
        sut.UpdateRole(Roles.Admin);

        // Assert
        sut.Role.Should().Be(Roles.Admin);
    }

    [Fact]
    public void Properties_ShouldHavePrivateSetters()
    {
        // Arrange & Act
        var sut = new UserTenant(Guid.NewGuid(), "user-123", Guid.NewGuid(), Roles.Member);

        // Assert - Verify properties are immutable from outside
        var idProperty = typeof(UserTenant).GetProperty(nameof(UserTenant.Id));
        var userIdProperty = typeof(UserTenant).GetProperty(nameof(UserTenant.UserId));
        var tenantIdProperty = typeof(UserTenant).GetProperty(nameof(UserTenant.TenantId));
        var roleProperty = typeof(UserTenant).GetProperty(nameof(UserTenant.Role));
        var joinedAtUtcProperty = typeof(UserTenant).GetProperty(nameof(UserTenant.JoinedAtUtc));

        idProperty!.SetMethod.Should().NotBeNull();
        idProperty.SetMethod!.IsPrivate.Should().BeTrue();

        userIdProperty!.SetMethod.Should().NotBeNull();
        userIdProperty.SetMethod!.IsPrivate.Should().BeTrue();

        tenantIdProperty!.SetMethod.Should().NotBeNull();
        tenantIdProperty.SetMethod!.IsPrivate.Should().BeTrue();

        roleProperty!.SetMethod.Should().NotBeNull();
        roleProperty.SetMethod!.IsPrivate.Should().BeTrue();

        joinedAtUtcProperty!.SetMethod.Should().NotBeNull();
        joinedAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceUserId_ShouldStillCreateUserTenant(string userId)
    {
        // Arrange & Act
        var sut = new UserTenant(Guid.NewGuid(), userId, Guid.NewGuid(), Roles.Member);

        // Assert
        sut.UserId.Should().Be(userId);
    }

    [Fact]
    public void Constructor_WithEmptyGuidIds_ShouldAcceptEmptyGuids()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var sut = new UserTenant(emptyGuid, "user-123", emptyGuid, Roles.Member);

        // Assert
        sut.Id.Should().Be(Guid.Empty);
        sut.TenantId.Should().Be(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InvalidRole")]
    public void UpdateRole_WithAnyString_ShouldAcceptRole(string newRole)
    {
        // Arrange
        var sut = new UserTenant(Guid.NewGuid(), "user-123", Guid.NewGuid(), Roles.Member);

        // Act
        sut.UpdateRole(newRole);

        // Assert
        sut.Role.Should().Be(newRole);
    }
}
