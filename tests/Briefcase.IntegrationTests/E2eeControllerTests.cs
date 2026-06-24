using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Briefcase.ApiService.Models;

namespace Briefcase.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class E2eeControllerTests
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

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var email = $"e2ee_{Guid.NewGuid():N}@test.com";
        var resp = await _anonClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "E2EE User"));
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    // ── Auth guard ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetSettings_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/e2ee/settings");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Enable_Unauthenticated_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/e2ee/enable",
            new EnableE2eeRequest("PBKDF2-SHA256", "c2FsdA==", "{}", "dmVyaWZpZXI="));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GetSettings — fresh account ──────────────────────────────────────────

    [TestMethod]
    public async Task GetSettings_NewUser_ReturnsDisabledWithNulls()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/e2ee/settings");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<E2eeSettingsResponse>();
        Assert.IsNotNull(body);
        Assert.IsFalse(body.IsEnabled);
        Assert.IsNull(body.KdfSalt);
        Assert.IsNull(body.KeyVerifier);
    }

    // ── Enable ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Enable_ValidRequest_Returns204AndPersists()
    {
        var client = await CreateAuthenticatedClientAsync();
        var request = new EnableE2eeRequest(
            KdfAlgorithm: "PBKDF2-SHA256",
            KdfSalt: "AAAAAAAAAAAAAAAAAAAAAA==",
            KdfParams: "{\"iterations\":600000}",
            KeyVerifier: "AQIDBAUGB==");

        var enableResp = await client.PostAsJsonAsync("/api/e2ee/enable", request);
        Assert.AreEqual(HttpStatusCode.NoContent, enableResp.StatusCode);

        var settingsResp = await client.GetAsync("/api/e2ee/settings");
        Assert.AreEqual(HttpStatusCode.OK, settingsResp.StatusCode);
        var body = await settingsResp.Content.ReadFromJsonAsync<E2eeSettingsResponse>();
        Assert.IsNotNull(body);
        Assert.IsTrue(body.IsEnabled);
        Assert.AreEqual("PBKDF2-SHA256", body.KdfAlgorithm);
        Assert.AreEqual("AAAAAAAAAAAAAAAAAAAAAA==", body.KdfSalt);
        Assert.AreEqual("{\"iterations\":600000}", body.KdfParams);
        Assert.AreEqual("AQIDBAUGB==", body.KeyVerifier);
    }

    [TestMethod]
    public async Task Enable_MissingFields_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/e2ee/enable", new { kdfAlgorithm = "PBKDF2-SHA256" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Disable ───────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Disable_AfterEnable_Returns204AndClearsSettings()
    {
        var client = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/e2ee/enable", new EnableE2eeRequest(
            "PBKDF2-SHA256", "AAAAAAAAAAAAAAAAAAAAAA==", "{}", "dGVzdA=="));

        var disableResp = await client.PostAsync("/api/e2ee/disable", null);
        Assert.AreEqual(HttpStatusCode.NoContent, disableResp.StatusCode);

        var settingsResp = await client.GetAsync("/api/e2ee/settings");
        var body = await settingsResp.Content.ReadFromJsonAsync<E2eeSettingsResponse>();
        Assert.IsNotNull(body);
        Assert.IsFalse(body.IsEnabled);
    }

    [TestMethod]
    public async Task Disable_WithNoSettingsRow_Returns204()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/e2ee/disable", null);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── ChangePassphrase ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task ChangePassphrase_WhenEnabled_Returns204AndReplacesArtefacts()
    {
        var client = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/e2ee/enable", new EnableE2eeRequest(
            "PBKDF2-SHA256", "AAAAAAAAAAAAAAAAAAAAAA==", "{}", "b2xkdmVyaWZpZXI="));

        var changeResp = await client.PutAsJsonAsync("/api/e2ee/change-passphrase",
            new ChangePassphraseRequest(
                "PBKDF2-SHA256",
                "BBBBBBBBBBBBBBBBBBBBBB==",
                "{\"iterations\":700000}",
                "bmV3dmVyaWZpZXI="));

        Assert.AreEqual(HttpStatusCode.NoContent, changeResp.StatusCode);

        var body = (await (await client.GetAsync("/api/e2ee/settings"))
            .Content.ReadFromJsonAsync<E2eeSettingsResponse>())!;
        Assert.AreEqual("BBBBBBBBBBBBBBBBBBBBBB==", body.KdfSalt);
        Assert.AreEqual("bmV3dmVyaWZpZXI=", body.KeyVerifier);
    }

    [TestMethod]
    public async Task ChangePassphrase_WhenNotEnabled_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/e2ee/change-passphrase",
            new ChangePassphraseRequest("PBKDF2-SHA256", "AAAAAAAAAAAAAAAAAAAAAA==", "{}", "dGVzdA=="));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── E2EE fields on messages ───────────────────────────────────────────────

    [TestMethod]
    public async Task CreateMessage_WithEncryptionFields_Returns201AndPersistsFlags()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/messages", new CreateMessageRequest(
            Domain.Entities.MessageKind.Text,
            "AQIDBAUGB/8=",   // simulated ciphertext
            null,
            true,
            "AAECBAUGB/8="));  // simulated IV

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var msg = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.IsNotNull(msg);
        Assert.IsTrue(msg.IsEncrypted);
        Assert.AreEqual("AAECBAUGB/8=", msg.EncryptionIV);
    }

    [TestMethod]
    public async Task UpdateMessage_WithEncryptionFields_UpdatesFlags()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(Domain.Entities.MessageKind.Text, "plaintext"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        var updateResp = await client.PutAsJsonAsync($"/api/messages/{created!.Id}", new
        {
            content = "AQIDBAUGB/8=",
            isEncrypted = true,
            encryptionIV = "AAECBAUGB/8="
        });

        Assert.AreEqual(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated.IsEncrypted);
        Assert.AreEqual("AAECBAUGB/8=", updated.EncryptionIV);
    }
}
