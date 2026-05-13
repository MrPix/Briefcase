using Microsoft.Extensions.Logging;
using SavedMessages.Components.Services;
using SavedMessages.Maui.Services;

namespace SavedMessages.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // ── App services ──────────────────────────────────────────────────
            builder.Services.AddSingleton<IClipboardService, MauiClipboardService>();
            builder.Services.AddTransient<IMessageService, MauiMessageService>();

            // ── Auth services ─────────────────────────────────────────────────
            builder.Services.AddSingleton<ITokenStorageService, MauiTokenStorageService>();
            builder.Services.AddTransient<AuthDelegatingHandler>();
            builder.Services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7574/");
            });
            builder.Services.AddSingleton<IAuthService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var tokenStorage = sp.GetRequiredService<ITokenStorageService>();
                return new AuthService(factory.CreateClient("AuthClient"), tokenStorage);
            });
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7574/");
            })
            .AddHttpMessageHandler<AuthDelegatingHandler>();

#if WINDOWS
            // ── Windows-specific services ────────────────────────────────────
            builder.Services.AddSingleton<IThemeService, WindowsThemeService>();
            builder.Services.AddSingleton<WindowsFileDropService>();
            builder.Services.AddSingleton<IFileDropService>(sp => sp.GetRequiredService<WindowsFileDropService>());
            builder.Services.AddSingleton<WindowsKeyboardShortcutService>();
            builder.Services.AddSingleton<IKeyboardShortcutService>(sp => sp.GetRequiredService<WindowsKeyboardShortcutService>());
            builder.Services.AddSingleton<IJumpListService, WindowsJumpListService>();
            builder.Services.AddSingleton<WindowsTrayService>();
#endif

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
