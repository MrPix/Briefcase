using Microsoft.AspNetCore.Mvc;

namespace SavedMessages.ApiService.Controllers;

/// <summary>
/// Anonymous quick-transfer sessions — no account required on the target device.
/// </summary>
[ApiController]
[Route("api/transfer")]
public class TransferController : ControllerBase
{
    // POST /api/transfer/session  →  create an anonymous transfer session, returns session ID
    [HttpPost("session")]
    public IActionResult CreateSession() => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/transfer/push  →  push content into a transfer session
    [HttpPost("push")]
    public IActionResult PushContent() => StatusCode(StatusCodes.Status501NotImplemented);
}
