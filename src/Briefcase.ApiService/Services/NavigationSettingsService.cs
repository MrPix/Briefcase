using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Services;

public record NavigationPreferences(bool Enabled, IReadOnlyList<string> ApplicationIds);

public class NavigationSettingsService(AppDbContext db, NavigationApplicationCatalog catalog)
{
    public async Task<NavigationPreferences> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await db.UserSettings.AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (settings is null)
            return new NavigationPreferences(true, catalog.GetApplications().Select(app => app.Id).ToList());

        return new NavigationPreferences(
            settings.GoogleMapsNavigationEnabled,
            catalog.NormalizeIds(ParseIds(settings.NavigationApplicationIds)));
    }

    public static IReadOnlyList<string> ParseIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}