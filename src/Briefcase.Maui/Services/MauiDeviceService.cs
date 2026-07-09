using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Briefcase.Components.Services;
using Device = Briefcase.Domain.Entities.Device;

namespace Briefcase.Maui.Services;

public class MauiDeviceService(IHttpClientFactory httpClientFactory) : IDeviceService
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

    public async Task<LoginCodeInfo> GenerateLoginCodeAsync(string deviceName, string platform)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/devices/login-code", new { deviceName, platform });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginCodeResponse>();
        return result is null
            ? throw new InvalidOperationException("No login code returned.")
            : new LoginCodeInfo(result.Code, result.ExpiresAt);
    }

    public async Task<LoginCodePollResult> PollLoginCodeAsync(string code)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"api/devices/login-code/{Uri.EscapeDataString(code)}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginCodePollResponse>()
            ?? throw new InvalidOperationException("No poll response returned.");
        return result.ToResult();
    }

    public async Task<LoginCodePollResult> WaitForLoginApprovalAsync(string code, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var hubUrl = new Uri(client.BaseAddress!, "/hubs/messages").ToString();

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        var approved = new SemaphoreSlim(0);
        connection.On<JsonElement>("LoginCodeApproved", _ => approved.Release());

        try
        {
            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinLoginCode", code, cancellationToken: cancellationToken);

            // Redeem immediately in case approval happened before we connected.
            var initial = await PollLoginCodeAsync(code);
            if (initial.Status != LoginCodeStatus.Pending)
                return initial;

            while (!cancellationToken.IsCancellationRequested)
            {
                await approved.WaitAsync(cancellationToken);
                var result = await PollLoginCodeAsync(code);
                if (result.Status != LoginCodeStatus.Pending)
                    return result;
            }

            return new LoginCodePollResult(LoginCodeStatus.Pending);
        }
        finally
        {
            await connection.StopAsync(CancellationToken.None);
            await connection.DisposeAsync();
        }
    }

    public async Task<string> ApproveLoginCodeAsync(string code)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/devices/login-code/approve", new { code });
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            throw new InvalidOperationException(problem?.Title ?? "Failed to approve login code.");
        }

        var result = await response.Content.ReadFromJsonAsync<ApproveLoginCodeResponse>();
        return result?.DeviceName ?? "the device";
    }

    private record PairCodeResponse(string Token);

    private record LoginCodeResponse(string Code, DateTime ExpiresAt);

    private record ApproveLoginCodeResponse(string DeviceName);

    private sealed class ProblemDetails
    {
        public string? Title { get; set; }
    }

    private record LoginCodePollResponse(
        string Status,
        string? AccessToken,
        string? RefreshToken,
        DateTime? AccessTokenExpiresAt)
    {
        public LoginCodePollResult ToResult() => Status switch
        {
            "approved" when AccessToken is not null && RefreshToken is not null && AccessTokenExpiresAt is not null =>
                new LoginCodePollResult(LoginCodeStatus.Approved,
                    new AuthResult(AccessToken, RefreshToken, AccessTokenExpiresAt.Value)),
            "expired" => new LoginCodePollResult(LoginCodeStatus.Expired),
            "notfound" => new LoginCodePollResult(LoginCodeStatus.NotFound),
            _ => new LoginCodePollResult(LoginCodeStatus.Pending),
        };
    }
}
