namespace SavedMessages.Components.Services;

public interface ITokenStorageService
{
    Task<string?> GetAccessTokenAsync();
    Task SetAccessTokenAsync(string token);
    Task<string?> GetRefreshTokenAsync();
    Task SetRefreshTokenAsync(string token);
    Task ClearAsync();
}
