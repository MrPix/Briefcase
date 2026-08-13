using Briefcase.Domain.Entities;

namespace Briefcase.Components.Services;

/// <summary>
/// Real-time stream of message changes pushed by the server over SignalR
/// (hub mounted at <c>/hubs/messages</c>). Lets a connected client — such as a
/// browser left open on a computer — update instantly when another device
/// (e.g. a tablet) creates, edits, pins, trashes or restores a message.
/// </summary>
public interface IMessageStreamService
{
    /// <summary>Raised when a new message is created (or restored from trash) on any device.</summary>
    event Func<Message, Task>? MessageCreated;

    /// <summary>Raised when an existing message is edited or pinned/unpinned on any device.</summary>
    event Func<Message, Task>? MessageUpdated;

    /// <summary>Raised when a message is trashed or permanently deleted on any device.</summary>
    event Func<Guid, Task>? MessageRemoved;

    /// <summary>Raised when this device was removed from the account and its session revoked.</summary>
    event Func<Task>? SessionRevoked;

    /// <summary>
    /// Opens the authenticated SignalR connection and begins receiving events.
    /// Idempotent — calling it while already connected is a no-op.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Closes the SignalR connection if it is open.</summary>
    Task StopAsync();
}
