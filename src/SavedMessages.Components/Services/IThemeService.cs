namespace SavedMessages.Components.Services;

public interface IThemeService
{
    event Action<string>? ThemeChanged;
    Task<string> GetCurrentThemeAsync();
    Task ApplyThemeAsync();
}
