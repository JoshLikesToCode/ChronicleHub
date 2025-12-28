using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChronicleHub.Application.DTOs.Auth;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ChronicleHub.Api.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class AuthControllerTests : IClassFixture<ChronicleHubWebApplicationFactory>, IAsyncDisposable
{
    private readonly ChronicleHubWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(ChronicleHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientWithCookies();
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

    [Fact]
    public async Task Register_WithValidRequest_ShouldReturnSuccessAndAccessToken()
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "newuser@example.com",
            Password: "SecurePass123",
            FirstName: "John",
            LastName: "Doe",
            TenantName: "Acme Corporation"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResult = await response.Content.ReadFromJsonAsync<AuthResult>();
        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.AccessToken.Should().NotBeNullOrEmpty();
        authResult.RefreshToken.Should().BeNull(); // Refresh token is in HttpOnly cookie
        authResult.User.Should().NotBeNull();
        authResult.User!.Email.Should().Be(request.Email);
        authResult.User!.FirstName.Should().Be(request.FirstName);
        authResult.User!.LastName.Should().Be(request.LastName);
        authResult.Tenant.Should().NotBeNull();
        authResult.Tenant!.Name.Should().Be(request.TenantName);

        // Verify refresh token cookie was set
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().NotBeNull();
        cookies!.Should().Contain(c => c.Contains("refreshToken"));
        cookies!.Should().Contain(c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        // Note: Secure flag is only set for HTTPS; tests run over HTTP so secure flag is correctly absent
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest(
            Email: "duplicate@example.com",
            Password: "SecurePass123",
            FirstName: "John",
            LastName: "Doe",
            TenantName: "First Tenant"
        );

        // Register once
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Try to register again with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccessAndAccessToken()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest(
            Email: "login@example.com",
            Password: "SecurePass123",
            FirstName: "Jane",
            LastName: "Smith",
            TenantName: "Test Company"
        );
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            Email: "login@example.com",
            Password: "SecurePass123",
            TenantId: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResult = await response.Content.ReadFromJsonAsync<AuthResult>();
        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.AccessToken.Should().NotBeNullOrEmpty();
        authResult.User.Should().NotBeNull();
        authResult.User!.Email.Should().Be(loginRequest.Email);
        authResult.Tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest(
            Email: "nonexistent@example.com",
            Password: "WrongPassword123",
            TenantId: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ShouldReturnNewAccessToken()
    {
        // Arrange - Register and get refresh token cookie
        var (accessToken, _, _) = await _factory.CreateTestUserAndLoginAsync(_client, "refresh@example.com");

        // Act - Call refresh endpoint
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResult = await response.Content.ReadFromJsonAsync<AuthResult>();
        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.AccessToken.Should().NotBeNullOrEmpty();
        authResult.AccessToken.Should().NotBe(accessToken); // Should be a new token
    }

    [Fact]
    public async Task Logout_WithValidRefreshToken_ShouldRevokeToken()
    {
        // Arrange - Register and get refresh token cookie
        await _factory.CreateTestUserAndLoginAsync(_client, "logout@example.com");

        // Act - Logout
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use the refresh token after logout - should fail
        var refreshResponse = await _client.PostAsync("/api/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidJWT_ShouldAllowAccess()
    {
        // Arrange
        var (accessToken, tenantId, _) = await _factory.CreateTestUserAndLoginAsync(_client, "protected@example.com");

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Act - Try to access a protected endpoint (GET /api/events requires tenant membership)
        var response = await authenticatedClient.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutJWT_ShouldReturn401()
    {
        // Act - Try to access a protected endpoint without authentication
        var response = await _client.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredOrInvalidJWT_ShouldReturn401()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        var response = await authenticatedClient.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
