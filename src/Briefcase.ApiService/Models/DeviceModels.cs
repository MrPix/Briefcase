using System.ComponentModel.DataAnnotations;
using Briefcase.Domain.Entities;

namespace Briefcase.ApiService.Models;

public record DeviceResponse(
    Guid Id,
    string Name,
    Platform Platform,
    DateTime LastSeenAt,
    DateTime CreatedAt,
    bool IsCurrent = false);

public record ClaimDeviceRequest(
    [Required] string Token,
    [Required, MaxLength(100)] string DeviceName,
    [Required] Platform Platform,
    [MaxLength(64)] string? InstallationId = null);

public record PairCodeResponse(string Token, DateTime ExpiresAt);

public record CreateLoginCodeRequest(
    [Required, MaxLength(100)] string DeviceName,
    [MaxLength(20)] string? Platform = null,
    [MaxLength(64)] string? InstallationId = null);

public record LoginCodeResponse(string Code, DateTime ExpiresAt);

public record ApproveLoginCodeRequest(
    [Required, MaxLength(16)] string Code);

public record ApproveLoginCodeResponse(string DeviceName, Platform Platform);

public record LoginCodePollResponse(
    string Status,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? AccessTokenExpiresAt = null);

public record SignOutOthersResponse(int RemovedCount);
