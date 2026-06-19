using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Briefcase.Components.Services;
using Briefcase.Domain.Entities;

namespace Briefcase.Maui.Services;

public class MauiTransferService(IHttpClientFactory httpClientFactory) : ITransferService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ApiClient");

    public async Task<string> CreateSessionAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsync("api/transfer/session", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SessionResponse>();
        return result?.SessionId ?? throw new InvalidOperationException("No session ID returned.");
    }

    public async Task PushContentAsync(string sessionId, string content)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/transfer/push", new { sessionId, content });
        response.EnsureSuccessStatusCode();
    }

    public async Task SendToAsync(string code, Guid messageId)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/transfer/send", new { code, messageId });
        response.EnsureSuccessStatusCode();
    }

    public async Task ListenForTransferAsync(string code, Func<string, Task> onUrlReceived, CancellationToken ct = default)
    {
        var client = CreateClient();
        var hubUrl = new Uri(client.BaseAddress!, "/hubs/messages").ToString();

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        connection.On<JsonElement>("TransferReceived", async data =>
        {
            if (data.TryGetProperty("url", out var urlProp))
            {
                var url = urlProp.GetString();
                if (url is not null)
                    await onUrlReceived(url);
            }
        });

        try
        {
            await connection.StartAsync(ct);
            await connection.InvokeAsync("JoinTransferSession", code, cancellationToken: ct);
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException) { /* normal cancellation */ }
        finally
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }

    public async Task<SharedMessageResult?> GetSharedMessageAsync(string slug)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"api/share/{Uri.EscapeDataString(slug)}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<SharedMessageDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dto is null) return null;

        var baseUrl = client.BaseAddress!.ToString().TrimEnd('/');

        var previewUrl = dto.FilePreviewToken is not null
            ? $"{baseUrl}/api/share/{Uri.EscapeDataString(slug)}/preview?token={Uri.EscapeDataString(dto.FilePreviewToken)}"
            : null;

        var downloadUrl = dto.FileDownloadToken is not null
            ? $"{baseUrl}/api/share/{Uri.EscapeDataString(slug)}/file?token={Uri.EscapeDataString(dto.FileDownloadToken)}"
            : null;

        return new SharedMessageResult(
            dto.MessageId,
            Enum.Parse<MessageKind>(dto.Kind, ignoreCase: true),
            dto.Content,
            dto.FileId,
            dto.FileName,
            previewUrl,
            downloadUrl,
            dto.CreatedAt);
    }

    private record SessionResponse(string SessionId);

    private record SharedMessageDto(
        Guid MessageId,
        string Kind,
        string? Content,
        Guid? FileId,
        string? FileName,
        string? FilePreviewToken,
        DateTime CreatedAt,
        string? FileDownloadToken);
}
