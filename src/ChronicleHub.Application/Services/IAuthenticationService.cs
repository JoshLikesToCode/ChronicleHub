using ChronicleHub.Application.DTOs.Auth;

namespace ChronicleHub.Application.Services;

public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, string ipAddress, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
}
