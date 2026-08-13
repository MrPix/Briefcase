namespace Briefcase.Domain.Entities;

/// <summary>
/// A short-lived, single-use code used to log a new (not-yet-authenticated) device
/// into an existing account. The new device generates the code and polls for approval;
/// an already-authenticated device approves it by entering the code.
/// </summary>
public class DeviceLoginCode
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public Platform Platform { get; set; }

    /// <summary>Installation id of the device that requested the code.</summary>
    public string? InstallationId { get; set; }

    /// <summary>Set when an authenticated device approves the code.</summary>
    public Guid? UserId { get; set; }
    public bool IsApproved { get; set; }

    /// <summary>Set once the pending device has redeemed the approval for tokens.</summary>
    public bool IsConsumed { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public User? User { get; set; }
}
