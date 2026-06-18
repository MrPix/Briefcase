using Briefcase.Domain.Entities;

namespace Briefcase.Components.Services;

public interface ITransferService
{
    /// <summary>Creates an anonymous transfer session and returns the 8-character code.</summary>
    Task<string> CreateSessionAsync();

    /// <summary>Pushes arbitrary text content into a session (legacy push flow).</summary>
    Task PushContentAsync(string sessionId, string content);

    /// <summary>
    /// Sends a message to the device identified by <paramref name="code"/>.
    /// Creates a one-time share link and pushes its URL via SignalR.
    /// Requires the caller to be authenticated.
    /// </summary>
    Task SendToAsync(string code, Guid messageId);

    /// <summary>
    /// Subscribes to the transfer session group and invokes <paramref name="onUrlReceived"/>
    /// when the sender pushes a share-link URL.  Completes when the <paramref name="ct"/> is
    /// cancelled or the URL is received.
    /// </summary>
    Task ListenForTransferAsync(string code, Func<string, Task> onUrlReceived, CancellationToken ct = default);

    /// <summary>Fetches the content of a one-time share link by its slug.</summary>
    Task<SharedMessageResult?> GetSharedMessageAsync(string slug);
}

public record SharedMessageResult(
    Guid MessageId,
    MessageKind Kind,
    string? Content,
    Guid? FileId,
    string? FileName,
    string? FilePreviewUrl,
    DateTime CreatedAt);
