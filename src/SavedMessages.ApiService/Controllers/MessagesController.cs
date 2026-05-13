using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    // GET /api/messages  →  list active messages (paged, newest first)
    [HttpGet]
    public IActionResult GetMessages() => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/messages  →  create text or URL message
    [HttpPost]
    public IActionResult CreateMessage() => StatusCode(StatusCodes.Status501NotImplemented);

    // DELETE /api/messages/{id}  →  move to Trash (soft-delete)
    [HttpDelete("{id:guid}")]
    public IActionResult DeleteMessage(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);

    // PATCH /api/messages/{id}/pin  →  toggle pin
    [HttpPatch("{id:guid}/pin")]
    public IActionResult TogglePin(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/messages/{id}/share  →  generate share link
    [HttpPost("{id:guid}/share")]
    public IActionResult CreateShareLink(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);

    // DELETE /api/messages/{id}/share  →  revoke share link
    [HttpDelete("{id:guid}/share")]
    public IActionResult RevokeShareLink(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);
}
