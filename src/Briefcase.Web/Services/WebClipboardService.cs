using Microsoft.JSInterop;
using Briefcase.Components.Services;

namespace Briefcase.Web.Services;

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
