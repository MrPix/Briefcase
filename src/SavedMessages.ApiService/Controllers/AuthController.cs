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

        return Ok(new AuthResponse(accessToken, refreshToken, expiresAt));
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var stored = await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        if (stored is null || !stored.IsActive)
            return Unauthorized(new ProblemDetails { Title = "Invalid or expired refresh token." });

        // Revoke the used token (rotation)
        stored.RevokedAt = DateTime.UtcNow;

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(stored.UserId, stored.User.Email);
        var newRefreshToken = await CreateRefreshTokenAsync(stored.UserId);

        return Ok(new AuthResponse(accessToken, newRefreshToken, expiresAt));
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
}
