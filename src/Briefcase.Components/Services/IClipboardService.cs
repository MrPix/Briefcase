namespace Briefcase.Components.Services;

public interface IClipboardService
{
    Task<string?> GetTextAsync();
    Task SetTextAsync(string text);
}
