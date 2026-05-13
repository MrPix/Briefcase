using Microsoft.AspNetCore.Mvc;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // POST /api/auth/register
    [HttpPost("register")]
    public IActionResult Register() => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/auth/login  →  JWT access + refresh tokens
    [HttpPost("login")]
    public IActionResult Login() => StatusCode(StatusCodes.Status501NotImplemented);

    // GET /api/auth/oauth/{provider}  →  redirect to OAuth provider
    [HttpGet("oauth/{provider}")]
    public IActionResult OAuthRedirect(string provider) => StatusCode(StatusCodes.Status501NotImplemented);

    // GET /api/auth/oauth/{provider}/callback  →  JWT
    [HttpGet("oauth/{provider}/callback")]
    public IActionResult OAuthCallback(string provider) => StatusCode(StatusCodes.Status501NotImplemented);

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public IActionResult Refresh() => StatusCode(StatusCodes.Status501NotImplemented);
}
