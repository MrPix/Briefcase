using Microsoft.JSInterop;
using SavedMessages.Components.Services;

namespace SavedMessages.Web.Services;

/// <summary>
/// Web token storage using browser localStorage via JS interop.
/// </summary>
public class WebTokenStorageService(IJSRuntime js) : ITokenStorageService
{
    private const string AccessTokenKey = "savedmessages_access_token";
    private const string RefreshTokenKey = "savedmessages_refresh_token";

    public async Task<string?> GetAccessTokenAsync() =>
        await js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);

    public async Task SetAccessTokenAsync(string token) =>
        await js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, token);

    public async Task<string?> GetRefreshTokenAsync() =>
        await js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

    public async Task SetRefreshTokenAsync(string token) =>
        await js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, token);

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }
}
