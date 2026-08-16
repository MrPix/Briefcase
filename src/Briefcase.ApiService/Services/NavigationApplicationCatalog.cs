using System.Globalization;

namespace Briefcase.ApiService.Services;

public record NavigationApplication(string Id, string DisplayName);

public record NavigationTarget(string ApplicationId, string DisplayName, string Uri);

public class NavigationApplicationCatalog
{
    private static readonly NavigationApplication[] Applications =
    [
        new("google-maps", "Google Maps"),
        new("waze", "Waze"),
        new("locus-map", "Locus Map"),
        new("maps-me", "MAPS.ME"),
    ];

    public IReadOnlyList<NavigationApplication> GetApplications() => Applications;

    public IReadOnlyList<string> NormalizeIds(IEnumerable<string>? ids)
    {
        var requested = ids?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        return Applications.Where(app => requested.Contains(app.Id)).Select(app => app.Id).ToList();
    }

    public bool Contains(string id) => Applications.Any(app => app.Id == id);

    public IReadOnlyList<NavigationTarget> BuildTargets(double latitude, double longitude, IEnumerable<string> applicationIds)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return [];

        var lat = latitude.ToString("0.######", CultureInfo.InvariantCulture);
        var lon = longitude.ToString("0.######", CultureInfo.InvariantCulture);
        var selected = applicationIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Applications
            .Where(app => selected.Contains(app.Id))
            .Select(app => new NavigationTarget(app.Id, app.DisplayName, BuildUri(app.Id, lat, lon)))
            .ToList();
    }

    private static string BuildUri(string id, string latitude, string longitude) => id switch
    {
        "google-maps" => $"https://www.google.com/maps/dir/?api=1&destination={latitude}%2C{longitude}&dir_action=navigate",
        "waze" => $"https://waze.com/ul?ll={latitude}%2C{longitude}&navigate=yes",
        "locus-map" => $"intent:#Intent;action=locus.api.android.ACTION_NAVIGATION_START;package=menion.android.locus;d.INTENT_EXTRA_LATITUDE={latitude};d.INTENT_EXTRA_LONGITUDE={longitude};end",
        "maps-me" => $"mapsme://map?ll={latitude}%2C{longitude}",
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };
}