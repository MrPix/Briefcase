using System.Collections.Concurrent;

namespace SavedMessages.ApiService.Services;

/// <summary>
/// In-memory store for anonymous quick-transfer sessions (TTL: 10 minutes, single-use).
/// Replace with a distributed cache (Redis / Azure Cache for Redis) before scaling horizontally.
/// </summary>
public sealed class TransferSessionService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);

    private sealed record Session(string Id, DateTimeOffset ExpiresAt)
    {
        public string? Content { get; set; }
        public DateTimeOffset? ClaimedAt { get; set; }
    }

    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    /// <summary>Creates a new transfer session and returns its ID.</summary>
    public string CreateSession()
    {
        var id = Guid.NewGuid().ToString("N");
        var session = new Session(id, DateTimeOffset.UtcNow.Add(SessionTtl));
        _sessions[id] = session;
        PurgeExpired();
        return id;
    }

    /// <summary>
    /// Pushes <paramref name="content"/> into the session identified by <paramref name="sessionId"/>.
    /// Returns <c>true</c> if the session existed, was unexpired, and had not already been claimed.
    /// </summary>
    public bool TryPush(string sessionId, string content)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        if (session.ExpiresAt < DateTimeOffset.UtcNow || session.ClaimedAt.HasValue)
            return false;

        session.Content = content;
        session.ClaimedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>Returns the session if it exists and has not expired.</summary>
    public (bool Found, string? Content, bool Expired) TryGet(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return (false, null, false);

        if (session.ExpiresAt < DateTimeOffset.UtcNow)
            return (true, null, true);

        return (true, session.Content, false);
    }

    private void PurgeExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, session) in _sessions)
        {
            if (session.ExpiresAt < now)
                _sessions.TryRemove(key, out _);
        }
    }
}
