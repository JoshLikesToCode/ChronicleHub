using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChronicleHub.Api.Contracts.Common;
using ChronicleHub.Api.Contracts.Events;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ChronicleHub.Api.IntegrationTests;

[Collection("IntegrationTests")]
public class TenantIsolationTests : IClassFixture<ChronicleHubWebApplicationFactory>, IAsyncDisposable
{
    private readonly ChronicleHubWebApplicationFactory _factory;

    public TenantIsolationTests(ChronicleHubWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up database after each test to prevent interference
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronicleHub.Infrastructure.Persistence.ChronicleHubDbContext>();

        // Clear all data from the database
        db.Events.RemoveRange(db.Events);
        db.ApiKeys.RemoveRange(db.ApiKeys);
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        db.UserTenants.RemoveRange(db.UserTenants);
        db.Tenants.RemoveRange(db.Tenants);
        // Note: ASP.NET Identity tables (Users, Roles, etc.) are managed by UserManager
        // We need to remove users through UserManager to maintain referential integrity
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ChronicleHub.Domain.Identity.ApplicationUser>>();
        var users = db.Users.ToList();
        foreach (var user in users)
        {
            await userManager.DeleteAsync(user);
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Events_UsersFromDifferentTenants_ShouldOnlySeeTheirOwnEvents()
    {
        // Arrange - Create two separate tenants with users
        var testId = Guid.NewGuid();
        var client1 = _factory.CreateClient();
        var (accessToken1, tenantId1, _) = await _factory.CreateTestUserAndLoginAsync(
            client1, $"tenant1-{testId}@example.com", "Pass12344", "User", "One", $"Tenant One {testId}");

        var client2 = _factory.CreateClient();
        var (accessToken2, tenantId2, _) = await _factory.CreateTestUserAndLoginAsync(
            client2, $"tenant2-{testId}@example.com", "Pass12344", "User", "Two", $"Tenant Two {testId}");

        // Get API keys for both tenants
        var apiKey1 = await _factory.CreateTestApiKeyAsync(tenantId1);
        var apiKey2 = await _factory.CreateTestApiKeyAsync(tenantId2);

        // Create events for Tenant 1
        var apiClient1 = _factory.CreateClient();
        apiClient1.DefaultRequestHeaders.Add("X-Api-Key", apiKey1);
        await apiClient1.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "user_action",
            Source: "tenant1-app",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"action\": \"tenant1_event_1\"}").RootElement
        ));
        await apiClient1.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "user_action",
            Source: "tenant1-app",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"action\": \"tenant1_event_2\"}").RootElement
        ));

        // Create events for Tenant 2
        var apiClient2 = _factory.CreateClient();
        apiClient2.DefaultRequestHeaders.Add("X-Api-Key", apiKey2);
        await apiClient2.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "user_action",
            Source: "tenant2-app",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"action\": \"tenant2_event_1\"}").RootElement
        ));
        await apiClient2.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "user_action",
            Source: "tenant2-app",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"action\": \"tenant2_event_2\"}").RootElement
        ));
        await apiClient2.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "user_action",
            Source: "tenant2-app",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"action\": \"tenant2_event_3\"}").RootElement
        ));

        // Act - Tenant 1 user queries events
        var authClient1 = _factory.CreateClient();
        authClient1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken1);
        var tenant1EventsResponse = await authClient1.GetAsync("/api/events");

        // Act - Tenant 2 user queries events
        var authClient2 = _factory.CreateClient();
        authClient2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken2);
        var tenant2EventsResponse = await authClient2.GetAsync("/api/events");

        // Assert - Tenant 1 should only see their 2 events
        tenant1EventsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenant1Events = await tenant1EventsResponse.Content.ReadFromJsonAsync<PagedEventsResponse<EventSummaryResponse>>();
        tenant1Events.Should().NotBeNull();
        tenant1Events!.TotalCount.Should().Be(2);
        tenant1Events.Items.Should().HaveCount(2);
        tenant1Events.Items.Should().AllSatisfy(e => e.TenantId.Should().Be(tenantId1));
        tenant1Events.Items.Should().AllSatisfy(e => e.Source.Should().Be("tenant1-app"));

        // Assert - Tenant 2 should only see their 3 events
        tenant2EventsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenant2Events = await tenant2EventsResponse.Content.ReadFromJsonAsync<PagedEventsResponse<EventSummaryResponse>>();
        tenant2Events.Should().NotBeNull();
        tenant2Events!.TotalCount.Should().Be(3);
        tenant2Events.Items.Should().HaveCount(3);
        tenant2Events.Items.Should().AllSatisfy(e => e.TenantId.Should().Be(tenantId2));
        tenant2Events.Items.Should().AllSatisfy(e => e.Source.Should().Be("tenant2-app"));
    }

    [Fact]
    public async Task Events_UserCannotAccessAnotherTenantsEventById()
    {
        // Arrange - Create two separate tenants with users
        var testId = Guid.NewGuid();
        var client1 = _factory.CreateClient();
        var (accessToken1, tenantId1, _) = await _factory.CreateTestUserAndLoginAsync(
            client1, $"tenant1-event-{testId}@example.com", "Pass1234", "User", "One", $"Tenant One Events {testId}");

        var client2 = _factory.CreateClient();
        var (accessToken2, tenantId2, _) = await _factory.CreateTestUserAndLoginAsync(
            client2, $"tenant2-event-{testId}@example.com", "Pass1234", "User", "Two", $"Tenant Two Events {testId}");

        // Create an event for Tenant 1
        var apiKey1 = await _factory.CreateTestApiKeyAsync(tenantId1);
        var apiClient1 = _factory.CreateClient();
        apiClient1.DefaultRequestHeaders.Add("X-Api-Key", apiKey1);
        var createResponse = await apiClient1.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "sensitive_action",
            Source: "tenant1-only",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"secret\": \"tenant1_data\"}").RootElement
        ));
        createResponse.EnsureSuccessStatusCode();
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Act - Tenant 2 user tries to access Tenant 1's event by ID
        var authClient2 = _factory.CreateClient();
        authClient2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken2);
        var getByIdResponse = await authClient2.GetAsync($"/api/events/{createdEvent!.Id}");

        // Assert - Should return 404 Not Found (event is filtered by tenant isolation)
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApiKey_CanOnlyCreateEventsForOwnTenant()
    {
        // Arrange - Create a tenant with API key
        var testId = Guid.NewGuid();
        var (tenant, _, apiKey) = await _factory.CreateTestTenantWithUserAndApiKeyAsync(
            $"apikey-{testId}@example.com", "Pass1234", $"API Key Tenant {testId}");

        // Act - Create event using API key
        var apiClient = _factory.CreateClient();
        apiClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        var createResponse = await apiClient.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "api_event",
            Source: "api-source",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"data\": \"test\"}").RootElement
        ));

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();
        createdEvent.Should().NotBeNull();
        createdEvent!.TenantId.Should().Be(tenant.Id);
        createdEvent!.UserId.Should().Be(Guid.Empty); // Service account
    }

    [Fact]
    public async Task ApiKey_CannotBeUsedForJWTProtectedEndpoints()
    {
        // Arrange - Create a tenant with API key
        var testId = Guid.NewGuid();
        var (tenant, _, apiKey) = await _factory.CreateTestTenantWithUserAndApiKeyAsync(
            $"apikey-protected-{testId}@example.com", "Pass1234", $"API Key Protected Tenant {testId}");

        // Act - Try to query events (GET /api/events) using API key (should fail)
        var apiClient = _factory.CreateClient();
        apiClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        var getEventsResponse = await apiClient.GetAsync("/api/events");

        // Assert - API key authentication scheme not allowed for this endpoint
        getEventsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_CannotBeUsedForApiKeyOnlyEndpoints()
    {
        // Arrange - Create a user with JWT token
        var testId = Guid.NewGuid();
        var client = _factory.CreateClient();
        var (accessToken, tenantId, _) = await _factory.CreateTestUserAndLoginAsync(
            client, $"jwt-apionly-{testId}@example.com", "Pass1234", "User", "JWT", $"JWT Tenant {testId}");

        // Act - Try to create event (POST /api/events) using JWT (should fail)
        var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var createResponse = await authClient.PostAsJsonAsync("/api/events", new CreateEventRequest(
            Type: "jwt_event",
            Source: "jwt-source",
            TimestampUtc: DateTime.UtcNow,
            Payload: JsonDocument.Parse("{\"data\": \"test\"}").RootElement
        ));

        // Assert - JWT not allowed for this endpoint (returns 401 because wrong authentication scheme)
        createResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
