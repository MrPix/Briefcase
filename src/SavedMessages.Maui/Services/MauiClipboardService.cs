using SavedMessages.Components.Services;

namespace SavedMessages.Maui.Services;

public class MauiClipboardService : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        return await Clipboard.Default.GetTextAsync();
    }

    public async Task SetTextAsync(string text)
    {
        await Clipboard.Default.SetTextAsync(text);
    }
}
