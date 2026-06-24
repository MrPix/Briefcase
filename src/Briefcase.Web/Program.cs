using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Briefcase.Components.Services;
using Briefcase.Web;
using Briefcase.Web.Services;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["services:apiservice:https:0"]
    ?? builder.Configuration["ApiBaseAddress"]
    ?? builder.HostEnvironment.BaseAddress;

// ── Auth services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenStorageService, WebTokenStorageService>();
builder.Services.AddScoped<IDeviceInfoProvider, DefaultDeviceInfoProvider>();
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddHttpClient("AuthClient", client =>
    {
        client.BaseAddress = new Uri(apiBaseAddress);
    });
builder.Services.AddScoped<IAuthService>(sp =>
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var tokenStorage = sp.GetRequiredService<ITokenStorageService>();
        var deviceInfo = sp.GetRequiredService<IDeviceInfoProvider>();
        return new AuthService(factory.CreateClient("AuthClient"), tokenStorage, deviceInfo);
    });
builder.Services.AddHttpClient("ApiClient", client =>
    {
        client.BaseAddress = new Uri(apiBaseAddress);
    })
    .AddHttpMessageHandler<AuthDelegatingHandler>();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseAddress);
    })
    .AddHttpMessageHandler<AuthDelegatingHandler>();

// ── App services ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IAppVersionService, AppVersionService>();
builder.Services.AddScoped<IClipboardService, WebClipboardService>();
builder.Services.AddScoped<IMessageService, WebMessageService>();
builder.Services.AddScoped<IDeviceService, WebDeviceService>();
builder.Services.AddScoped<ITransferService, WebTransferService>();
builder.Services.AddScoped<ITrashService, WebTrashService>();
builder.Services.AddScoped<IE2eeService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var js = sp.GetRequiredService<IJSRuntime>();
    return new WebE2eeService(factory.CreateClient("ApiClient"), js);
});

await builder.Build().RunAsync();
