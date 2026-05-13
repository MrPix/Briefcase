using SavedMessages.Components.Services;

namespace SavedMessages.Web.Services;

/// <summary>
/// Web token storage: access token in memory, refresh token handled by HttpOnly cookie (no client storage needed).
/// </summary>
public class WebTokenStorageService : ITokenStorageService
{
    private string? _accessToken;

    public Task<string?> GetAccessTokenAsync() => Task.FromResult(_accessToken);

    public Task SetAccessTokenAsync(string token)
    {
        _accessToken = token;
        return Task.CompletedTask;
    }

    // Refresh token is managed via HttpOnly cookie — no client-side storage
    public Task<string?> GetRefreshTokenAsync() => Task.FromResult<string?>(null);

    public Task SetRefreshTokenAsync(string token) => Task.CompletedTask;

    public Task ClearAsync()
    {
        _accessToken = null;
        return Task.CompletedTask;
    }
}
