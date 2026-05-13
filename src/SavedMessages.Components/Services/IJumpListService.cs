namespace SavedMessages.Components.Services;

public interface IJumpListService
{
    Task UpdateRecentMessagesAsync(IEnumerable<(string title, string arguments)> recentItems);
    Task ClearAsync();
}
