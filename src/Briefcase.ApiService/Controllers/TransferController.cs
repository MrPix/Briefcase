using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Briefcase.ApiService.Hubs;
using Briefcase.ApiService.Services;

namespace Briefcase.ApiService.Controllers;

/// <summary>
/// Anonymous quick-transfer sessions — no account required on the target device.
/// </summary>
[ApiController]
[Route("api/transfer")]
public class TransferController(TransferSessionService sessions, IHubContext<MessageHub> hub) : ControllerBase
{
    // POST /api/transfer/session  →  create an anonymous transfer session, returns session ID
    [HttpPost("session")]
    public IActionResult CreateSession()
    {
        var sessionId = sessions.CreateSession();
        return Ok(new { sessionId });
    }

    // POST /api/transfer/push  →  push content into a transfer session
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
}

public record TransferPushRequest(string SessionId, string Content);
