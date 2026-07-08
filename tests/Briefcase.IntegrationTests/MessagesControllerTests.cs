using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Briefcase.ApiService.Controllers;
using Briefcase.ApiService.Models;
using Briefcase.Domain.Entities;

namespace Briefcase.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class MessagesControllerTests
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

    /// <summary>Registers a new user and returns an authenticated HttpClient.</summary>
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var email = $"msg_{Guid.NewGuid():N}@test.com";
        var resp = await _anonClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Password123!", "Test User"));
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    // ── Auth guard ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetMessages_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/messages");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task CreateMessage_TextKind_Returns201WithCorrectBody()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Hello, world!"));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var msg = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.IsNotNull(msg);
        Assert.AreEqual("Hello, world!", msg.Content);
        Assert.AreEqual(MessageKind.Text, msg.Kind);
        Assert.IsFalse(msg.IsPinned);
        Assert.IsFalse(msg.IsEncrypted);
        Assert.AreNotEqual(Guid.Empty, msg.Id);
    }

    [TestMethod]
    public async Task CreateMessage_UrlKind_Returns201()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Url, "https://example.com"));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateMessage_Unauthenticated_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Sneak"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetMessages_ReturnsOnlyCallerMessages()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        var sentByA = $"A_{Guid.NewGuid():N}";
        var sentByB = $"B_{Guid.NewGuid():N}";
        await clientA.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, sentByA));
        await clientB.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, sentByB));

        var listA = await clientA.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/messages");
        var listB = await clientB.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/messages");

        Assert.IsTrue(listA!.Items.Any(m => m.Content == sentByA));
        Assert.IsFalse(listA.Items.Any(m => m.Content == sentByB));
        Assert.IsTrue(listB!.Items.Any(m => m.Content == sentByB));
        Assert.IsFalse(listB.Items.Any(m => m.Content == sentByA));
    }

    [TestMethod]
    public async Task GetMessages_PageSizeParam_LimitsResults()
    {
        var client = await CreateAuthenticatedClientAsync();
        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/messages",
                new CreateMessageRequest(MessageKind.Text, $"Message {i}"));

        var response = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
            "/api/messages?pageSize=2");

        Assert.AreEqual(2, response!.Items.Count);
        Assert.AreEqual(2, response.PageSize);
    }

    [TestMethod]
    public async Task GetMessages_DeletedMessages_NotIncluded()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "To be deleted"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        await client.DeleteAsync($"/api/messages/{created!.Id}");

        var list = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/messages");
        Assert.IsFalse(list!.Items.Any(m => m.Id == created.Id));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateMessage_ValidId_Returns200WithUpdatedContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Original"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        var updateResp = await client.PutAsJsonAsync(
            $"/api/messages/{created!.Id}",
            new UpdateMessageRequest("Updated content"));

        Assert.AreEqual(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.AreEqual("Updated content", updated!.Content);
    }

    [TestMethod]
    public async Task UpdateMessage_OtherUsersMessage_Returns404()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        var createResp = await clientA.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "User A"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        var response = await clientB.PutAsJsonAsync(
            $"/api/messages/{created!.Id}",
            new UpdateMessageRequest("Hijacked"));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateMessage_NonExistentId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/messages/{Guid.NewGuid()}",
            new UpdateMessageRequest("Ghost update"));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Toggle Pin ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task TogglePin_UnpinnedMessage_PinsIt()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Pin me"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        var response = await client.PatchAsJsonAsync(
            $"/api/messages/{created!.Id}/pin", new { });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.IsTrue(result!.IsPinned);
        Assert.IsNotNull(result.PinnedAt);
    }

    [TestMethod]
    public async Task TogglePin_PinnedMessage_UnpinsIt()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Toggle"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        // First toggle: pin
        await client.PatchAsJsonAsync($"/api/messages/{created!.Id}/pin", new { });
        // Second toggle: unpin
        var response = await client.PatchAsJsonAsync(
            $"/api/messages/{created.Id}/pin", new { });

        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.IsFalse(result!.IsPinned);
        Assert.IsNull(result.PinnedAt);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task DeleteMessage_ValidId_Returns204AndMessageAbsentFromList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Delete me"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        var deleteResp = await client.DeleteAsync($"/api/messages/{created!.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var list = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/messages");
        Assert.IsFalse(list!.Items.Any(m => m.Id == created.Id));
    }

    [TestMethod]
    public async Task DeleteMessage_OtherUsersMessage_Returns404()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        var createResp = await clientA.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "User A's"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();

        var response = await clientB.DeleteAsync($"/api/messages/{created!.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteMessage_NonExistentId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/messages/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Share links ───────────────────────────────────────────────────────────

    private async Task<Guid> CreateTextMessageAsync(HttpClient client, string content)
    {
        var resp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, content));
        var msg = await resp.Content.ReadFromJsonAsync<MessageResponse>();
        return msg!.Id;
    }

    [TestMethod]
    public async Task CreateShareLink_ReturnsSlug_AndPubliclyViewable()
    {
        var client = await CreateAuthenticatedClientAsync();
        var messageId = await CreateTextMessageAsync(client, "Shareable note");

        var response = await client.PostAsJsonAsync($"/api/messages/{messageId}/share",
            new CreateShareLinkRequest(OneTime: false, ExpiresInMinutes: 60));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<ShareLinkResponse>();
        Assert.IsNotNull(link);
        Assert.IsFalse(string.IsNullOrWhiteSpace(link.Slug));
        Assert.AreEqual($"/share/{link.Slug}", link.Url);
        Assert.IsNotNull(link.ExpiresAt);

        // The slug is anonymously viewable.
        var shared = await _anonClient.GetFromJsonAsync<SharedMessageDto>($"/api/share/{link.Slug}");
        Assert.IsNotNull(shared);
        Assert.AreEqual("Shareable note", shared.Content);
    }

    [TestMethod]
    public async Task CreateShareLink_OneTime_SelfDestructsAfterFirstView()
    {
        var client = await CreateAuthenticatedClientAsync();
        var messageId = await CreateTextMessageAsync(client, "Burn after reading");

        var response = await client.PostAsJsonAsync($"/api/messages/{messageId}/share",
            new CreateShareLinkRequest(OneTime: true, ExpiresInMinutes: null));
        var link = await response.Content.ReadFromJsonAsync<ShareLinkResponse>();

        // First view succeeds.
        var first = await _anonClient.GetAsync($"/api/share/{link!.Slug}");
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);

        // Second view is revoked.
        var second = await _anonClient.GetAsync($"/api/share/{link.Slug}");
        Assert.AreEqual(HttpStatusCode.NotFound, second.StatusCode);
    }

    [TestMethod]
    public async Task RevokeShareLink_MakesLinkUnavailable()
    {
        var client = await CreateAuthenticatedClientAsync();
        var messageId = await CreateTextMessageAsync(client, "Revoke me");

        var createResp = await client.PostAsJsonAsync($"/api/messages/{messageId}/share",
            new CreateShareLinkRequest(OneTime: false, ExpiresInMinutes: null));
        var link = await createResp.Content.ReadFromJsonAsync<ShareLinkResponse>();

        var revokeResp = await client.DeleteAsync($"/api/messages/{messageId}/share");
        Assert.AreEqual(HttpStatusCode.NoContent, revokeResp.StatusCode);

        var view = await _anonClient.GetAsync($"/api/share/{link!.Slug}");
        Assert.AreEqual(HttpStatusCode.NotFound, view.StatusCode);
    }

    [TestMethod]
    public async Task CreateShareLink_OtherUsersMessage_Returns404()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var messageId = await CreateTextMessageAsync(clientA, "Private");

        var response = await clientB.PostAsJsonAsync($"/api/messages/{messageId}/share",
            new CreateShareLinkRequest());

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateShareLink_Unauthenticated_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync($"/api/messages/{Guid.NewGuid()}/share",
            new CreateShareLinkRequest());

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RevokeShareLink_NoActiveLinks_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var messageId = await CreateTextMessageAsync(client, "No links yet");

        var response = await client.DeleteAsync($"/api/messages/{messageId}/share");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
