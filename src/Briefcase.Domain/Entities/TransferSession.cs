namespace Briefcase.Domain.Entities;

public class TransferSession
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public string? Content { get; set; }
}
