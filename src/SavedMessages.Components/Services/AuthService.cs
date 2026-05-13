using System.Net.Http.Json;

namespace SavedMessages.Components.Services;

public class AuthService(HttpClient httpClient, ITokenStorageService tokenStorage) : IAuthService
{
    private string? _accessToken;
    private DateTime _expiresAt;

    public string? AccessToken => _accessToken;
    public bool IsAuthenticated => _accessToken is not null && DateTime.UtcNow < _expiresAt;

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResult>()
            ?? throw new InvalidOperationException("Invalid auth response.");

        await StoreTokensAsync(result);
        return result;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string displayName)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/register", new { email, password, displayName });
        response.EnsureSuccessStatusCode();

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

    private async Task StoreTokensAsync(AuthResult result)
    {
        _accessToken = result.AccessToken;
        _expiresAt = result.AccessTokenExpiresAt;
        await tokenStorage.SetAccessTokenAsync(result.AccessToken);
        await tokenStorage.SetRefreshTokenAsync(result.RefreshToken);
    }
}
