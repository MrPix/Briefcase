using System.Net.Http.Json;
using SavedMessages.Components.Services;
using SavedMessages.Domain.Entities;

namespace SavedMessages.Maui.Services;

public class MauiMessageService(IHttpClientFactory httpClientFactory) : IMessageService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ApiClient");

    private record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    private record MessageResponse(
        Guid Id, MessageKind Kind, string? Content, Guid? FileId,
        bool IsPinned, bool IsEncrypted, DateTime CreatedAt, DateTime UpdatedAt);

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(int page = 1, int pageSize = 20)
    {
        var client = CreateClient();
        var paged = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>($"api/messages?page={page}&pageSize={pageSize}");
        if (paged is null) return [];

        return paged.Items.Select(r => new Message
        {
            Id = r.Id,
            Kind = r.Kind,
            Content = r.Content,
            FileId = r.FileId,
            IsPinned = r.IsPinned,
            IsEncrypted = r.IsEncrypted,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList().AsReadOnly();
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
        var response = await client.PatchAsync($"api/messages/{messageId}/pin", null);
        response.EnsureSuccessStatusCode();
    }
}
