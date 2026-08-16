using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Models;
using Briefcase.ApiService.Services;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UserSettingsController(
    AppDbContext db,
    NavigationApplicationCatalog navigationCatalog,
    NavigationSettingsService navigationSettings,
    IGoogleMapsResolver mapsResolver) : ControllerBase
{
    private static readonly string[] SupportedLanguages = ["en", "uk"];

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    // GET /api/users/settings  →  the user's preferred language, or null to follow client detection
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var userId = GetUserId();
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        var preferences = await navigationSettings.GetAsync(userId);
        return Ok(ToResponse(settings?.Language, preferences));
    }

    // PUT /api/users/settings  →  upsert the user's preferred language
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
    {
        if (request.Language is not null && !SupportedLanguages.Contains(request.Language))
            return BadRequest(new { title = $"Unsupported language: {request.Language}." });

        var userId = GetUserId();
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings is null)
        {
            settings = new UserSettings { UserId = userId };
            db.UserSettings.Add(settings);
        }

        settings.Language = request.Language;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("settings/navigation")]
    public async Task<IActionResult> UpdateNavigationSettings([FromBody] UpdateNavigationSettingsRequest request)
    {
        var unknownIds = request.ApplicationIds
            .Where(id => !navigationCatalog.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknownIds.Count > 0)
            return BadRequest(new { title = $"Unsupported navigation application: {string.Join(", ", unknownIds)}." });

        var userId = GetUserId();
        var settings = await db.UserSettings.FirstOrDefaultAsync(item => item.UserId == userId);
        var wasEnabled = settings?.GoogleMapsNavigationEnabled ?? true;
        if (settings is null)
        {
            settings = new UserSettings { UserId = userId };
            db.UserSettings.Add(settings);
        }

        var normalizedIds = navigationCatalog.NormalizeIds(request.ApplicationIds);
        settings.GoogleMapsNavigationEnabled = request.Enabled;
        settings.NavigationApplicationIds = System.Text.Json.JsonSerializer.Serialize(normalizedIds);
        settings.UpdatedAt = DateTime.UtcNow;

        if (!request.Enabled)
        {
            await db.Messages
                .Where(message => message.UserId == userId && message.NavigationStatus != NavigationProcessingStatus.None)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(message => message.NavigationStatus, NavigationProcessingStatus.None)
                    .SetProperty(message => message.NavigationLatitude, (double?)null)
                    .SetProperty(message => message.NavigationLongitude, (double?)null)
                    .SetProperty(message => message.NavigationProcessingStartedAt, (DateTime?)null)
                    .SetProperty(message => message.NavigationProcessedAt, (DateTime?)null)
                    .SetProperty(message => message.NavigationProcessingAttempts, 0)
                    .SetProperty(message => message.NavigationProcessingError, (string?)null));
        }
        else if (!wasEnabled)
        {
            var messages = await db.Messages
                .Where(message => message.UserId == userId
                    && message.Kind == MessageKind.Url
                    && !message.IsEncrypted
                    && !message.IsDeleted
                    && !message.IsPermanentlyDeleted)
                .ToListAsync();
            foreach (var message in messages.Where(message => mapsResolver.IsSupportedUrl(message.Content)))
            {
                message.NavigationStatus = NavigationProcessingStatus.Pending;
                message.NavigationLatitude = null;
                message.NavigationLongitude = null;
                message.NavigationProcessingStartedAt = null;
                message.NavigationProcessedAt = null;
                message.NavigationProcessingAttempts = 0;
                message.NavigationProcessingError = null;
            }
        }

        await db.SaveChangesAsync();
        return Ok(ToResponse(settings.Language, new NavigationPreferences(request.Enabled, normalizedIds)));
    }

    private UserSettingsResponse ToResponse(string? language, NavigationPreferences preferences) => new(
        language,
        preferences.Enabled,
        preferences.ApplicationIds,
        navigationCatalog.GetApplications()
            .Select(application => new NavigationApplicationResponse(application.Id, application.DisplayName))
            .ToList());
}
