using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Services;

/// <summary>
/// Decides whether an access token bound to a device is still usable.
/// Results are cached in-process; <see cref="Invalidate"/> makes removal take effect immediately.
/// Note: with more than one API instance this cache is per-instance, so a distributed
/// backplane would be required for cross-instance immediacy.
/// </summary>
public class DeviceSessionValidator(IServiceScopeFactory scopeFactory, IMemoryCache cache)
{
    private static readonly TimeSpan ActiveTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RevokedTtl = TimeSpan.FromMinutes(15);

    private static string CacheKey(Guid deviceId) => $"device-active:{deviceId}";

    public async Task<bool> IsActiveAsync(Guid deviceId)
    {
        if (cache.TryGetValue<bool>(CacheKey(deviceId), out var cached))
            return cached;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await db.Devices.AnyAsync(d => d.Id == deviceId);

        cache.Set(CacheKey(deviceId), exists, exists ? ActiveTtl : RevokedTtl);
        return exists;
    }

    public void Invalidate(Guid deviceId) =>
        cache.Set(CacheKey(deviceId), false, RevokedTtl);
}
