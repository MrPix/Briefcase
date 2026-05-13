using Microsoft.UI.Xaml;
using SavedMessages.Maui.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SavedMessages.Maui.WinUI
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
            }
        }

        private new IServiceProvider Services =>
            IPlatformApplication.Current!.Services;
    }

}
