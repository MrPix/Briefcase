using System.Net.Http.Json;
using SavedMessages.Components.Services;
using SavedMessages.Domain.Entities;

namespace SavedMessages.Maui.Services;

public class MauiMessageService(IHttpClientFactory httpClientFactory) : IMessageService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ApiClient");

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(int page = 1, int pageSize = 20)
    {
        var client = CreateClient();
        var messages = await client.GetFromJsonAsync<List<Message>>($"api/messages?page={page}&pageSize={pageSize}");
        return messages?.AsReadOnly() ?? (IReadOnlyList<Message>)[];
    }

    public async Task<Message> CreateMessageAsync(MessageKind kind, string content)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/messages", new { kind, content });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Message>())!;
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        var client = CreateClient();
        var response = await client.DeleteAsync($"api/messages/{messageId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task TogglePinAsync(Guid messageId)
    {
        var client = CreateClient();
        var response = await client.PutAsync($"api/messages/{messageId}/pin", null);
        response.EnsureSuccessStatusCode();
    }
}
