using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Briefcase.ApiService.Models;

namespace Briefcase.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class DeviceSessionTests
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

    private static HttpRequestMessage Authed(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    /// <summary>Registers a user, then signs a second device in, returning both sessions.</summary>
    private static async Task<(AuthResponse First, AuthResponse Second)> CreateTwoDeviceSessionsAsync()
    {
        var email = $"devices_{Guid.NewGuid():N}@test.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, "Password123!", "User", "Laptop", "Windows", $"install-{Guid.NewGuid():N}"));
        registerResponse.EnsureSuccessStatusCode();
        var first = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            email, "Password123!", "Phone", "Android", $"install-{Guid.NewGuid():N}"));
        loginResponse.EnsureSuccessStatusCode();
        var second = (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        return (first, second);
    }

    private static async Task<List<DeviceResponse>> ListDevicesAsync(string accessToken)
    {
        var response = await _client.SendAsync(Authed(HttpMethod.Get, "/api/devices", accessToken));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<DeviceResponse>>())!;
    }

    [TestMethod]
    public async Task GetDevices_MarksTheCallingDeviceAsCurrent()
    {
        var (first, _) = await CreateTwoDeviceSessionsAsync();

        var devices = await ListDevicesAsync(first.AccessToken);

        Assert.AreEqual(2, devices.Count);
        Assert.AreEqual(1, devices.Count(d => d.IsCurrent));
        Assert.AreEqual("Laptop", devices.Single(d => d.IsCurrent).Name);
    }

    [TestMethod]
    public async Task RemoveDevice_RevokesThatDevicesAccessToken()
    {
        var (first, second) = await CreateTwoDeviceSessionsAsync();
        var phone = (await ListDevicesAsync(first.AccessToken)).Single(d => d.Name == "Phone");

        var remove = await _client.SendAsync(Authed(HttpMethod.Delete, $"/api/devices/{phone.Id}", first.AccessToken));
        Assert.AreEqual(HttpStatusCode.NoContent, remove.StatusCode);

        var revoked = await _client.SendAsync(Authed(HttpMethod.Get, "/api/devices", second.AccessToken));
        Assert.AreEqual(HttpStatusCode.Unauthorized, revoked.StatusCode);

        var remaining = await _client.SendAsync(Authed(HttpMethod.Get, "/api/devices", first.AccessToken));
        Assert.AreEqual(HttpStatusCode.OK, remaining.StatusCode);
    }

    [TestMethod]
    public async Task RemoveDevice_RevokesThatDevicesRefreshToken()
    {
        var (first, second) = await CreateTwoDeviceSessionsAsync();
        var phone = (await ListDevicesAsync(first.AccessToken)).Single(d => d.Name == "Phone");

        await _client.SendAsync(Authed(HttpMethod.Delete, $"/api/devices/{phone.Id}", first.AccessToken));

        var refreshRevoked = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(second.RefreshToken));
        Assert.AreEqual(HttpStatusCode.Unauthorized, refreshRevoked.StatusCode);

        var refreshOk = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken));
        Assert.AreEqual(HttpStatusCode.OK, refreshOk.StatusCode);
    }

    [TestMethod]
    public async Task Login_SameInstallationId_ReusesTheSameDeviceRow()
    {
        var email = $"reuse_{Guid.NewGuid():N}@test.com";
        var installationId = $"install-{Guid.NewGuid():N}";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, "Password123!", "User", "Laptop", "Windows", installationId));
        register.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            email, "Password123!", "Laptop renamed", "Windows", installationId));
        login.EnsureSuccessStatusCode();
        var session = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;

        var devices = await ListDevicesAsync(session.AccessToken);

        Assert.AreEqual(1, devices.Count);
        Assert.AreEqual("Laptop renamed", devices[0].Name);
    }

    [TestMethod]
    public async Task SignOutOthers_KeepsCallerAndRevokesTheRest()
    {
        var (first, second) = await CreateTwoDeviceSessionsAsync();

        var response = await _client.SendAsync(Authed(HttpMethod.Post, "/api/devices/sign-out-others", first.AccessToken));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SignOutOthersResponse>();
        Assert.AreEqual(1, result!.RemovedCount);

        var revoked = await _client.SendAsync(Authed(HttpMethod.Get, "/api/devices", second.AccessToken));
        Assert.AreEqual(HttpStatusCode.Unauthorized, revoked.StatusCode);

        var devices = await ListDevicesAsync(first.AccessToken);
        Assert.AreEqual(1, devices.Count);
        Assert.IsTrue(devices[0].IsCurrent);
    }
}
