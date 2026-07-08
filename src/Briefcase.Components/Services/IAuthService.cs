namespace Briefcase.Components.Services;

public record AuthResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
public record ExternalAuthProvider(string Key, string DisplayName);

public class AuthException(string message) : Exception(message);

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password, string displayName);
    IReadOnlyList<ExternalAuthProvider> ExternalProviders { get; }
    string BuildExternalLoginUrl(string provider, string clientRedirectUri);
    Task<AuthResult> CompleteExternalLoginAsync(string accessToken, string refreshToken, DateTime accessTokenExpiresAt);
    Task<AuthResult?> RefreshAsync();
    Task LogoutAsync();
    Task ChangePasswordAsync(string currentPassword, string newPassword);
    Task TryRestoreSessionAsync();
    string? AccessToken { get; }
    bool IsAuthenticated { get; }
}
