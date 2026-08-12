using System.ComponentModel.DataAnnotations;

namespace Briefcase.ApiService.Models;

public record UserSettingsResponse(string? Language);

public record UpdateUserSettingsRequest(
    [MaxLength(10)] string? Language);
