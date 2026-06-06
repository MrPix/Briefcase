using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/e2ee")]
public class E2eeController : ControllerBase
{
    // GET /api/e2ee/settings  →  KDF salt, params, key verifier, isEnabled
    [HttpGet("settings")]
    public IActionResult GetSettings() => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/e2ee/enable  →  store KDF artefacts; mark IsEnabled = true
    [HttpPost("enable")]
    public IActionResult Enable() => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/e2ee/disable  →  clear E2EE settings
    [HttpPost("disable")]
    public IActionResult Disable() => StatusCode(StatusCodes.Status501NotImplemented);

    // PUT /api/e2ee/change-passphrase  →  replace KDF artefacts after client re-encryption
    [HttpPut("change-passphrase")]
    public IActionResult ChangePassphrase() => StatusCode(StatusCodes.Status501NotImplemented);
}
