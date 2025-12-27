using ChronicleHub.Application.DTOs.Auth;
using ChronicleHub.Application.Services;
using ChronicleHub.Domain.Constants;
using ChronicleHub.Domain.Entities;
using ChronicleHub.Domain.Exceptions;
using ChronicleHub.Domain.Identity;
using ChronicleHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ChronicleHub.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ChronicleHubDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ChronicleHubDbContext db,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken ct = default)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResult(false, null, null, null, null, null, "User with this email already exists");
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true // For demo; in production, send confirmation email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new AuthResult(false, null, null, null, null, null, errors);
        }

        // Create tenant for the user
        var tenantSlug = GenerateSlug(request.TenantName);
        var tenant = new Tenant(Guid.NewGuid(), request.TenantName, tenantSlug);
        _db.Tenants.Add(tenant);

        // Create user-tenant relationship with Owner role
        var userTenant = new UserTenant(Guid.NewGuid(), user.Id, tenant.Id, Roles.Owner);
        _db.UserTenants.Add(userTenant);

        await _db.SaveChangesAsync(ct);

        // Generate tokens
        return await GenerateAuthResultAsync(user, tenant.Id, Roles.Owner, ipAddress, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResult(false, null, null, null, null, null, "Invalid email or password");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return new AuthResult(false, null, null, null, null, null, "Invalid email or password");
        }

        // Get user's tenants
        var userTenants = await _db.UserTenants
            .Include(ut => ut.Tenant)
            .Where(ut => ut.UserId == user.Id)
            .ToListAsync(ct);

        if (!userTenants.Any())
        {
            return new AuthResult(false, null, null, null, null, null, "User is not associated with any tenant");
        }

        // Select tenant (use specified or default to first)
        UserTenant selectedUserTenant;
        if (request.TenantId.HasValue)
        {
            selectedUserTenant = userTenants.FirstOrDefault(ut => ut.TenantId == request.TenantId.Value)
                ?? throw new ForbiddenException("You do not have access to the specified tenant");
        }
        else
        {
            selectedUserTenant = userTenants.First();
        }

        // Check if tenant is active
        if (!selectedUserTenant.Tenant.IsActive)
        {
            return new AuthResult(false, null, null, null, null, null, "Tenant is deactivated");
        }

        // Update last login
        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        return await GenerateAuthResultAsync(user, selectedUserTenant.TenantId, selectedUserTenant.Role, ipAddress, ct);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (storedToken == null || !storedToken.IsActive())
        {
            throw new UnauthorizedException("Invalid or expired refresh token");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null)
        {
            throw new UnauthorizedException("User not found");
        }

        // Get user's primary tenant (first one for simplicity)
        var userTenant = await _db.UserTenants
            .Include(ut => ut.Tenant)
            .FirstOrDefaultAsync(ut => ut.UserId == user.Id, ct);

        if (userTenant == null)
        {
            throw new UnauthorizedException("User is not associated with any tenant");
        }

        // Check if tenant is active
        if (!userTenant.Tenant.IsActive)
        {
            throw new UnauthorizedException("Tenant is deactivated");
        }

        // Generate new tokens
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newTokenHash = _tokenService.HashToken(newRefreshToken);

        // Revoke old token (token rotation)
        storedToken.Revoke(ipAddress, newTokenHash);

        // Create new refresh token
        var refreshTokenLifetimeDays = _configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays", 7);
        var newRefreshTokenEntity = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            newTokenHash,
            DateTime.UtcNow.AddDays(refreshTokenLifetimeDays),
            ipAddress
        );

        _db.RefreshTokens.Add(newRefreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        // Generate new access token
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, userTenant.TenantId, userTenant.Role);
        var expiresInMinutes = _configuration.GetValue<int>("Jwt:ExpiresInMinutes", 15);

        return new AuthResult(
            true,
            accessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(expiresInMinutes),
            new UserInfo(user.Id, user.Email!, user.FirstName, user.LastName),
            new TenantInfo(userTenant.TenantId, userTenant.Tenant.Name, userTenant.Tenant.Slug, userTenant.Role)
        );
    }

    public async Task LogoutAsync(string refreshToken, string ipAddress, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (storedToken != null && storedToken.IsActive())
        {
            storedToken.Revoke(ipAddress);
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResult> GenerateAuthResultAsync(
        ApplicationUser user,
        Guid tenantId,
        string role,
        string ipAddress,
        CancellationToken ct)
    {
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, tenantId, role);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);

        var refreshTokenLifetimeDays = _configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays", 7);
        var refreshTokenEntity = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(refreshTokenLifetimeDays),
            ipAddress
        );

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct);
        var expiresInMinutes = _configuration.GetValue<int>("Jwt:ExpiresInMinutes", 15);

        return new AuthResult(
            true,
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(expiresInMinutes),
            new UserInfo(user.Id, user.Email!, user.FirstName, user.LastName),
            new TenantInfo(tenantId, tenant!.Name, tenant.Slug, role)
        );
    }

    private string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Trim('-');
    }
}
