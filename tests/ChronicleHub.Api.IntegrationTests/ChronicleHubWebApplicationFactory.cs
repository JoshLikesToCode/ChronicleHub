using ChronicleHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChronicleHub.Api.IntegrationTests;

public class ChronicleHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string DatabaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ChronicleHubDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using in-memory database for testing (use shared database name)
            services.AddDbContext<ChronicleHubDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
            });
        });

        // Override configuration to ensure in-memory database is used
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // This will be used by the in-memory database
        });

        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the host
        var host = base.CreateHost(builder);

        // Create a scope and initialize the database
        using (var scope = host.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ChronicleHubDbContext>();

            // Ensure the database is created (works for in-memory)
            db.Database.EnsureCreated();
        }

        return host;
    }
}
