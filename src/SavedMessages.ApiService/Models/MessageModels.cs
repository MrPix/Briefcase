using System.ComponentModel.DataAnnotations;
using SavedMessages.Domain.Entities;

namespace SavedMessages.ApiService.Models;

public record CreateMessageRequest(
    [Required] MessageKind Kind,
    [MaxLength(50_000)] string? Content);

public record MessageResponse(
    Guid Id,
    MessageKind Kind,
    string? Content,
    Guid? FileId,
    bool IsPinned,
    bool IsEncrypted,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
