using System.ComponentModel.DataAnnotations;

namespace SavedMessages.ApiService.Models;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MaxLength(100)] string DisplayName,
    [MaxLength(200)] string? DeviceName = null,
    string? DevicePlatform = null);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [MaxLength(200)] string? DeviceName = null,
    string? DevicePlatform = null);

public record RefreshRequest(
    [Required] string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);

public record OAuthCallbackRequest(
    [Required] string Code,
    [Required] string State);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(128)] string NewPassword);
