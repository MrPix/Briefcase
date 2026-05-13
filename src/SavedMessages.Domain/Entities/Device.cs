namespace SavedMessages.Domain.Entities;

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
    public string Name { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string? PushToken { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
