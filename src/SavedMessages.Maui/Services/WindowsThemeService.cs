#if WINDOWS
using Microsoft.JSInterop;
using Microsoft.UI.Xaml;
using SavedMessages.Components.Services;

namespace SavedMessages.Maui.Services;

public class WindowsThemeService : IThemeService, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = "light";

    public event Action<string>? ThemeChanged;

    public WindowsThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;

        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        uiSettings.ColorValuesChanged += OnSystemThemeChanged;

        _currentTheme = GetSystemTheme();
    }

    public Task<string> GetCurrentThemeAsync()
    {
        _currentTheme = GetSystemTheme();
        return Task.FromResult(_currentTheme);
    }

    public async Task ApplyThemeAsync()
    {
        _currentTheme = GetSystemTheme();
        await SetBodyThemeClassAsync(_currentTheme);
        ThemeChanged?.Invoke(_currentTheme);
    }

    private string GetSystemTheme()
    {
        var app = Microsoft.UI.Xaml.Application.Current;
        if (app is not null)
        {
            return app.RequestedTheme == ApplicationTheme.Dark ? "dark" : "light";
        }
        return "light";
    }

    private async void OnSystemThemeChanged(Windows.UI.ViewManagement.UISettings sender, object args)
    {
        var newTheme = GetSystemTheme();
        if (newTheme != _currentTheme)
        {
            _currentTheme = newTheme;
            try
            {
                await SetBodyThemeClassAsync(_currentTheme);
                ThemeChanged?.Invoke(_currentTheme);
            }
            catch
            {
                // JS interop may not be available yet
            }
        }
    }

    private async Task SetBodyThemeClassAsync(string theme)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval",
                $"document.body.className = document.body.className.replace(/\\btheme-\\w+\\b/g, '') + ' theme-{theme}'");
        }
        catch (InvalidOperationException)
        {
            // JS interop may not be available outside of a WebView context
        }
    }

    public void Dispose()
    {
        // UISettings event is weak-referenced, no explicit unsubscribe needed
    }
}
#endif
