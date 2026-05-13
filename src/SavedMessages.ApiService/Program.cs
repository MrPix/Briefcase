using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SavedMessages.ApiService.Hubs;
using SavedMessages.ApiService.Services;
using Scalar.AspNetCore;
using Amazon.S3;
using SavedMessages.Domain.Interfaces;
using SavedMessages.Infrastructure.Persistence;
using SavedMessages.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// ── EF Core (PostgreSQL via Aspire) ──────────────────────────────────────────
builder.AddNpgsqlDbContext<AppDbContext>("savedmessagesdb");

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the access token from POST /api/auth/login"
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
        });

        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<QrCodeService>();
builder.Services.AddSingleton<TransferSessionService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<OAuthService>();
builder.Services.AddHttpClient();

// ── File Storage (MinIO / S3-compatible) ─────────────────────────────────────
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config.GetConnectionString("s3") ?? "http://localhost:9000";
    var useHttps = endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    var s3Config = new AmazonS3Config
    {
        ServiceURL = endpoint,
        ForcePathStyle = true,
        UseHttp = !useHttps,
    };
    return new AmazonS3Client("minioadmin", "minioadmin", s3Config);
});
builder.Services.AddSingleton<IFileStorageService>(sp =>
    new MinioStorageService(sp.GetRequiredService<IAmazonS3>()));

// ── JWT Authentication (§3.2 / §6) ───────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero, // access tokens are short-lived (15 min)
        };

        // Allow the JWT to arrive via the SignalR query-string parameter
        // so browser WebSocket connections (which cannot set headers) still authenticate.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Auto-apply pending EF Core migrations in development.
    // Retry briefly in case PostgreSQL is still starting up.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var retries = 5;
    for (var i = 0; ; i++)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception) when (i < retries)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MessageHub>("/hubs/messages");

app.MapDefaultEndpoints();

await app.RunAsync();

