namespace SavedMessages.Domain.Entities;

public class ShareLink
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsOneTime { get; set; }
    public int ViewCount { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
