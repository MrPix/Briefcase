using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    // GET /api/devices  →  list registered devices for the current user
    [HttpGet]
    public IActionResult GetDevices() => StatusCode(StatusCodes.Status501NotImplemented);

    // DELETE /api/devices/{id}  →  remove a device
    [HttpDelete("{id:guid}")]
    public IActionResult RemoveDevice(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/devices/pair-code  →  generate a short-lived signed QR pairing token (JWT, 5 min TTL)
    [HttpPost("pair-code")]
    public IActionResult GeneratePairCode() => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/devices/claim  →  claim device via QR pairing token
    [HttpPost("claim")]
    public IActionResult ClaimDevice() => StatusCode(StatusCodes.Status501NotImplemented);
}
