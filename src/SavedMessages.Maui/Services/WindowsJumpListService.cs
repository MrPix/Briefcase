#if WINDOWS
using SavedMessages.Components.Services;

namespace SavedMessages.Maui.Services;

public class WindowsJumpListService : IJumpListService
{
    public async Task UpdateRecentMessagesAsync(IEnumerable<(string title, string arguments)> recentItems)
    {
        try
        {
            if (!Windows.UI.StartScreen.JumpList.IsSupported())
                return;

            var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();
            jumpList.Items.Clear();
            jumpList.SystemGroupKind = Windows.UI.StartScreen.JumpListSystemGroupKind.None;

            // Add task items
            var newMessage = Windows.UI.StartScreen.JumpListItem.CreateWithArguments("action=new-message", "New Message");
            newMessage.GroupName = "Tasks";
            jumpList.Items.Add(newMessage);

            var quickTransfer = Windows.UI.StartScreen.JumpListItem.CreateWithArguments("action=quick-transfer", "Quick Transfer");
            quickTransfer.GroupName = "Tasks";
            jumpList.Items.Add(quickTransfer);

            var openClipboard = Windows.UI.StartScreen.JumpListItem.CreateWithArguments("action=open-clipboard", "Open Clipboard");
            openClipboard.GroupName = "Tasks";
            jumpList.Items.Add(openClipboard);

            // Add recent message snippets
            foreach (var (title, arguments) in recentItems.Take(5))
            {
                var item = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(arguments, title);
                item.GroupName = "Recent";
                jumpList.Items.Add(item);
            }

            await jumpList.SaveAsync();
        }
        catch
        {
            // Jump list may not be available in unpackaged mode
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            if (!Windows.UI.StartScreen.JumpList.IsSupported())
                return;

            var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();
            jumpList.Items.Clear();
            await jumpList.SaveAsync();
        }
        catch
        {
            // Jump list may not be available in unpackaged mode
        }
    }
}
#endif
