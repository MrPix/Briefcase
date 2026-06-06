using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Maui.Storage;
using Briefcase.Maui.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Briefcase.Maui.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            var window = Microsoft.Maui.MauiWinUIApplication.Current?.Application?.Windows
                .FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

            if (window?.Content is Microsoft.UI.Xaml.UIElement rootElement)
            {
                var keyboardService = Services.GetService<WindowsKeyboardShortcutService>();
                keyboardService?.RegisterAccelerators(rootElement);
            }

            if (window is not null)
            {
                var trayService = Services.GetService<WindowsTrayService>();
                trayService?.Initialize(window);

                RestoreWindowBounds(window);
                window.SizeChanged += OnWindowSizeChanged;
                window.AppWindow.Changed += OnAppWindowChanged;
            }
        }

        private const string PrefWindowWidth = "WindowWidth";
        private const string PrefWindowHeight = "WindowHeight";
        private const string PrefWindowX = "WindowX";
        private const string PrefWindowY = "WindowY";

        private static void RestoreWindowBounds(Microsoft.UI.Xaml.Window window)
        {
            var appWindow = window.AppWindow;
            if (appWindow is null) return;

            var width = Preferences.Get(PrefWindowWidth, 0.0);
            var height = Preferences.Get(PrefWindowHeight, 0.0);
            if (width > 0 && height > 0)
                appWindow.Resize(new Windows.Graphics.SizeInt32((int)width, (int)height));

            var x = Preferences.Get(PrefWindowX, int.MinValue);
            var y = Preferences.Get(PrefWindowY, int.MinValue);
            if (x != int.MinValue && y != int.MinValue)
                appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }

        private static void OnWindowSizeChanged(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs e)
        {
            Preferences.Set(PrefWindowWidth, (double)e.Size.Width);
            Preferences.Set(PrefWindowHeight, (double)e.Size.Height);
        }

        private static void OnAppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange)
            {
                Preferences.Set(PrefWindowX, sender.Position.X);
                Preferences.Set(PrefWindowY, sender.Position.Y);
            }
        }

        private new IServiceProvider Services =>
            IPlatformApplication.Current!.Services;
    }

}
