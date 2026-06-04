using SavedMessages.Domain.Entities;

namespace SavedMessages.Components.Services;

public interface ITrashService
{
    Task<IReadOnlyList<Message>> GetTrashedMessagesAsync(int page = 1, int pageSize = 50);
    Task RestoreMessageAsync(Guid messageId);
}
