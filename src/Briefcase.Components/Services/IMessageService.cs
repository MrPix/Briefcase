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
}
