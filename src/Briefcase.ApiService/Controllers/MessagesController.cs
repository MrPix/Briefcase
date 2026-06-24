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
[Route("api/messages")]
public class MessagesController(AppDbContext db, IHubContext<MessageHub> hub) : ControllerBase
{
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static string? BuildPreviewUrl(FileAttachment? attachment) =>
        attachment?.PreviewBlobPath is not null ? $"/api/files/{attachment.Id}/preview" : null;

    private static MessageResponse ToResponse(Message m) => new(
        m.Id, m.Kind, m.Content, m.FileId,
        m.FileName,
        BuildPreviewUrl(m.FileAttachment),
        m.IsPinned, m.PinnedAt, m.IsEncrypted, m.EncryptionIV,
        m.CreatedAt, m.UpdatedAt);

    // GET /api/messages  →  list active messages (paged, newest first)
    [HttpGet]
    public async Task<IActionResult> GetMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = db.Messages
            .Where(m => m.UserId == userId && !m.IsDeleted)
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.PinnedAt)
            .ThenByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageResponse(
                m.Id,
                m.Kind,
                m.Content,
                m.FileId,
                m.FileAttachment != null ? m.FileAttachment.OriginalName : null,
                m.FileAttachment != null && m.FileAttachment.PreviewBlobPath != null ? $"/api/files/{m.FileAttachment.Id}/preview" : null,
                m.IsPinned,
                m.PinnedAt,
                m.IsEncrypted,
                m.EncryptionIV,
                m.CreatedAt,
                m.UpdatedAt))
            .ToListAsync();

        return Ok(new PagedResponse<MessageResponse>(items, page, pageSize, totalCount));
    }

    // POST /api/messages  →  create text or URL message
    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = request.Kind,
            Content = request.Content,
            FileId = request.FileId,
            IsPinned = false,
            IsDeleted = false,
            IsEncrypted = request.IsEncrypted,
            EncryptionIV = request.IsEncrypted ? request.EncryptionIV : null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var response = ToResponse(message);
        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.MessageCreated, response);

        return CreatedAtAction(nameof(GetMessages), null, response);
    }

    // DELETE /api/messages/{id}  →  move to Trash (soft-delete)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        var userId = GetUserId();
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && !m.IsDeleted);

        if (message is null)
            return NotFound();

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.MessageTrashed, new { id });

        return NoContent();
    }

    // PATCH /api/messages/{id}/pin  →  toggle pin
    [HttpPatch("{id:guid}/pin")]
    public async Task<IActionResult> TogglePin(Guid id)
    {
        var userId = GetUserId();
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && !m.IsDeleted);

        if (message is null)
            return NotFound();

        message.IsPinned = !message.IsPinned;
        message.PinnedAt = message.IsPinned ? DateTime.UtcNow : null;
        message.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ToResponse(message));
    }

    // PUT /api/messages/{id}  →  update message content
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMessage(Guid id, [FromBody] UpdateMessageRequest request)
    {
        var userId = GetUserId();
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && !m.IsDeleted);

        if (message is null)
            return NotFound();

        message.Content = request.Content;
        message.IsEncrypted = request.IsEncrypted;
        message.EncryptionIV = request.IsEncrypted ? request.EncryptionIV : null;
        message.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var response = ToResponse(message);
        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.MessageUpdated, response);

        return Ok(response);
    }

    // POST /api/messages/{id}/share  →  generate share link
    [HttpPost("{id:guid}/share")]
    public IActionResult CreateShareLink(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);

    // DELETE /api/messages/{id}/share  →  revoke share link
    [HttpDelete("{id:guid}/share")]
    public IActionResult RevokeShareLink(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);
}
