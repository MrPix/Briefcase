using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Briefcase.ApiService.Controllers;
using Briefcase.ApiService.Models;
using Briefcase.Domain.Entities;

namespace Briefcase.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class UserSettingsControllerTests
{
    private static ApiWebApplicationFactory _factory = null!;
    private static HttpClient _anonymousClient = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new ApiWebApplicationFactory();
        _factory.EnsureDatabaseCreated();
        _anonymousClient = _factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _anonymousClient.Dispose();
        await _factory.DisposeAsync();
    }

    [TestMethod]
    public async Task GetSettings_NewUser_ReturnsNavigationEnabledWithAllApplications()
    {
        var client = await CreateAuthenticatedClientAsync();

        var settings = await client.GetFromJsonAsync<UserSettingsResponse>("/api/users/settings");

        Assert.IsTrue(settings!.GoogleMapsNavigationEnabled);
        CollectionAssert.AreEqual(
            new[] { "google-maps", "waze", "locus-map", "maps-me" },
            settings.NavigationApplicationIds.ToArray());
        Assert.AreEqual(4, settings.NavigationApplications.Count);
    }

    [TestMethod]
    public async Task UpdateNavigationSettings_UnknownApplication_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/users/settings/navigation",
            new UpdateNavigationSettingsRequest(true, ["unknown"]));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task DisableThenEnable_BackfillsExistingGoogleMapsMessage()
    {
        var client = await CreateAuthenticatedClientAsync();
        var disabled = await client.PutAsJsonAsync("/api/users/settings/navigation",
            new UpdateNavigationSettingsRequest(false, ["waze"]));
        disabled.EnsureSuccessStatusCode();

        var create = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Url, "https://maps.app.goo.gl/example"));
        var created = await create.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.AreEqual(NavigationProcessingStatus.None, created!.NavigationStatus);

        var enabled = await client.PutAsJsonAsync("/api/users/settings/navigation",
            new UpdateNavigationSettingsRequest(true, ["waze"]));
        enabled.EnsureSuccessStatusCode();

        var completed = await WaitForMessageAsync(client, created.Id, NavigationProcessingStatus.Completed);
        Assert.AreEqual(1, completed.NavigationTargets.Count);
        Assert.AreEqual("waze", completed.NavigationTargets[0].ApplicationId);

        await client.PutAsJsonAsync("/api/users/settings/navigation",
            new UpdateNavigationSettingsRequest(false, ["waze"]));
        var cleared = await GetMessageAsync(client, created.Id);
        Assert.AreEqual(NavigationProcessingStatus.None, cleared.NavigationStatus);
        Assert.AreEqual(0, cleared.NavigationTargets.Count);
    }

    [TestMethod]
    public async Task RestoreGoogleMapsMessage_ReenabledWhileTrashed_QueuesProcessing()
    {
        var client = await CreateAuthenticatedClientAsync();
        var disabled = await client.PutAsJsonAsync("/api/users/settings/navigation",
            new UpdateNavigationSettingsRequest(false, ["maps-me"]));
        disabled.EnsureSuccessStatusCode();

        var create = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Url, "https://maps.app.goo.gl/restored"));
        var created = await create.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.AreEqual(NavigationProcessingStatus.None, created!.NavigationStatus);

        var deleted = await client.DeleteAsync($"/api/messages/{created.Id}");
        deleted.EnsureSuccessStatusCode();
        var enabled = await client.PutAsJsonAsync("/api/users/settings/navigation",
            new UpdateNavigationSettingsRequest(true, ["maps-me"]));
        enabled.EnsureSuccessStatusCode();

        var restored = await client.PostAsync($"/api/trash/{created.Id}/restore", null);
        restored.EnsureSuccessStatusCode();

        var completed = await WaitForMessageAsync(client, created.Id, NavigationProcessingStatus.Completed);
        Assert.AreEqual(1, completed.NavigationTargets.Count);
        Assert.AreEqual("maps-me", completed.NavigationTargets[0].ApplicationId);
    }

    [TestMethod]
    public async Task CreateEncryptedGoogleMapsMessage_DoesNotQueueProcessing()
    {
        var client = await CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(
                MessageKind.Url,
                "https://maps.app.goo.gl/encrypted",
                IsEncrypted: true,
                EncryptionIV: "dGVzdC1ub25jZQ=="));
        var created = await create.Content.ReadFromJsonAsync<MessageResponse>();

        Assert.AreEqual(NavigationProcessingStatus.None, created!.NavigationStatus);
        Assert.AreEqual(0, created.NavigationTargets.Count);
    }

    [TestMethod]
    public async Task EditCompletedGoogleMapsMessage_ToUnsupportedUrl_ClearsNavigation()
    {
        var client = await CreateAuthenticatedClientAsync();
        var create = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Url, "https://maps.app.goo.gl/edit"));
        var created = await create.Content.ReadFromJsonAsync<MessageResponse>();
        await WaitForMessageAsync(client, created!.Id, NavigationProcessingStatus.Completed);

        var update = await client.PutAsJsonAsync($"/api/messages/{created.Id}",
            new UpdateMessageRequest("https://example.com"));
        var updated = await update.Content.ReadFromJsonAsync<MessageResponse>();

        Assert.AreEqual(NavigationProcessingStatus.None, updated!.NavigationStatus);
        Assert.AreEqual(0, updated.NavigationTargets.Count);
    }

    private static async Task<MessageResponse> WaitForMessageAsync(
        HttpClient client,
        Guid id,
        NavigationProcessingStatus expected)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var message = await GetMessageAsync(client, id);
            if (message.NavigationStatus == expected) return message;
            await Task.Delay(100);
        }
        Assert.Fail($"Message {id} did not reach {expected}.");
        return null!;
    }

    private static async Task<MessageResponse> GetMessageAsync(HttpClient client, Guid id)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/messages?pageSize=100");
        return page!.Items.Single(message => message.Id == id);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"settings_{Guid.NewGuid():N}@test.com", "Password123!", "Test User"));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }
}