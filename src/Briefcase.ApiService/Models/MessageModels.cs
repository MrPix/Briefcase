using System.ComponentModel.DataAnnotations;
using Briefcase.Domain.Entities;

namespace Briefcase.ApiService.Models;

public record CreateMessageRequest(
    [Required] MessageKind Kind,
    [MaxLength(100_000)] string? Content,
    Guid? FileId = null,
    bool IsEncrypted = false,
    [MaxLength(24)] string? EncryptionIV = null);

public record MessageResponse(
    Guid Id,
    MessageKind Kind,
    string? Content,
    Guid? FileId,
    string? FileName,
    string? FilePreviewUrl,
    bool IsPinned,
    DateTime? PinnedAt,
    bool IsEncrypted,
    string? EncryptionIV,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UpdateMessageRequest(
    [MaxLength(100_000)] string? Content,
    bool IsEncrypted = false,
    [MaxLength(24)] string? EncryptionIV = null);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
