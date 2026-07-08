using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    public async Task<IActionResult> CreateShareLink(Guid id, [FromBody] CreateShareLinkRequest request)
    {
        var userId = GetUserId();
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && !m.IsDeleted);

        if (message is null)
            return NotFound();

        const string slugChars = "abcdefghjkmnpqrstuvwxyz23456789";
        var slug = RandomNumberGenerator.GetString(slugChars, 16);

        DateTime? expiresAt = request.ExpiresInMinutes is int minutes and > 0
            ? DateTime.UtcNow.AddMinutes(minutes)
            : null;

        var shareLink = new ShareLink
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            UserId = userId,
            Slug = slug,
            ExpiresAt = expiresAt,
            IsOneTime = request.OneTime,
            CreatedAt = DateTime.UtcNow,
        };

        db.ShareLinks.Add(shareLink);
        await db.SaveChangesAsync();

        var response = new ShareLinkResponse(slug, $"/share/{slug}", expiresAt, request.OneTime);

        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.ShareLinkCreated, new { messageId = message.Id, response.Slug, response.Url });

        return Ok(response);
    }

    // DELETE /api/messages/{id}/share  →  revoke all active share links for a message
    [HttpDelete("{id:guid}/share")]
    public async Task<IActionResult> RevokeShareLink(Guid id)
    {
        var userId = GetUserId();
        var links = await db.ShareLinks
            .Where(s => s.MessageId == id && s.UserId == userId && s.RevokedAt == null)
            .ToListAsync();

        if (links.Count == 0)
            return NotFound();

        var now = DateTime.UtcNow;
        foreach (var link in links)
            link.RevokedAt = now;

        await db.SaveChangesAsync();

        await hub.Clients.Group(userId.ToString())
            .SendAsync(MessageHub.ShareLinkRevoked, new { messageId = id });

        return NoContent();
    }
}
