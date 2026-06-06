namespace Briefcase.Domain.Entities;

public class FileAttachment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string? PreviewBlobPath { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
