using SavedMessages.Components.Services;
using SavedMessages.Web;
using SavedMessages.Web.Components;
using SavedMessages.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// ── Auth services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenStorageService, WebTokenStorageService>();
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    });
builder.Services.AddHttpClient("ApiClient", client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    })
    .AddHttpMessageHandler<AuthDelegatingHandler>();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    })
    .AddHttpMessageHandler<AuthDelegatingHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
