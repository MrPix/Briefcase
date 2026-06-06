namespace Briefcase.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Device> Devices { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<FileAttachment> FileAttachments { get; set; } = [];
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
    public ICollection<ShareLink> ShareLinks { get; set; } = [];
    public UserE2eeSettings? E2eeSettings { get; set; }
}
