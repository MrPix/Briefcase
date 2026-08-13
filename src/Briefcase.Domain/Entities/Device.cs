namespace Briefcase.Domain.Entities;

public enum Platform
{
    Windows,
    Android,
    iOS,
    macOS,
    Web
}

public class Device
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Opaque id generated and persisted by the client install; identifies this device across logins.</summary>
    public string? InstallationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string? PushToken { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Set by the API when serialising for the device that is making the request.</summary>
    public bool IsCurrent { get; set; }

    public User User { get; set; } = null!;
}
