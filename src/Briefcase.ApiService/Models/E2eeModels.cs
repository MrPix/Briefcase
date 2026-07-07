using System.ComponentModel.DataAnnotations;

namespace Briefcase.ApiService.Models;

public record E2eeSettingsResponse(
    bool IsEnabled,
    string? KdfAlgorithm,
    string? KdfSalt,
    string? KdfParams,
    string? KeyVerifier);

public record EnableE2eeRequest(
    [Required, MaxLength(50)] string KdfAlgorithm,
    [Required, MaxLength(256)] string KdfSalt,
    [Required] string KdfParams,
    [Required, MaxLength(512)] string KeyVerifier);

public record ChangePassphraseRequest(
    [Required, MaxLength(50)] string KdfAlgorithm,
    [Required, MaxLength(256)] string KdfSalt,
    [Required] string KdfParams,
    [Required, MaxLength(512)] string KeyVerifier);
