using System.Net.Http.Json;
using Briefcase.Components.Services;
using Briefcase.Domain.Entities;

namespace Briefcase.Web.Services;

public class WebMessageService(IHttpClientFactory httpClientFactory, ITokenStorageService tokenStorage) : IMessageService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ApiClient");

    private record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    private record MessageResponse(
        Guid Id, MessageKind Kind, string? Content, Guid? FileId, string? FileName, string? FilePreviewUrl,
        bool IsPinned, DateTime? PinnedAt, bool IsEncrypted, string? EncryptionIV, DateTime CreatedAt, DateTime UpdatedAt);

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(int page = 1, int pageSize = 20)
    {
        var client = CreateClient();
        var paged = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>($"api/messages?page={page}&pageSize={pageSize}");
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
            Downloaded = false,
            IsPinned = r.IsPinned,
            PinnedAt = r.PinnedAt,
            IsEncrypted = r.IsEncrypted,
            EncryptionIV = r.EncryptionIV,
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

    public async Task<Message> CreateMessageAsync(MessageKind kind, string content,
        bool isEncrypted = false, string? encryptionIV = null)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/messages",
            new { kind, content, isEncrypted, encryptionIV });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Message>())!;
    }

    public async Task<(byte[] Data, string ContentType, string FileName)> DownloadFileAsync(Guid fileId)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"api/files/{fileId}");
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "download";
        return (data, contentType, fileName);
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        var client = CreateClient();
        var response = await client.DeleteAsync($"api/messages/{messageId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task EditMessageAsync(Guid messageId, string? content,
        bool isEncrypted = false, string? encryptionIV = null)
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync($"api/messages/{messageId}",
            new { content, isEncrypted, encryptionIV });
        response.EnsureSuccessStatusCode();
    }

    public async Task<Message> UploadFileAsync(string fileName, string contentType, Stream fileStream, string? comment = null)
    {
        var client = CreateClient();

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var uploadResponse = await client.PostAsync("api/files", content);
        uploadResponse.EnsureSuccessStatusCode();
        var fileResult = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponse>();

        var messageContent = string.IsNullOrWhiteSpace(comment) ? fileName : comment;
        var msgResponse = await client.PostAsJsonAsync("api/messages", new { kind = MessageKind.File, content = messageContent, fileId = fileResult!.Id });
        msgResponse.EnsureSuccessStatusCode();
        return (await msgResponse.Content.ReadFromJsonAsync<Message>())!;
    }

    private record FileUploadResponse(Guid Id, string OriginalName, string ContentType, long SizeBytes, DateTime CreatedAt);

    public async Task TogglePinAsync(Guid messageId)
    {
        var client = CreateClient();
        var response = await client.PatchAsync($"api/messages/{messageId}/pin", null);
        response.EnsureSuccessStatusCode();
    }
}
