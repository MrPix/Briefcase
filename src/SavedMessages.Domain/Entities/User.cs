namespace SavedMessages.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Device> Devices { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<FileAttachment> FileAttachments { get; set; } = [];
}
