using System.Net.Http.Json;
using SavedMessages.Components.Services;
using SavedMessages.Domain.Entities;

namespace SavedMessages.Web.Services;

public class WebDeviceService(IHttpClientFactory httpClientFactory) : IDeviceService
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("ApiClient");

    public async Task<IReadOnlyList<Device>> GetDevicesAsync()
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<IReadOnlyList<Device>>("api/devices") ?? [];
    }

    public async Task RemoveDeviceAsync(Guid deviceId)
    {
        var client = CreateClient();
        var response = await client.DeleteAsync($"api/devices/{deviceId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GeneratePairCodeAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsync("api/devices/pair-code", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PairCodeResponse>();
        return result?.Token ?? throw new InvalidOperationException("No pair code returned.");
    }

    public async Task ClaimDeviceAsync(string token)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/devices/claim", new { token });
        response.EnsureSuccessStatusCode();
    }

    private record PairCodeResponse(string Token);
}
