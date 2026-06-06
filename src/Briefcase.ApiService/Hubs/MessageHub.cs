using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Briefcase.ApiService.Services;

namespace Briefcase.ApiService.Hubs;

/// <summary>
/// SignalR hub mounted at /hubs/messages.
/// Authenticated clients join a group keyed by their UserId so the server
/// can push events to all of a user's connected devices simultaneously.
/// Anonymous clients may join a transfer-session group (§3.4).
/// </summary>
public class MessageHub(TransferSessionService sessions) : Hub
{
    // Server → client event names (match client-side expectations exactly)
    public const string MessageCreated    = nameof(MessageCreated);
    public const string MessageUpdated    = nameof(MessageUpdated);
    public const string MessageDeleted    = nameof(MessageDeleted);
    public const string MessageTrashed    = nameof(MessageTrashed);
    public const string MessageRestored   = nameof(MessageRestored);
    public const string TransferReceived  = nameof(TransferReceived);
    public const string ShareLinkCreated  = nameof(ShareLinkCreated);
    public const string ShareLinkRevoked  = nameof(ShareLinkRevoked);
    public const string E2eeSettingsChanged = nameof(E2eeSettingsChanged);

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows a client to join a transfer-session group so it receives
    /// the <see cref="TransferReceived"/> event when content is pushed.
    /// The session must exist and not be expired or already claimed (§3.4, §5).
    /// No authentication required — session ID is the auth.
    /// </summary>
    public async Task JoinTransferSession(string sessionId)
    {
        var (found, _, expired) = sessions.TryGet(sessionId);
        if (!found || expired)
            throw new HubException("Invalid or expired transfer session.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"transfer:{sessionId}");
    }

    /// <summary>
    /// Removes the client from a transfer-session group.
    /// </summary>
    public async Task LeaveTransferSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"transfer:{sessionId}");
    }

    private string? GetUserId() =>
        Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}
