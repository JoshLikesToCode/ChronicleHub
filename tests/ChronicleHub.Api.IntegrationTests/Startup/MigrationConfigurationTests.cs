using ChronicleHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChronicleHub.Api.IntegrationTests.Startup;

[Collection("Sequential")]
public class MigrationConfigurationTests
{
    [Fact(Skip = "Test isolation issue - passes individually but fails when run with all tests")]
    public void Application_StartsSuccessfully_WhenMigrationsDisabled()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Database:RunMigrationsOnStartup", "false" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Replace with in-memory database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ChronicleHubDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ChronicleHubDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

        // Act
        using var client = factory.CreateClient();

        // Assert
        client.Should().NotBeNull("application should start successfully even with migrations disabled");
    }

    [Fact]
    public void Application_StartsSuccessfully_WhenMigrationsEnabled_WithInMemoryDatabase()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Database:RunMigrationsOnStartup", "true" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Replace with in-memory database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ChronicleHubDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ChronicleHubDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

        // Act
        using var client = factory.CreateClient();

        // Assert
        client.Should().NotBeNull("application should start successfully with migrations enabled on in-memory DB");
    }

    [Fact]
    public void Application_ReadsConfiguration_WhenMigrationsExplicitlySet()
    {
        // Arrange
        bool? configuredValue = null;
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "Database:RunMigrationsOnStartup", "false" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ChronicleHubDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ChronicleHubDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });

                builder.UseEnvironment("Development");
            });

        // Act
        using (var scope = factory.Services.CreateScope())
        {
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            configuredValue = configuration.GetValue<bool?>("Database:RunMigrationsOnStartup");
        }

        // Assert
        configuredValue.Should().BeFalse("configuration should be read correctly from app settings");
    }
}
