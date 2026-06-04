using System.Net.Http.Json;
using SavedMessages.Components.Services;
using SavedMessages.Domain.Entities;

namespace SavedMessages.Web.Services;

public class WebTrashService(IHttpClientFactory httpClientFactory) : ITrashService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ApiClient");

    private record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    private record MessageResponse(
        Guid Id, MessageKind Kind, string? Content, Guid? FileId,
        bool IsPinned, bool IsEncrypted, DateTime CreatedAt, DateTime UpdatedAt);

    public async Task<IReadOnlyList<Message>> GetTrashedMessagesAsync(int page = 1, int pageSize = 50)
    {
        var client = CreateClient();
        var paged = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>($"api/trash?page={page}&pageSize={pageSize}");
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

    public async Task RestoreMessageAsync(Guid messageId)
    {
        var client = CreateClient();
        var response = await client.PostAsync($"api/trash/{messageId}/restore", null);
        response.EnsureSuccessStatusCode();
    }
}
