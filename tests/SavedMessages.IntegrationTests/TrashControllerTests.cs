using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SavedMessages.ApiService.Models;
using SavedMessages.Domain.Entities;

namespace SavedMessages.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class TrashControllerTests
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
        var email = $"trash_{Guid.NewGuid():N}@test.com";
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
    public async Task GetTrash_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/trash");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── List trashed messages ─────────────────────────────────────────────────

    [TestMethod]
    public async Task GetTrash_AfterDeletingMessage_ReturnsItInTrash()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create and soft-delete a message
        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Trash candidate"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();
        await client.DeleteAsync($"/api/messages/{created!.Id}");

        var trash = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/trash");

        Assert.IsTrue(trash!.Items.Any(m => m.Id == created.Id));
    }

    [TestMethod]
    public async Task GetTrash_NeverDeletedMessages_EmptyList()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create a message but do NOT delete it
        await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Still active"));

        var trash = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/trash");

        Assert.AreEqual(0, trash!.TotalCount);
    }

    [TestMethod]
    public async Task GetTrash_OnlyReturnsCurrentUsersTrash()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        var createResp = await clientA.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "User A's deleted"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();
        await clientA.DeleteAsync($"/api/messages/{created!.Id}");

        // User B's trash should be empty
        var trashB = await clientB.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/trash");

        Assert.IsFalse(trashB!.Items.Any(m => m.Id == created.Id));
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Restore_DeletedMessage_MovesItBackToMessages()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "Restore me"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();
        await client.DeleteAsync($"/api/messages/{created!.Id}");

        var restoreResp = await client.PostAsync(
            $"/api/trash/{created.Id}/restore", null);

        Assert.AreEqual(HttpStatusCode.OK, restoreResp.StatusCode);
        var restored = await restoreResp.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.AreEqual(created.Id, restored!.Id);

        // Confirm it is back in the active list
        var list = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/messages");
        Assert.IsTrue(list!.Items.Any(m => m.Id == created.Id));

        // Confirm it is gone from trash
        var trash = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/api/trash");
        Assert.IsFalse(trash!.Items.Any(m => m.Id == created.Id));
    }

    [TestMethod]
    public async Task Restore_NonExistentId_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync(
            $"/api/trash/{Guid.NewGuid()}/restore", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Restore_OtherUsersMessage_Returns404()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        var createResp = await clientA.PostAsJsonAsync("/api/messages",
            new CreateMessageRequest(MessageKind.Text, "User A's deleted"));
        var created = await createResp.Content.ReadFromJsonAsync<MessageResponse>();
        await clientA.DeleteAsync($"/api/messages/{created!.Id}");

        // User B tries to restore User A's trashed message
        var response = await clientB.PostAsync(
            $"/api/trash/{created.Id}/restore", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
