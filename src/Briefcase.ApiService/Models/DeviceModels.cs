using System.ComponentModel.DataAnnotations;
using Briefcase.Domain.Entities;

namespace Briefcase.ApiService.Models;

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
