using Microsoft.JSInterop;
using SavedMessages.Components.Services;

namespace SavedMessages.Web.Services;

public class WebClipboardService(IJSRuntime js) : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        return await js.InvokeAsync<string?>("navigator.clipboard.readText");
    }

    public async Task SetTextAsync(string text)
    {
        await js.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }
}
