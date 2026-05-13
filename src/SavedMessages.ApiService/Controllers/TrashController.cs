using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SavedMessages.ApiService.Hubs;
using SavedMessages.ApiService.Models;
using SavedMessages.Domain.Entities;
using SavedMessages.Infrastructure.Persistence;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/trash")]
public class TrashController(AppDbContext db, IHubContext<MessageHub> hub) : ControllerBase
{
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static MessageResponse ToResponse(Message m) => new(
        m.Id, m.Kind, m.Content, m.FileId,
        m.IsPinned, m.IsEncrypted,
        m.CreatedAt, m.UpdatedAt);

    // GET /api/trash  →  list trashed messages (paged, IsDeleted = true)
    [HttpGet]
    public async Task<IActionResult> GetTrashed([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = db.Messages
            .Where(m => m.UserId == userId && m.IsDeleted)
            .OrderByDescending(m => m.DeletedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => ToResponse(m))
            .ToListAsync();

        return Ok(new PagedResponse<MessageResponse>(items, page, pageSize, totalCount));
    }

    // POST /api/trash/{id}/restore  →  restore message (IsDeleted = false, clears DeletedAt)
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var userId = GetUserId();
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.IsDeleted);

        if (message is null)
            return NotFound();

        message.IsDeleted = false;
        message.DeletedAt = null;
        message.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var response = ToResponse(message);
        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.MessageRestored, response);

        return Ok(response);
    }
}
