using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ChronicleHub.Api.Tests.Configuration;

public class DatabaseConfigurationTests
{
    [Fact]
    public void RunMigrationsOnStartup_DefaultsToTrue_WhenNotConfigured()
    {
        // Arrange
        var sut = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var result = sut.GetValue<bool>("Database:RunMigrationsOnStartup", true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RunMigrationsOnStartup_ReturnsFalse_WhenConfiguredAsFalse()
    {
        // Arrange
        var sut = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:RunMigrationsOnStartup", "false" }
            })
            .Build();

        // Act
        var result = sut.GetValue<bool>("Database:RunMigrationsOnStartup", true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RunMigrationsOnStartup_ReturnsTrue_WhenConfiguredAsTrue()
    {
        // Arrange
        var sut = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:RunMigrationsOnStartup", "true" }
            })
            .Build();

        // Act
        var result = sut.GetValue<bool>("Database:RunMigrationsOnStartup", true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RunMigrationsOnStartup_CanBeSetViaNestedConfiguration()
    {
        // Arrange - Simulating how nested configuration works
        var sut = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:RunMigrationsOnStartup", "false" }
            })
            .Build();

        // Act - Access via IConfiguration section navigation
        var databaseSection = sut.GetSection("Database");
        var result = databaseSection.GetValue<bool>("RunMigrationsOnStartup", true);

        // Assert
        result.Should().BeFalse("nested configuration should be accessible via section");
    }

    [Theory]
    [InlineData("False", false)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void RunMigrationsOnStartup_ParsesVariousBooleanFormats(string value, bool expected)
    {
        // Arrange
        var sut = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:RunMigrationsOnStartup", value }
            })
            .Build();

        // Act
        var result = sut.GetValue<bool>("Database:RunMigrationsOnStartup", true);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RunMigrationsOnStartup_UsesDefaultValue_WhenKeyNotPresent()
    {
        // Arrange
        var sut = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var result = sut.GetValue<bool>("Database:RunMigrationsOnStartup", true);

        // Assert
        result.Should().BeTrue("missing key should use default value");
    }
}
