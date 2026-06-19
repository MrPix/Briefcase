using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Briefcase.ApiService.Services;

/// <summary>
/// In-memory store for anonymous quick-transfer sessions (TTL: 10 minutes, single-use).
/// Sessions are identified by a human-readable 8-character alphanumeric code.
/// Replace with a distributed cache (Redis / Azure Cache for Redis) before scaling horizontally.
/// </summary>
public sealed class TransferSessionService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);

    // Avoid visually ambiguous characters (0/O, 1/I/l)
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private sealed record Session(string Code, DateTimeOffset ExpiresAt)
    {
        public string? Content { get; set; }
        public DateTimeOffset? ClaimedAt { get; set; }
    }

    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    /// <summary>Creates a new transfer session and returns its 8-character code.</summary>
    public string CreateSession()
    {
        var code = GenerateCode();
        var session = new Session(code, DateTimeOffset.UtcNow.Add(SessionTtl));
        _sessions[code] = session;
        PurgeExpired();
        return code;
    }

    private static string GenerateCode() => RandomNumberGenerator.GetString(CodeChars, 8);

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
