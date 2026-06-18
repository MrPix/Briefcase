using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Hubs;
using Briefcase.ApiService.Services;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Controllers;

/// <summary>
/// Anonymous quick-transfer sessions — no account required on the target device.
/// </summary>
[ApiController]
[Route("api/transfer")]
public class TransferController(
    TransferSessionService sessions,
    IHubContext<MessageHub> hub,
    AppDbContext db) : ControllerBase
{
    // POST /api/transfer/session  →  create an anonymous transfer session, returns 8-char code
    [HttpPost("session")]
    public IActionResult CreateSession()
    {
        var code = sessions.CreateSession();
        return Ok(new { code });
    }

    // POST /api/transfer/push  →  push content into a transfer session (legacy)
    [HttpPost("push")]
    public async Task<IActionResult> PushContent([FromBody] TransferPushRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest();

        if (!sessions.TryPush(request.SessionId, request.Content))
            return NotFound();

        // Push to the transfer-session SignalR group (the target device joined this group)
        await hub.Clients.Group($"transfer:{request.SessionId}")
            .SendAsync(MessageHub.TransferReceived, new { request.SessionId, request.Content });

        return Ok();
    }

    /// <summary>
    /// POST /api/transfer/send  →  create a one-time share link for a message and push its
    /// URL to the receiving device (identified by <paramref name="request"/>.Code) via SignalR.
    /// Requires authentication — the sender must own the message.
    /// </summary>
    [HttpPost("send")]
    [Authorize]
    public async Task<IActionResult> SendTo([FromBody] TransferSendRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        var (found, _, expired) = sessions.TryGet(request.Code);
        if (!found || expired)
            return NotFound("Transfer session not found or expired.");

        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.UserId == userId && !m.IsDeleted);

        if (message is null)
            return NotFound("Message not found.");

        const string slugChars = "abcdefghjkmnpqrstuvwxyz23456789";
        var slug = RandomNumberGenerator.GetString(slugChars, 16);

        var shareLink = new ShareLink
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            UserId = userId,
            Slug = slug,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsOneTime = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.ShareLinks.Add(shareLink);
        await db.SaveChangesAsync();

        await hub.Clients.Group($"transfer:{request.Code}")
            .SendAsync(MessageHub.TransferReceived, new { url = $"/share/{slug}" });

        return Ok(new { slug });
    }
}

public record TransferPushRequest(string SessionId, string Content);
public record TransferSendRequest(string Code, Guid MessageId);

