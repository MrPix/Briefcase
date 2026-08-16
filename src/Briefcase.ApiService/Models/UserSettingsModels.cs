using System.ComponentModel.DataAnnotations;

namespace Briefcase.ApiService.Models;

public record UserSettingsResponse(
    string? Language,
    bool GoogleMapsNavigationEnabled,
    IReadOnlyList<string> NavigationApplicationIds,
    IReadOnlyList<NavigationApplicationResponse> NavigationApplications);

public record NavigationApplicationResponse(string Id, string DisplayName);

public record UpdateUserSettingsRequest(
    [MaxLength(10)] string? Language);

public record UpdateNavigationSettingsRequest(
    bool Enabled,
    [Required] IReadOnlyList<string> ApplicationIds);
