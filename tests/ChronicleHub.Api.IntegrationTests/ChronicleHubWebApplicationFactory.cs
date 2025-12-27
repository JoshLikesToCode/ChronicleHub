using System.Net.Http.Json;
using ChronicleHub.Application.DTOs.Auth;
using ChronicleHub.Domain.Constants;
using ChronicleHub.Domain.Entities;
using ChronicleHub.Domain.Identity;
using ChronicleHub.Infrastructure.Persistence;
using ChronicleHub.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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

            // Configure relaxed password requirements for tests
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
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

    /// <summary>
    /// Creates a test user with a tenant and returns the access token and tenant ID
    /// </summary>
    public async Task<(string AccessToken, Guid TenantId, string UserId)> CreateTestUserAndLoginAsync(
        HttpClient client,
        string email = "test@example.com",
        string password = "TestPass123",
        string firstName = "Test",
        string lastName = "User",
        string tenantName = "Test Tenant")
    {
        // Register the user
        var registerRequest = new RegisterRequest(email, password, firstName, lastName, tenantName);
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResult>();

        if (authResult == null || !authResult.Success || string.IsNullOrEmpty(authResult.AccessToken))
        {
            throw new InvalidOperationException("Failed to register and login test user");
        }

        return (authResult.AccessToken, authResult.Tenant!.Id, authResult.User!.Id);
    }

    /// <summary>
    /// Creates an API key for a tenant
    /// </summary>
    public async Task<string> CreateTestApiKeyAsync(Guid tenantId, string name = "Test API Key")
    {
        using var scope = Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        var (apiKey, plaintextKey) = await apiKeyService.CreateApiKeyAsync(
            tenantId,
            name,
            DateTime.UtcNow.AddDays(30));

        return plaintextKey;
    }

    /// <summary>
    /// Creates a test tenant directly in the database
    /// </summary>
    public async Task<(Tenant Tenant, ApplicationUser User, string PlaintextApiKey)> CreateTestTenantWithUserAndApiKeyAsync(
        string email = "tenant@example.com",
        string password = "Password123",
        string tenantName = "Test Tenant")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronicleHubDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        // Create tenant
        var tenant = new Tenant(Guid.NewGuid(), tenantName, tenantName.ToLowerInvariant().Replace(" ", "-"));
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // Create user
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        if (!createUserResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
        }

        // Create user-tenant membership
        var userTenant = new UserTenant(Guid.NewGuid(), user.Id, tenant.Id, Roles.Owner);
        db.UserTenants.Add(userTenant);
        await db.SaveChangesAsync();

        // Create API key
        var (_, plaintextKey) = await apiKeyService.CreateApiKeyAsync(tenant.Id, "Test API Key", DateTime.UtcNow.AddDays(30));

        return (tenant, user, plaintextKey);
    }
}
