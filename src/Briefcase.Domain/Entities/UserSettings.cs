namespace Briefcase.Domain.Entities;

public class UserSettings
{
    public Guid UserId { get; set; }

    /// <summary>ISO 639-1 code (e.g. "en", "uk"), or null to follow the client's detected language.</summary>
    public string? Language { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
