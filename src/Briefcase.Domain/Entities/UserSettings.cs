namespace Briefcase.Domain.Entities;

public class UserSettings
{
    public const string DefaultNavigationApplicationIds = "[\"google-maps\",\"waze\",\"locus-map\",\"maps-me\"]";

    public Guid UserId { get; set; }

    /// <summary>ISO 639-1 code (e.g. "en", "uk"), or null to follow the client's detected language.</summary>
    public string? Language { get; set; }

    public bool GoogleMapsNavigationEnabled { get; set; } = true;

    public string NavigationApplicationIds { get; set; } = DefaultNavigationApplicationIds;

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
