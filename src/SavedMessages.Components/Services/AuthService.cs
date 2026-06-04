using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SavedMessages.Components.Services;

internal sealed class ProblemDetails
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class AuthService(HttpClient httpClient, ITokenStorageService tokenStorage, IDeviceInfoProvider deviceInfoProvider) : IAuthService
{
    private string? _accessToken;
    private DateTime _expiresAt;

    public string? AccessToken => _accessToken;
    public bool IsAuthenticated => _accessToken is not null && DateTime.UtcNow < _expiresAt;

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", new { email, password, deviceName = deviceInfoProvider.DeviceName, devicePlatform = deviceInfoProvider.Platform });

        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            throw new AuthException(problem?.Title ?? "Login failed.");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResult>()
            ?? throw new InvalidOperationException("Invalid auth response.");

        await StoreTokensAsync(result);
        return result;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string displayName)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/register", new { email, password, displayName, deviceName = deviceInfoProvider.DeviceName, devicePlatform = deviceInfoProvider.Platform });

        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            throw new AuthException(problem?.Title ?? "Registration failed.");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResult>()
            ?? throw new InvalidOperationException("Invalid auth response.");

        await StoreTokensAsync(result);
        return result;
    }

    public async Task<AuthResult?> RefreshAsync()
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();

        // For web clients, the HttpOnly cookie is sent automatically;
        // for MAUI clients, we send the token in the body.
        var body = refreshToken is not null ? new { refreshToken } : null;

        var response = await httpClient.PostAsJsonAsync("api/auth/refresh", body);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        if (result is not null)
            await StoreTokensAsync(result);

        return result;
    }

    public async Task LogoutAsync()
    {
        try
        {
            await httpClient.PostAsync("api/auth/logout", null);
        }
        catch
        {
            // Best-effort server-side revocation
        }

        _accessToken = null;
        _expiresAt = default;
        await tokenStorage.ClearAsync();
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/change-password", new { currentPassword, newPassword });

        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            throw new AuthException(problem?.Title ?? "Failed to change password.");
        }
    }

    private async Task StoreTokensAsync(AuthResult result)
    {
        _accessToken = result.AccessToken;
        _expiresAt = result.AccessTokenExpiresAt;
        await tokenStorage.SetAccessTokenAsync(result.AccessToken);
        await tokenStorage.SetRefreshTokenAsync(result.RefreshToken);
    }
}
