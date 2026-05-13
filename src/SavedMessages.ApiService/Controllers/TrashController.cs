using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/trash")]
public class TrashController : ControllerBase
{
    // GET /api/trash  →  list trashed messages (paged, IsDeleted = true)
    [HttpGet]
    public IActionResult GetTrashed() => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/trash/{id}/restore  →  restore message (IsDeleted = false, clears DeletedAt)
    [HttpPost("{id:guid}/restore")]
    public IActionResult Restore(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);
}
