using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SavedMessages.ApiService.Hubs;

/// <summary>
/// SignalR hub mounted at /hubs/messages.
/// Every authenticated client joins a group keyed by their UserId so the server
/// can push events to all of a user's connected devices simultaneously.
/// </summary>
[Authorize]
public class MessageHub : Hub
{
    // Server → client event names (match client-side expectations exactly)
    public const string MessageCreated    = nameof(MessageCreated);
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

    private string? GetUserId() =>
        Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}
