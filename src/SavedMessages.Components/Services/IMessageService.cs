using SavedMessages.Domain.Entities;

namespace SavedMessages.Components.Services;

public interface IMessageService
{
    Task<IReadOnlyList<Message>> GetMessagesAsync(int page = 1, int pageSize = 20);
    Task<Message> CreateMessageAsync(MessageKind kind, string content);
    Task DeleteMessageAsync(Guid messageId);
    Task TogglePinAsync(Guid messageId);
}
