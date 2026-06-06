using System.Net;
using System.Net.Http.Json;
using Briefcase.ApiService.Models;

namespace Briefcase.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class AuthControllerTests
{
    private static ApiWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new ApiWebApplicationFactory();
        _factory.EnsureDatabaseCreated();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ── Register ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Register_ValidRequest_Returns200WithTokens()
    {
        var request = new RegisterRequest(
            Email: $"register_{Guid.NewGuid():N}@test.com",
            Password: "Password123!",
            DisplayName: "Test User");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(body);
        Assert.IsFalse(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.IsFalse(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.IsTrue(body.AccessTokenExpiresAt > DateTime.UtcNow);
    }

    [TestMethod]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest(email, "Password123!", "User");

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_MissingPassword_Returns400()
    {
        // Password below minimum length (8 chars)
        var request = new RegisterRequest($"short_{Guid.NewGuid():N}@test.com", "abc", "User");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var email = $"login_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "User"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(body);
        Assert.IsFalse(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    [TestMethod]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"wrongpw_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "User"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "WrongPassword!"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@nowhere.example", "Password123!"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        var email = $"refresh_{Guid.NewGuid():N}@test.com";
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "User"));
        var auth = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth!.RefreshToken));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(newAuth);
        Assert.IsFalse(string.IsNullOrWhiteSpace(newAuth.AccessToken));
        // Token rotation: the new refresh token must differ from the old one
        Assert.AreNotEqual(auth.RefreshToken, newAuth.RefreshToken);
    }

    [TestMethod]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest("not-a-real-token"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Refresh_AlreadyRotatedToken_Returns401()
    {
        var email = $"revoked_{Guid.NewGuid():N}@test.com";
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "User"));
        var auth = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Rotate the token once
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth!.RefreshToken));

        // Original token is now revoked
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth.RefreshToken));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Logout_AuthenticatedUser_Returns204()
    {
        var email = $"logout_{Guid.NewGuid():N}@test.com";
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "User"));
        var auth = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();

        using var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await authedClient.PostAsJsonAsync("/api/auth/logout", new { });

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    public async Task Logout_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
