namespace SavedMessages.Domain.Entities;

public enum MessageKind
{
    Text,
    Url,
    File
}

public class Message
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public MessageKind Kind { get; set; }
    public string? Content { get; set; }
    public Guid? FileId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionIV { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public FileAttachment? FileAttachment { get; set; }
    public ICollection<ShareLink> ShareLinks { get; set; } = [];
}
