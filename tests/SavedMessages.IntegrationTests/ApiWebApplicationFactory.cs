using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SavedMessages.Domain.Interfaces;
using SavedMessages.Infrastructure.Persistence;

namespace SavedMessages.IntegrationTests;

/// <summary>
/// Spins up the ApiService in-process with a per-instance SQLite in-memory
/// database and a no-op file storage stub so tests run without infrastructure.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    // One named shared-cache connection keeps the in-memory DB alive for the
    // factory lifetime even though EF Core opens/closes its own connections.
    private readonly string _dbName = $"integration_{Guid.NewGuid():N}";
    private readonly SqliteConnection _keepAliveConnection;

    public ApiWebApplicationFactory()
    {
        _keepAliveConnection = new SqliteConnection(ConnectionString);
        _keepAliveConnection.Open();
    }

    private string ConnectionString => $"DataSource={_dbName};Mode=Memory;Cache=Shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide configuration that prevents Aspire from throwing about missing
        // connection strings during service registration.
        // Note: Jwt settings are intentionally NOT overridden so that Program.cs
        // and TokenService both read the same secret from appsettings.json.
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Placeholder — the Aspire registration is fully replaced in
                // ConfigureTestServices so this value is never actually used.
                ["ConnectionStrings:savedmessagesdb"] = "Host=localhost;Database=test_placeholder;Username=test;Password=test",
                ["ConnectionStrings:s3"]              = "http://localhost:9000",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // ── Replace PostgreSQL DbContext with SQLite in-memory ────────────
            // Remove AppDbContext itself and every generic EF Core service that
            // has AppDbContext as a type argument (DbContextOptions<AppDbContext>,
            // IDbContextOptionsConfiguration<AppDbContext>,
            // IDbContextFactory<AppDbContext>, etc.).
            var toRemove = services
                .Where(d => d.ServiceType == typeof(AppDbContext)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext))))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(ConnectionString));

            // ── Replace MinIO / S3 storage with a no-op stub ─────────────────
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService, NullFileStorageService>();
        });
    }

    /// <summary>
    /// Ensures the SQLite schema is created. Call once after constructing the factory.
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _keepAliveConnection.DisposeAsync();
    }
}

/// <summary>No-op implementation of IFileStorageService for use in tests.</summary>
file sealed class NullFileStorageService : IFileStorageService
{
    public Task UploadAsync(string blobPath, string contentType, Stream content,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<Uri> GetDownloadUrlAsync(string blobPath,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new Uri("https://test.invalid/stub"));

    public Task<Stream> DownloadAsync(string blobPath,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(string blobPath,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
