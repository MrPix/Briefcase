using Microsoft.JSInterop;
using Briefcase.Components.Services;

namespace Briefcase.Web.Services;

/// <summary>
/// Web token storage using browser localStorage via JS interop.
/// </summary>
public class WebTokenStorageService(IJSRuntime js) : ITokenStorageService
{
    private const string AccessTokenKey = "Briefcase_access_token";
    private const string RefreshTokenKey = "Briefcase_refresh_token";

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
