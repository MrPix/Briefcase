using Briefcase.Components.Services;

namespace Briefcase.Maui.Services;

/// <summary>
/// MAUI token storage: both tokens stored in platform secure storage.
/// </summary>
public class MauiTokenStorageService : ITokenStorageService
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    public async Task<string?> GetAccessTokenAsync()
        => await SecureStorage.Default.GetAsync(AccessTokenKey);

    public async Task SetAccessTokenAsync(string token)
        => await SecureStorage.Default.SetAsync(AccessTokenKey, token);

    public async Task<string?> GetRefreshTokenAsync()
        => await SecureStorage.Default.GetAsync(RefreshTokenKey);

    public async Task SetRefreshTokenAsync(string token)
        => await SecureStorage.Default.SetAsync(RefreshTokenKey, token);

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }
}
