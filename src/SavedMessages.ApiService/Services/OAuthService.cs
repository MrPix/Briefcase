using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SavedMessages.ApiService.Services;

public class OAuthService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    private static readonly string[] SupportedProviders = ["Google", "Facebook", "Apple", "Microsoft"];

    private static readonly ConcurrentDictionary<string, OAuthPendingState> PendingStates = new();

    public bool IsProviderSupported(string provider) =>
        SupportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase);

    public string NormalizeProvider(string provider) =>
        SupportedProviders.First(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase));

    public (string AuthorizationUrl, string State) BuildAuthorizationUrl(string provider, string redirectUri)
    {
        provider = NormalizeProvider(provider);
        var config = GetProviderConfig(provider);

        var state = GenerateRandomString(32);
        var codeVerifier = GenerateRandomString(64);
        var codeChallenge = ComputeCodeChallenge(codeVerifier);

        PendingStates[state] = new OAuthPendingState(provider, codeVerifier, redirectUri, DateTime.UtcNow);

        var callbackUrl = redirectUri.TrimEnd('/');
        // The callback goes back to our API, not to the client directly
        var scopes = Uri.EscapeDataString(config.Scopes);

        var queryParams = new StringBuilder();
        queryParams.Append($"?client_id={Uri.EscapeDataString(config.ClientId)}");
        queryParams.Append($"&redirect_uri={Uri.EscapeDataString(callbackUrl)}");
        queryParams.Append("&response_type=code");
        queryParams.Append($"&scope={scopes}");
        queryParams.Append($"&state={Uri.EscapeDataString(state)}");
        queryParams.Append($"&code_challenge={Uri.EscapeDataString(codeChallenge)}");
        queryParams.Append("&code_challenge_method=S256");

        // Apple requires response_mode=form_post
        if (provider == "Apple")
            queryParams.Append("&response_mode=form_post");

        return ($"{config.AuthorizationEndpoint}{queryParams}", state);
    }

    public bool TryConsumePendingState(string state, out OAuthPendingState pendingState)
    {
        if (PendingStates.TryRemove(state, out var stored))
        {
            // States expire after 10 minutes
            if (stored.CreatedAt.AddMinutes(10) > DateTime.UtcNow)
            {
                pendingState = stored;
                return true;
            }
        }

        pendingState = default!;
        return false;
    }

    public async Task<OAuthTokenResponse?> ExchangeCodeAsync(string provider, string code, string codeVerifier, string redirectUri)
    {
        var config = GetProviderConfig(provider);
        var client = httpClientFactory.CreateClient();

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["code_verifier"] = codeVerifier,
        };

        var response = await client.PostAsync(config.TokenEndpoint, new FormUrlEncodedContent(parameters));
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new OAuthTokenResponse(
            AccessToken: json.GetProperty("access_token").GetString()!,
            IdToken: json.TryGetProperty("id_token", out var idToken) ? idToken.GetString() : null
        );
    }

    public async Task<OAuthUserInfo?> GetUserInfoAsync(string provider, string accessToken, string? idToken)
    {
        provider = NormalizeProvider(provider);

        // Apple doesn't have a userinfo endpoint — user info comes from the id_token
        if (provider == "Apple")
            return GetAppleUserInfoFromIdToken(idToken);

        var config = GetProviderConfig(provider);
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(config.UserInfoEndpoint);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        return provider switch
        {
            "Google" => new OAuthUserInfo(
                ProviderKey: json.GetProperty("sub").GetString()!,
                Email: json.GetProperty("email").GetString()!,
                Name: json.TryGetProperty("name", out var gName) ? gName.GetString()! : json.GetProperty("email").GetString()!,
                AvatarUrl: json.TryGetProperty("picture", out var gPic) ? gPic.GetString() : null
            ),
            "Facebook" => new OAuthUserInfo(
                ProviderKey: json.GetProperty("id").GetString()!,
                Email: json.GetProperty("email").GetString()!,
                Name: json.TryGetProperty("name", out var fbName) ? fbName.GetString()! : json.GetProperty("email").GetString()!,
                AvatarUrl: json.TryGetProperty("picture", out var fbPic) && fbPic.TryGetProperty("data", out var fbPicData)
                    ? fbPicData.GetProperty("url").GetString() : null
            ),
            "Microsoft" => new OAuthUserInfo(
                ProviderKey: json.GetProperty("sub").GetString()!,
                Email: json.TryGetProperty("email", out var msEmail) ? msEmail.GetString()! : "",
                Name: json.TryGetProperty("name", out var msName) ? msName.GetString()! : "",
                AvatarUrl: null
            ),
            _ => null
        };
    }

    private static OAuthUserInfo? GetAppleUserInfoFromIdToken(string? idToken)
    {
        if (string.IsNullOrEmpty(idToken))
            return null;

        // Decode JWT payload (we trust it since it came directly from Apple's token endpoint over HTTPS)
        var parts = idToken.Split('.');
        if (parts.Length < 2) return null;

        var payload = parts[1];
        // Fix base64url padding
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var json = JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(payload));
        return new OAuthUserInfo(
            ProviderKey: json.GetProperty("sub").GetString()!,
            Email: json.TryGetProperty("email", out var email) ? email.GetString()! : "",
            Name: json.TryGetProperty("email", out var name) ? name.GetString()! : "",
            AvatarUrl: null
        );
    }

    private OAuthProviderConfig GetProviderConfig(string provider)
    {
        var section = configuration.GetSection($"OAuth:{provider}");
        return new OAuthProviderConfig(
            ClientId: section["ClientId"] ?? throw new InvalidOperationException($"OAuth:{provider}:ClientId is not configured."),
            ClientSecret: section["ClientSecret"] ?? throw new InvalidOperationException($"OAuth:{provider}:ClientSecret is not configured."),
            AuthorizationEndpoint: section["AuthorizationEndpoint"]!,
            TokenEndpoint: section["TokenEndpoint"]!,
            UserInfoEndpoint: section["UserInfoEndpoint"] ?? "",
            Scopes: section["Scopes"] ?? "openid email profile"
        );
    }

    private static string GenerateRandomString(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            [..length];
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}

public record OAuthPendingState(string Provider, string CodeVerifier, string RedirectUri, DateTime CreatedAt);
public record OAuthTokenResponse(string AccessToken, string? IdToken);
public record OAuthUserInfo(string ProviderKey, string Email, string Name, string? AvatarUrl);
public record OAuthProviderConfig(string ClientId, string ClientSecret, string AuthorizationEndpoint, string TokenEndpoint, string UserInfoEndpoint, string Scopes);
