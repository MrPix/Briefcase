using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SavedMessages.Components.Services;
using SavedMessages.Web;
using SavedMessages.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? builder.HostEnvironment.BaseAddress;

// ── Auth services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenStorageService, WebTokenStorageService>();
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
    {
        client.BaseAddress = new Uri(apiBaseAddress);
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

await builder.Build().RunAsync();
