using ChronicleHub.Domain.Entities;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Entities;

public class TenantTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTenant()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Acme Corporation";
        var slug = "acme-corp";

        // Act
        var sut = new Tenant(id, name, slug);

        // Assert
        sut.Id.Should().Be(id);
        sut.Name.Should().Be(name);
        sut.Slug.Should().Be(slug);
        sut.IsActive.Should().BeTrue();
        sut.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        sut.DeactivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldSetCreatedAtUtc_ToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");

        var afterCreation = DateTime.UtcNow;

        // Assert
        sut.CreatedAtUtc.Should().BeOnOrAfter(beforeCreation);
        sut.CreatedAtUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_ShouldSetIsActive_ToTrue()
    {
        // Arrange & Act
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");

        // Assert
        sut.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateTenant()
    {
        // Arrange
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");
        var beforeDeactivation = DateTime.UtcNow;

        // Act
        sut.Deactivate();

        var afterDeactivation = DateTime.UtcNow;

        // Assert
        sut.IsActive.Should().BeFalse();
        sut.DeactivatedAtUtc.Should().NotBeNull();
        sut.DeactivatedAtUtc.Should().BeOnOrAfter(beforeDeactivation);
        sut.DeactivatedAtUtc.Should().BeOnOrBefore(afterDeactivation);
    }

    [Fact]
    public void Deactivate_WhenAlreadyDeactivated_ShouldRemainDeactivated()
    {
        // Arrange
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");
        sut.Deactivate();
        var firstDeactivationTime = sut.DeactivatedAtUtc;

        // Act
        sut.Deactivate();

        // Assert
        sut.IsActive.Should().BeFalse();
        sut.DeactivatedAtUtc.Should().Be(firstDeactivationTime);
    }

    [Fact]
    public void Reactivate_WhenDeactivated_ShouldReactivateTenant()
    {
        // Arrange
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");
        sut.Deactivate();

        // Act
        sut.Reactivate();

        // Assert
        sut.IsActive.Should().BeTrue();
        sut.DeactivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");

        // Act
        sut.Reactivate();

        // Assert
        sut.IsActive.Should().BeTrue();
        sut.DeactivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldHavePrivateSetters()
    {
        // Arrange & Act
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", "test-tenant");

        // Assert - Verify properties are immutable from outside
        var idProperty = typeof(Tenant).GetProperty(nameof(Tenant.Id));
        var nameProperty = typeof(Tenant).GetProperty(nameof(Tenant.Name));
        var slugProperty = typeof(Tenant).GetProperty(nameof(Tenant.Slug));
        var isActiveProperty = typeof(Tenant).GetProperty(nameof(Tenant.IsActive));
        var createdAtUtcProperty = typeof(Tenant).GetProperty(nameof(Tenant.CreatedAtUtc));
        var deactivatedAtUtcProperty = typeof(Tenant).GetProperty(nameof(Tenant.DeactivatedAtUtc));

        idProperty!.SetMethod.Should().NotBeNull();
        idProperty.SetMethod!.IsPrivate.Should().BeTrue();

        nameProperty!.SetMethod.Should().NotBeNull();
        nameProperty.SetMethod!.IsPrivate.Should().BeTrue();

        slugProperty!.SetMethod.Should().NotBeNull();
        slugProperty.SetMethod!.IsPrivate.Should().BeTrue();

        isActiveProperty!.SetMethod.Should().NotBeNull();
        isActiveProperty.SetMethod!.IsPrivate.Should().BeTrue();

        createdAtUtcProperty!.SetMethod.Should().NotBeNull();
        createdAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        deactivatedAtUtcProperty!.SetMethod.Should().NotBeNull();
        deactivatedAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceName_ShouldStillCreateTenant(string name)
    {
        // Arrange & Act
        var sut = new Tenant(Guid.NewGuid(), name, "slug");

        // Assert
        sut.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceSlug_ShouldStillCreateTenant(string slug)
    {
        // Arrange & Act
        var sut = new Tenant(Guid.NewGuid(), "Test Tenant", slug);

        // Assert
        sut.Slug.Should().Be(slug);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldAcceptEmptyGuid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var sut = new Tenant(emptyGuid, "Test Tenant", "test-tenant");

        // Assert
        sut.Id.Should().Be(Guid.Empty);
    }
}
