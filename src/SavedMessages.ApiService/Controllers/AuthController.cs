using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedMessages.ApiService.Models;
using SavedMessages.ApiService.Services;
using SavedMessages.Domain.Entities;
using SavedMessages.Infrastructure.Persistence;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new ProblemDetails { Title = "Email already registered." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        SetRefreshTokenCookie(refreshToken);
        return Ok(new AuthResponse(accessToken, refreshToken, expiresAt));
    }

    // POST /api/auth/login  →  JWT access + refresh tokens
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || user.PasswordHash is null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        SetRefreshTokenCookie(refreshToken);
        return Ok(new AuthResponse(accessToken, refreshToken, expiresAt));
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request)
    {
        // Read refresh token from HttpOnly cookie first, fall back to request body
        var refreshTokenValue = Request.Cookies[RefreshTokenCookieName]
            ?? request?.RefreshToken;

        if (string.IsNullOrEmpty(refreshTokenValue))
            return BadRequest(new ProblemDetails { Title = "Refresh token is required." });

        var stored = await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshTokenValue);

        if (stored is null || !stored.IsActive)
            return Unauthorized(new ProblemDetails { Title = "Invalid or expired refresh token." });

        // Revoke the used token (rotation)
        stored.RevokedAt = DateTime.UtcNow;

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(stored.UserId, stored.User.Email);
        var newRefreshToken = await CreateRefreshTokenAsync(stored.UserId);

        SetRefreshTokenCookie(newRefreshToken);
        return Ok(new AuthResponse(accessToken, newRefreshToken, expiresAt));
    }

    // POST /api/auth/logout  →  revoke refresh token + clear cookie
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshTokenValue = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            var stored = await db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshTokenValue && r.RevokedAt == null);

            if (stored is not null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        ClearRefreshTokenCookie();
        return NoContent();
    }

    // GET /api/auth/oauth/{provider}  →  redirect to OAuth provider
    [HttpGet("oauth/{provider}")]
    public IActionResult OAuthRedirect(string provider) => StatusCode(StatusCodes.Status501NotImplemented);

    // GET /api/auth/oauth/{provider}/callback  →  JWT
    [HttpGet("oauth/{provider}/callback")]
    public IActionResult OAuthCallback(string provider) => StatusCode(StatusCodes.Status501NotImplemented);

    private async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = tokenService.GenerateRefreshToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(tokenService.RefreshTokenDays),
        };

        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        return token.Token;
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(tokenService.RefreshTokenDays),
            Path = "/api/auth",
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
        });
    }
}
