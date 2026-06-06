using System.Net.Http.Json;
using Briefcase.Components.Services;
using Briefcase.Domain.Entities;

namespace Briefcase.Maui.Services;

public class MauiTrashService(IHttpClientFactory httpClientFactory, ITokenStorageService tokenStorage) : ITrashService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ApiClient");

    private record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    private record MessageResponse(
        Guid Id, MessageKind Kind, string? Content, Guid? FileId, string? FileName, string? FilePreviewUrl,
        bool IsPinned, bool IsEncrypted, DateTime CreatedAt, DateTime UpdatedAt);

    public async Task<IReadOnlyList<Message>> GetTrashedMessagesAsync(int page = 1, int pageSize = 50)
    {
        var client = CreateClient();
        var paged = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>($"api/trash?page={page}&pageSize={pageSize}");
        if (paged is null) return [];

        var accessToken = await tokenStorage.GetAccessTokenAsync();
        var apiBaseAddress = client.BaseAddress;

        return paged.Items.Select(r => new Message
        {
            Id = r.Id,
            Kind = r.Kind,
            Content = r.Content,
            FileId = r.FileId,
            FileName = r.FileName,
            FilePreviewUrl = AppendAccessToken(r.FilePreviewUrl, accessToken, apiBaseAddress),
            IsPinned = r.IsPinned,
            IsEncrypted = r.IsEncrypted,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList().AsReadOnly();
    }

    private static string? AppendAccessToken(string? previewUrl, string? accessToken, Uri? apiBaseAddress)
    {
        if (string.IsNullOrWhiteSpace(previewUrl))
            return previewUrl;

        var resolvedPreviewUrl = previewUrl;
        if (apiBaseAddress is not null && Uri.TryCreate(previewUrl, UriKind.Relative, out _))
            resolvedPreviewUrl = new Uri(apiBaseAddress, previewUrl).ToString();

        if (string.IsNullOrWhiteSpace(accessToken))
            return resolvedPreviewUrl;

        var separator = resolvedPreviewUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{resolvedPreviewUrl}{separator}access_token={Uri.EscapeDataString(accessToken)}";
    }

    public async Task RestoreMessageAsync(Guid messageId)
    {
        var client = CreateClient();
        var response = await client.PostAsync($"api/trash/{messageId}/restore", null);
        response.EnsureSuccessStatusCode();
    }
}
