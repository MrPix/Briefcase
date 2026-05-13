namespace SavedMessages.Domain.Entities;

public class UserE2eeSettings
{
    public Guid UserId { get; set; }
    public bool IsEnabled { get; set; }
    public string KdfAlgorithm { get; set; } = string.Empty;
    public string KdfSalt { get; set; } = string.Empty;
    public string KdfParams { get; set; } = string.Empty;
    public string KeyVerifier { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
