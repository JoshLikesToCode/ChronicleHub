using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChronicleHub.Api.Contracts.Common;
using ChronicleHub.Api.Contracts.Events;
using ChronicleHub.Application.DTOs.Auth;
using ChronicleHub.Application.ProblemDetails;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ChronicleHub.Api.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class EventsControllerTests : IClassFixture<ChronicleHubWebApplicationFactory>, IAsyncDisposable
{
    private readonly ChronicleHubWebApplicationFactory _factory;

    public EventsControllerTests(ChronicleHubWebApplicationFactory factory)
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

        // Remove users through UserManager to maintain referential integrity
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ChronicleHub.Domain.Identity.ApplicationUser>>();
        var users = db.Users.ToList();
        foreach (var user in users)
        {
            await userManager.DeleteAsync(user);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates an API key client AND a JWT client for the SAME tenant
    /// This ensures tenant isolation works correctly in tests
    /// </summary>
    private async Task<(HttpClient ApiKeyClient, HttpClient JwtClient, Guid TenantId)> CreateSameTenantClientsAsync()
    {
        // Create a tenant with user and API key
        var (tenant, user, apiKey) = await _factory.CreateTestTenantWithUserAndApiKeyAsync(
            $"tenant-{Guid.NewGuid()}@example.com",
            "Password123",
            "Shared Tenant"
        );

        // Create API key client
        var apiKeyClient = _factory.CreateClient();
        apiKeyClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        // Create JWT client by logging in as the tenant's user
        var jwtClient = _factory.CreateClient();
        var loginRequest = new LoginRequest(
            Email: user.Email!,
            Password: "Password123",
            TenantId: null
        );
        var loginResponse = await jwtClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed: {loginResponse.StatusCode} - {errorContent}");
        }

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();

        if (authResult == null || !authResult.Success || string.IsNullOrEmpty(authResult.AccessToken))
        {
            throw new InvalidOperationException($"Login succeeded but auth result was invalid. Success: {authResult?.Success}, Error: {authResult?.ErrorMessage}");
        }

        if (authResult.Tenant?.Id != tenant.Id)
        {
            throw new InvalidOperationException($"Login returned wrong tenant. Expected: {tenant.Id}, Got: {authResult.Tenant?.Id}");
        }

        jwtClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

        return (apiKeyClient, jwtClient, tenant.Id);
    }

    private async Task<HttpClient> CreateApiKeyClientAsync()
    {
        var client = _factory.CreateClient();

        // Create a tenant with user and API key
        var (_, _, apiKey) = await _factory.CreateTestTenantWithUserAndApiKeyAsync(
            $"apikey-{Guid.NewGuid()}@example.com",
            "Password123",
            "API Key Tenant"
        );

        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }

    private async Task<HttpClient> CreateAuthenticatedJwtClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _, _) = await _factory.CreateTestUserAndLoginAsync(
            client,
            $"test-{Guid.NewGuid()}@example.com",
            "SecurePass123",
            "Test",
            "User",
            "Test Tenant"
        );

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task CreateEvent_WithValidRequest_ShouldReturnCreatedWithEvent()
    {
        // Arrange
        var client = await CreateApiKeyClientAsync();
        var request = new
        {
            Type = "page_view",
            Source = "web-app",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { page = "/home", duration = 1500 }
        };

        // Act
        var action = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await action.Content.ReadFromJsonAsync<EventResponse>();
        response.Should().NotBeNull();
        response!.Id.Should().NotBeEmpty();
        response.Type.Should().Be("page_view");
        response.Source.Should().Be("web-app");
        response.Payload.GetProperty("page").GetString().Should().Be("/home");
        response.Payload.GetProperty("duration").GetInt32().Should().Be(1500);
        response.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        action.Headers.Location.Should().NotBeNull();
        action.Headers.Location!.ToString().Should().Contain($"/api/Events/{response.Id}"); // Note: ASP.NET Core uses PascalCase for controller names in routes
    }

    [Fact]
    public async Task CreateEvent_WithoutApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            Type = "test_event",
            Source = "test",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { }
        };

        // Act
        var action = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
        var request = new
        {
            Type = "test_event",
            Source = "test",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { }
        };

        // Act
        var action = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEvent_WithEmptyType_ShouldReturnBadRequest()
    {
        // Arrange
        var client = await CreateApiKeyClientAsync();
        var request = new
        {
            Type = "",
            Source = "test",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { }
        };

        // Act
        var action = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEvent_WithEmptySource_ShouldReturnBadRequest()
    {
        // Arrange
        var client = await CreateApiKeyClientAsync();
        var request = new
        {
            Type = "test",
            Source = "",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { }
        };

        // Act
        var action = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WithExistingEvent_ShouldReturnSuccessEnvelope()
    {
        // Arrange - Create clients for the same tenant
        var (apiClient, jwtClient, _) = await CreateSameTenantClientsAsync();

        var createRequest = new
        {
            Type = "user_login",
            Source = "mobile-app",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { userId = "user123", platform = "iOS" }
        };

        var createResponse = await apiClient.PostAsJsonAsync("/api/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Act
        var action = await jwtClient.GetAsync($"/api/events/{createdEvent!.Id}");

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await action.Content.ReadFromJsonAsync<ApiResponse<EventResponse>>();
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(createdEvent.Id);
        response.Data.Type.Should().Be("user_login");
        response.Data.Source.Should().Be("mobile-app");
        response.Error.Should().BeNull();
        response.Metadata.Should().NotBeNull();
        response.Metadata!.RequestDurationMs.Should().BeGreaterThan(0);
        response.Metadata.TimestampUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetById_WithNonExistentEvent_ShouldReturnNotFoundProblemDetails()
    {
        // Arrange
        var client = await CreateAuthenticatedJwtClientAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var action = await client.GetAsync($"/api/events/{nonExistentId}");

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.NotFound);
        action.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await action.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Contain("ActivityEvent");
        problemDetails.Detail.Should().Contain(nonExistentId.ToString());
        problemDetails.Instance.Should().Be($"/api/events/{nonExistentId}");
        problemDetails.Extensions.Should().ContainKey("resourceName");
        problemDetails.Extensions!["resourceName"].ToString().Should().Be("ActivityEvent");
    }

    [Fact]
    public async Task GetById_WithoutApiKey_ShouldAllowReadOperation()
    {
        // Arrange - GET requests require JWT authentication, not API key
        var client = await CreateAuthenticatedJwtClientAsync();
        var eventId = Guid.NewGuid();

        // Act
        var action = await client.GetAsync($"/api/events/{eventId}");

        // Assert - Should return 404 (not found) instead of 401 (unauthorized)
        // This proves the request was authenticated with JWT
        action.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ResponseMetadata_ShouldIncludeRequestDuration()
    {
        // Arrange - Create clients for the same tenant
        var (apiClient, jwtClient, _) = await CreateSameTenantClientsAsync();

        var createRequest = new
        {
            Type = "test_event",
            Source = "test",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { test = true }
        };

        var createResponse = await apiClient.PostAsJsonAsync("/api/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Act
        var action = await jwtClient.GetAsync($"/api/events/{createdEvent!.Id}");

        // Assert
        var response = await action.Content.ReadFromJsonAsync<ApiResponse<EventResponse>>();
        response!.Metadata.Should().NotBeNull();
        response.Metadata!.RequestDurationMs.Should().BeGreaterThan(0);
        response.Metadata.RequestDurationMs.Should().BeLessThan(10000); // Should be less than 10 seconds
    }

    [Fact]
    public async Task GetEvents_WithoutFilters_ShouldReturnPagedEvents()
    {
        // Arrange - Create clients for the same tenant
        var (apiClient, jwtClient, _) = await CreateSameTenantClientsAsync();

        for (int i = 0; i < 5; i++)
        {
            var request = new
            {
                Type = $"event_type_{i}",
                Source = "test-source",
                TimestampUtc = DateTime.UtcNow.AddMinutes(-i),
                Payload = new { index = i }
            };
            await apiClient.PostAsJsonAsync("/api/events", request);
        }

        // Act
        var action = await jwtClient.GetAsync("/api/events?page=1&pageSize=10");

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await action.Content.ReadFromJsonAsync<PagedEventsResponse<EventSummaryResponse>>();
        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
        response.Items.Count.Should().BeGreaterThanOrEqualTo(5);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(10);
        response.TotalCount.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetEvents_WithTypeFilter_ShouldReturnFilteredEvents()
    {
        // Arrange - Create clients for the same tenant
        var (apiClient, jwtClient, _) = await CreateSameTenantClientsAsync();

        await apiClient.PostAsJsonAsync("/api/events", new
        {
            Type = "unique_type_filter_test",
            Source = "test",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { }
        });

        await apiClient.PostAsJsonAsync("/api/events", new
        {
            Type = "other_type",
            Source = "test",
            TimestampUtc = DateTime.UtcNow,
            Payload = new { }
        });

        // Act
        var action = await jwtClient.GetAsync("/api/events?type=unique_type_filter_test&page=1&pageSize=10");

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await action.Content.ReadFromJsonAsync<PagedEventsResponse<EventSummaryResponse>>();
        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
        response.Items.Should().OnlyContain(e => e.Type == "unique_type_filter_test");
    }

    [Fact]
    public async Task GetEvents_WithInvalidPageSize_ShouldReturnBadRequest()
    {
        // Arrange
        var client = await CreateAuthenticatedJwtClientAsync();

        // Act
        var action = await client.GetAsync("/api/events?page=1&pageSize=101");

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEvents_WithInvalidPage_ShouldReturnBadRequest()
    {
        // Arrange
        var client = await CreateAuthenticatedJwtClientAsync();

        // Act
        var action = await client.GetAsync("/api/events?page=0&pageSize=10");

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAndRetrieve_FullWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange - Create clients for the same tenant
        var (apiClient, jwtClient, _) = await CreateSameTenantClientsAsync();

        var createRequest = new
        {
            Type = "purchase",
            Source = "e-commerce",
            TimestampUtc = new DateTime(2025, 12, 11, 15, 30, 0, DateTimeKind.Utc),
            Payload = new
            {
                productId = "prod-123",
                price = 99.99,
                currency = "USD"
            }
        };

        // Act - Create with API key
        var createAction = await apiClient.PostAsJsonAsync("/api/events", createRequest);
        createAction.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdEvent = await createAction.Content.ReadFromJsonAsync<EventResponse>();
        createdEvent.Should().NotBeNull();

        // Act - Retrieve with JWT
        var getAction = await jwtClient.GetAsync($"/api/events/{createdEvent!.Id}");
        getAction.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedResponse = await getAction.Content.ReadFromJsonAsync<ApiResponse<EventResponse>>();

        // Assert
        retrievedResponse.Should().NotBeNull();
        retrievedResponse!.Success.Should().BeTrue();
        retrievedResponse.Data.Should().NotBeNull();
        retrievedResponse.Data!.Id.Should().Be(createdEvent.Id);
        retrievedResponse.Data.Type.Should().Be("purchase");
        retrievedResponse.Data.Source.Should().Be("e-commerce");
        retrievedResponse.Data.Payload.GetProperty("productId").GetString().Should().Be("prod-123");
        retrievedResponse.Data.Payload.GetProperty("price").GetDouble().Should().Be(99.99);
    }

    [Fact]
    public async Task GetEvents_WithPagination_ShouldRespectPageSize()
    {
        // Arrange - Create clients for the same tenant
        var (apiClient, jwtClient, _) = await CreateSameTenantClientsAsync();

        for (int i = 0; i < 15; i++)
        {
            await apiClient.PostAsJsonAsync("/api/events", new
            {
                Type = $"pagination_test_{i}",
                Source = "test",
                TimestampUtc = DateTime.UtcNow,
                Payload = new { }
            });
        }

        // Act
        var action = await jwtClient.GetAsync("/api/events?page=1&pageSize=5");

        // Assert
        action.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await action.Content.ReadFromJsonAsync<PagedEventsResponse<EventSummaryResponse>>();
        response.Should().NotBeNull();
        response!.Items.Count.Should().BeLessThanOrEqualTo(5);
        response.TotalCount.Should().BeGreaterThanOrEqualTo(15);
    }
}
