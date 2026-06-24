using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Hubs;
using Briefcase.ApiService.Models;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/e2ee")]
public class E2eeController(AppDbContext db, IHubContext<MessageHub> hub) : ControllerBase
{
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    // GET /api/e2ee/settings  →  KDF salt, params, key verifier, isEnabled
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var userId = GetUserId();
        var settings = await db.UserE2eeSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings is null)
            return Ok(new E2eeSettingsResponse(false, null, null, null, null));

        return Ok(new E2eeSettingsResponse(
            settings.IsEnabled,
            settings.KdfAlgorithm,
            settings.KdfSalt,
            settings.KdfParams,
            settings.KeyVerifier));
    }

    // POST /api/e2ee/enable  →  store KDF artefacts; mark IsEnabled = true
    [HttpPost("enable")]
    public async Task<IActionResult> Enable([FromBody] EnableE2eeRequest request)
    {
        var userId = GetUserId();
        var settings = await db.UserE2eeSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings is null)
        {
            settings = new UserE2eeSettings { UserId = userId };
            db.UserE2eeSettings.Add(settings);
        }

        settings.IsEnabled = true;
        settings.KdfAlgorithm = request.KdfAlgorithm;
        settings.KdfSalt = request.KdfSalt;
        settings.KdfParams = request.KdfParams;
        settings.KeyVerifier = request.KeyVerifier;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.E2eeSettingsChanged, new { isEnabled = true });

        return NoContent();
    }

    // POST /api/e2ee/disable  →  clear E2EE settings
    [HttpPost("disable")]
    public async Task<IActionResult> Disable()
    {
        var userId = GetUserId();
        var settings = await db.UserE2eeSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings is null)
            return NoContent();

        settings.IsEnabled = false;
        settings.KdfAlgorithm = string.Empty;
        settings.KdfSalt = string.Empty;
        settings.KdfParams = string.Empty;
        settings.KeyVerifier = string.Empty;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.E2eeSettingsChanged, new { isEnabled = false });

        return NoContent();
    }

    // PUT /api/e2ee/change-passphrase  →  replace KDF artefacts after client re-encryption
    [HttpPut("change-passphrase")]
    public async Task<IActionResult> ChangePassphrase([FromBody] ChangePassphraseRequest request)
    {
        var userId = GetUserId();
        var settings = await db.UserE2eeSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings is null || !settings.IsEnabled)
            return BadRequest(new { title = "E2EE is not enabled for this account." });

        settings.KdfAlgorithm = request.KdfAlgorithm;
        settings.KdfSalt = request.KdfSalt;
        settings.KdfParams = request.KdfParams;
        settings.KeyVerifier = request.KeyVerifier;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.E2eeSettingsChanged, new { isEnabled = true });

        return NoContent();
    }
}
