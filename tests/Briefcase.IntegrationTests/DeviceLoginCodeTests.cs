using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Briefcase.ApiService.Models;
using Briefcase.Domain.Entities;

namespace Briefcase.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class DeviceLoginCodeTests
{
    private static ApiWebApplicationFactory _factory = null!;
    private static HttpClient _anonClient = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new ApiWebApplicationFactory();
        _factory.EnsureDatabaseCreated();
        _anonClient = _factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _anonClient.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var resp = await _anonClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "Test User"));
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [TestMethod]
    public async Task CreateLoginCode_ReturnsEightCharCode()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/devices/login-code",
            new CreateLoginCodeRequest("New Laptop", "Windows"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginCodeResponse>();
        Assert.IsNotNull(body);
        Assert.AreEqual(8, body.Code.Length);
        Assert.IsTrue(body.ExpiresAt > DateTime.UtcNow);
    }

    [TestMethod]
    public async Task PollLoginCode_BeforeApproval_ReturnsPending()
    {
        var create = await _anonClient.PostAsJsonAsync("/api/devices/login-code",
            new CreateLoginCodeRequest("Pending Device", "Web"));
        var code = (await create.Content.ReadFromJsonAsync<LoginCodeResponse>())!.Code;

        var poll = await _anonClient.GetFromJsonAsync<LoginCodePollResponse>(
            $"/api/devices/login-code/{code}");

        Assert.IsNotNull(poll);
        Assert.AreEqual("pending", poll.Status);
        Assert.IsNull(poll.AccessToken);
    }

    [TestMethod]
    public async Task PollLoginCode_UnknownCode_ReturnsNotFound()
    {
        var poll = await _anonClient.GetFromJsonAsync<LoginCodePollResponse>(
            "/api/devices/login-code/ZZZZZZZZ");

        Assert.IsNotNull(poll);
        Assert.AreEqual("notfound", poll.Status);
    }

    [TestMethod]
    public async Task ApproveLoginCode_Unauthenticated_Returns401()
    {
        var create = await _anonClient.PostAsJsonAsync("/api/devices/login-code",
            new CreateLoginCodeRequest("Device", "Web"));
        var code = (await create.Content.ReadFromJsonAsync<LoginCodeResponse>())!.Code;

        var response = await _anonClient.PostAsJsonAsync("/api/devices/login-code/approve",
            new ApproveLoginCodeRequest(code));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ApproveLoginCode_UnknownCode_Returns404()
    {
        var authed = await CreateAuthenticatedClientAsync($"approve404_{Guid.NewGuid():N}@test.com");

        var response = await authed.PostAsJsonAsync("/api/devices/login-code/approve",
            new ApproveLoginCodeRequest("ZZZZZZZZ"));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task FullFlow_ApproveThenPoll_LogsInPendingDevice()
    {
        var email = $"flow_{Guid.NewGuid():N}@test.com";
        var authed = await CreateAuthenticatedClientAsync(email);

        // 1. New device requests a code.
        var create = await _anonClient.PostAsJsonAsync("/api/devices/login-code",
            new CreateLoginCodeRequest("New Phone", "Android"));
        var code = (await create.Content.ReadFromJsonAsync<LoginCodeResponse>())!.Code;

        // 2. Signed-in device approves it.
        var approve = await authed.PostAsJsonAsync("/api/devices/login-code/approve",
            new ApproveLoginCodeRequest(code));
        Assert.AreEqual(HttpStatusCode.OK, approve.StatusCode);
        var approveBody = await approve.Content.ReadFromJsonAsync<ApproveLoginCodeResponse>();
        Assert.AreEqual("New Phone", approveBody!.DeviceName);

        // 3. New device polls and receives tokens.
        var poll = await _anonClient.GetFromJsonAsync<LoginCodePollResponse>(
            $"/api/devices/login-code/{code}");
        Assert.IsNotNull(poll);
        Assert.AreEqual("approved", poll.Status);
        Assert.IsFalse(string.IsNullOrWhiteSpace(poll.AccessToken));
        Assert.IsFalse(string.IsNullOrWhiteSpace(poll.RefreshToken));

        // 4. The issued access token works against a protected endpoint.
        var newDevice = _factory.CreateClient();
        newDevice.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", poll.AccessToken);
        var devices = await newDevice.GetAsync("/api/devices");
        Assert.AreEqual(HttpStatusCode.OK, devices.StatusCode);

        // 5. The code is single-use — a second poll no longer returns tokens.
        var secondPoll = await _anonClient.GetFromJsonAsync<LoginCodePollResponse>(
            $"/api/devices/login-code/{code}");
        Assert.AreEqual("notfound", secondPoll!.Status);
    }

    [TestMethod]
    public async Task ApproveLoginCode_Twice_ReturnsConflict()
    {
        var authed = await CreateAuthenticatedClientAsync($"twice_{Guid.NewGuid():N}@test.com");

        var create = await _anonClient.PostAsJsonAsync("/api/devices/login-code",
            new CreateLoginCodeRequest("Device", "Web"));
        var code = (await create.Content.ReadFromJsonAsync<LoginCodeResponse>())!.Code;

        var first = await authed.PostAsJsonAsync("/api/devices/login-code/approve",
            new ApproveLoginCodeRequest(code));
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);

        var second = await authed.PostAsJsonAsync("/api/devices/login-code/approve",
            new ApproveLoginCodeRequest(code));
        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode);
    }
}
