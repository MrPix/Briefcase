using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Hubs;
using Briefcase.ApiService.Models;
using Briefcase.ApiService.Services;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/trash")]
public class TrashController(
    AppDbContext db,
    IHubContext<MessageHub> hub,
    IGoogleMapsResolver mapsResolver,
    NavigationSettingsService navigationSettings,
    MessageResponseMapper responseMapper) : ControllerBase
{
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    // GET /api/trash  →  list trashed messages (paged, IsDeleted = true)
    [HttpGet]
    public async Task<IActionResult> GetTrashed([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = db.Messages
            .Where(m => m.UserId == userId && m.IsDeleted && !m.IsPermanentlyDeleted)
            .OrderByDescending(m => m.DeletedAt);

        var totalCount = await query.CountAsync();
        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.FileAttachment)
            .ToListAsync();
        var preferences = await navigationSettings.GetAsync(userId);
        var items = messages.Select(message => responseMapper.Map(message, preferences)).ToList();

        return Ok(new PagedResponse<MessageResponse>(items, page, pageSize, totalCount));
    }

    // POST /api/trash/{id}/restore  →  restore message (IsDeleted = false, clears DeletedAt)
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var userId = GetUserId();
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.IsDeleted && !m.IsPermanentlyDeleted);

        if (message is null)
            return NotFound();

        message.IsDeleted = false;
        message.DeletedAt = null;
        message.UpdatedAt = DateTime.UtcNow;
        var preferences = await navigationSettings.GetAsync(userId);
        if (preferences.Enabled
            && message.NavigationStatus == NavigationProcessingStatus.None
            && message.Kind == MessageKind.Url
            && !message.IsEncrypted
            && mapsResolver.IsSupportedUrl(message.Content))
        {
            message.NavigationStatus = NavigationProcessingStatus.Pending;
        }

        await db.SaveChangesAsync();

        var response = responseMapper.Map(message, preferences);
        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.MessageRestored, response);

        return Ok(response);
    }

    // DELETE /api/trash/{id}  →  permanently hide a trashed message without deleting its record
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteForever(Guid id)
    {
        var userId = GetUserId();
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.IsDeleted && !m.IsPermanentlyDeleted);

        if (message is null)
            return NotFound();

        var now = DateTime.UtcNow;
        message.IsPermanentlyDeleted = true;
        message.PermanentlyDeletedAt = now;
        message.UpdatedAt = now;
        await db.SaveChangesAsync();

        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.MessageDeleted, new { id });

        return NoContent();
    }

    // DELETE /api/trash  →  permanently hide all trashed messages without deleting their records
    [HttpDelete]
    public async Task<IActionResult> EmptyTrash()
    {
        var userId = GetUserId();
        var messages = await db.Messages
            .Where(m => m.UserId == userId && m.IsDeleted && !m.IsPermanentlyDeleted)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var message in messages)
        {
            message.IsPermanentlyDeleted = true;
            message.PermanentlyDeletedAt = now;
            message.UpdatedAt = now;
        }

        await db.SaveChangesAsync();

        foreach (var message in messages)
        {
            await hub.Clients.Group(userId.ToString())
                .SendAsync(MessageHub.MessageDeleted, new { id = message.Id });
        }

        return NoContent();
    }
}
