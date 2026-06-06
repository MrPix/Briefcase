using System.Net.Http.Json;
using Briefcase.Components.Services;

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

    private record SessionResponse(string SessionId);
}
