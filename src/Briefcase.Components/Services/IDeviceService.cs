using Briefcase.Domain.Entities;

namespace Briefcase.Components.Services;

public interface IDeviceService
{
    Task<IReadOnlyList<Device>> GetDevicesAsync();
    Task RemoveDeviceAsync(Guid deviceId);
    Task<string> GeneratePairCodeAsync();
    Task ClaimDeviceAsync(string token);

    /// <summary>
    /// Requests a short login code for this (not-yet-authenticated) device so it can be
    /// added to an existing account from an already-signed-in device.
    /// </summary>
    Task<LoginCodeInfo> GenerateLoginCodeAsync(string deviceName, string platform);

    /// <summary>Polls the server to see whether a login code has been approved.</summary>
    Task<LoginCodePollResult> PollLoginCodeAsync(string code);

    /// <summary>
    /// Waits (over a SignalR connection) until a login code is approved, expires, or is
    /// cancelled, then redeems and returns the resulting tokens. Mirrors the share-to-device flow.
    /// </summary>
    Task<LoginCodePollResult> WaitForLoginApprovalAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Approves a login code entered on this authenticated device.</summary>
    Task<string> ApproveLoginCodeAsync(string code);
}

public record LoginCodeInfo(string Code, DateTime ExpiresAt);

public enum LoginCodeStatus
{
    Pending,
    Approved,
    Expired,
    NotFound
}

public record LoginCodePollResult(LoginCodeStatus Status, AuthResult? Auth = null);
