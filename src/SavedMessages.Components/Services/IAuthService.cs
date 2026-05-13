namespace SavedMessages.Components.Services;

public record AuthResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);

public class AuthException(string message) : Exception(message);

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password, string displayName);
    Task<AuthResult?> RefreshAsync();
    Task LogoutAsync();
    string? AccessToken { get; }
    bool IsAuthenticated { get; }
}
