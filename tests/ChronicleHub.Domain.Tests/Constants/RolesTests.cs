using ChronicleHub.Domain.Constants;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Constants;

public class RolesTests
{
    [Fact]
    public void Owner_ShouldHaveCorrectValue()
    {
        // Assert
        Roles.Owner.Should().Be("Owner");
    }

    [Fact]
    public void Admin_ShouldHaveCorrectValue()
    {
        // Assert
        Roles.Admin.Should().Be("Admin");
    }

    [Fact]
    public void Member_ShouldHaveCorrectValue()
    {
        // Assert
        Roles.Member.Should().Be("Member");
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Member)]
    public void IsValid_WithValidRole_ShouldReturnTrue(string role)
    {
        // Act
        var result = Roles.IsValid(role);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("owner")] // Case-sensitive
    [InlineData("ADMIN")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SuperAdmin")]
    [InlineData("Guest")]
    public void IsValid_WithInvalidRole_ShouldReturnFalse(string role)
    {
        // Act
        var result = Roles.IsValid(role);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = Roles.IsValid(null!);

        // Assert
        result.Should().BeFalse();
    }
}
