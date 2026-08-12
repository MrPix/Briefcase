using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Models;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UserSettingsController(AppDbContext db) : ControllerBase
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
        return Ok(new UserSettingsResponse(settings?.Language));
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
}
