using Briefcase.Domain.Entities;

namespace Briefcase.Components.Services;

public interface IMessageService
{
    Task<IReadOnlyList<Message>> GetMessagesAsync(int page = 1, int pageSize = 20);
    Task<Message> CreateMessageAsync(MessageKind kind, string content,
        bool isEncrypted = false, string? encryptionIV = null);
    Task<Message> UploadFileAsync(string fileName, string contentType, Stream fileStream, string? comment = null);
    Task<(byte[] Data, string ContentType, string FileName)> DownloadFileAsync(Guid fileId);
    Task DeleteMessageAsync(Guid messageId);
    Task EditMessageAsync(Guid messageId, string? content,
        bool isEncrypted = false, string? encryptionIV = null);
    Task TogglePinAsync(Guid messageId);

    /// <summary>
    /// Creates a public share link for a note or file. Set <paramref name="oneTime"/> to make the
    /// link self-destruct after the first view, and/or <paramref name="expiresInMinutes"/> to make
    /// it expire after a fixed duration (null = never expires).
    /// </summary>
    Task<ShareLinkResult> CreateShareLinkAsync(Guid messageId, bool oneTime, int? expiresInMinutes);

    /// <summary>Revokes all active public share links for the given message.</summary>
    Task RevokeShareLinkAsync(Guid messageId);
}

public record ShareLinkResult(
    string Slug,
    string Url,
    DateTime? ExpiresAt,
    bool OneTime);