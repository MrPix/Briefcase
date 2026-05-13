using System.ComponentModel.DataAnnotations;
using SavedMessages.Domain.Entities;

namespace SavedMessages.ApiService.Models;

public record DeviceResponse(
    Guid Id,
    string Name,
    Platform Platform,
    DateTime LastSeenAt,
    DateTime CreatedAt);

public record ClaimDeviceRequest(
    [Required] string Token,
    [Required, MaxLength(100)] string DeviceName,
    [Required] Platform Platform);

public record PairCodeResponse(string Token, DateTime ExpiresAt);
