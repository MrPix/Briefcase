using Briefcase.Components.Services;

namespace Briefcase.Maui.Services;

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
