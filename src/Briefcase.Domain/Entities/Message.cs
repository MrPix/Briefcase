using System.ComponentModel.DataAnnotations.Schema;

namespace Briefcase.Domain.Entities;

public enum MessageKind
{
    Text,
    Url,
    File
}

public enum NavigationProcessingStatus
{
    None,
    Pending,
    Processing,
    Completed,
    Failed
}

public class Message
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public MessageKind Kind { get; set; }
    public string? Content { get; set; }
    public Guid? FileId { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? PinnedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsPermanentlyDeleted { get; set; }
    public DateTime? PermanentlyDeletedAt { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionIV { get; set; }
    public NavigationProcessingStatus NavigationStatus { get; set; }
    public double? NavigationLatitude { get; set; }
    public double? NavigationLongitude { get; set; }
    public DateTime? NavigationProcessingStartedAt { get; set; }
    public DateTime? NavigationProcessedAt { get; set; }
    public int NavigationProcessingAttempts { get; set; }
    public string? NavigationProcessingError { get; set; }
    [NotMapped]
    public string? FileName { get; set; }
    [NotMapped]
    public string? FilePreviewUrl { get; set; }
    [NotMapped]
    public bool Downloaded { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public FileAttachment? FileAttachment { get; set; }
    public ICollection<ShareLink> ShareLinks { get; set; } = [];
}
