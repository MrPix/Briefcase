using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Hubs;
using Briefcase.ApiService.Models;
using Briefcase.ApiService.Services;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController(AppDbContext db, TokenService tokenService, IHubContext<MessageHub> hub) : ControllerBase
{
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static DeviceResponse ToResponse(Device d) => new(
        d.Id, d.Name, d.Platform, d.LastSeenAt, d.CreatedAt);

    // GET /api/devices  →  list registered devices for the current user
    [HttpGet]
    public async Task<IActionResult> GetDevices()
    {
        var userId = GetUserId();
        var devices = await db.Devices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenAt)
            .Select(d => ToResponse(d))
            .ToListAsync();

        return Ok(devices);
    }

    // DELETE /api/devices/{id}  →  remove a device
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveDevice(Guid id)
    {
        var userId = GetUserId();
        var device = await db.Devices
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (device is null)
            return NotFound();

        db.Devices.Remove(device);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/devices/pair-code  →  generate a short-lived signed QR pairing token (JWT, 5 min TTL)
    [HttpPost("pair-code")]
    public IActionResult GeneratePairCode()
    {
        var userId = GetUserId();
        var token = tokenService.GenerateDevicePairToken(userId);
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        return Ok(new PairCodeResponse(token, expiresAt));
    }

    // POST /api/devices/claim  →  claim device via QR pairing token
    [AllowAnonymous]
    [HttpPost("claim")]
    public async Task<IActionResult> ClaimDevice([FromBody] ClaimDeviceRequest request)
    {
        var principal = tokenService.ValidateDevicePairToken(request.Token);
        if (principal is null)
            return Unauthorized(new ProblemDetails { Title = "Invalid or expired pairing token." });

        var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return Unauthorized(new ProblemDetails { Title = "User not found." });

        var device = new Device
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.DeviceName,
            Platform = request.Platform,
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        db.Devices.Add(device);
        await db.SaveChangesAsync();

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user.Id, user.Email);
        return Ok(new AuthResponse(accessToken, string.Empty, expiresAt));
    }

    // POST /api/devices/login-code  →  a not-yet-authenticated device requests a short login code
    [AllowAnonymous]
    [HttpPost("login-code")]
    public async Task<IActionResult> CreateLoginCode([FromBody] CreateLoginCodeRequest request)
    {
        // Generate a unique, unambiguous 8-character code (retry on the rare collision).
        string code;
        var attempts = 0;
        do
        {
            code = GenerateLoginCode();
            attempts++;
        }
        while (attempts < 5 && await db.DeviceLoginCodes.AnyAsync(c => c.Code == code && !c.IsConsumed));

        var platform = Enum.TryParse<Platform>(request.Platform, ignoreCase: true, out var p) ? p : Platform.Web;

        var entry = new DeviceLoginCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            DeviceName = request.DeviceName,
            Platform = platform,
            ExpiresAt = DateTime.UtcNow.AddMinutes(LoginCodeTtlMinutes),
            CreatedAt = DateTime.UtcNow,
        };

        db.DeviceLoginCodes.Add(entry);
        await db.SaveChangesAsync();

        return Ok(new LoginCodeResponse(entry.Code, entry.ExpiresAt));
    }

    // GET /api/devices/login-code/{code}  →  the pending device polls for approval
    [AllowAnonymous]
    [HttpGet("login-code/{code}")]
    public async Task<IActionResult> PollLoginCode(string code)
    {
        var normalized = NormalizeLoginCode(code);
        var entry = await db.DeviceLoginCodes
            .FirstOrDefaultAsync(c => c.Code == normalized);

        if (entry is null || entry.IsConsumed)
            return Ok(new LoginCodePollResponse("notfound"));

        if (entry.IsExpired)
            return Ok(new LoginCodePollResponse("expired"));

        if (!entry.IsApproved || entry.UserId is null)
            return Ok(new LoginCodePollResponse("pending"));

        var user = await db.Users.FindAsync(entry.UserId.Value);
        if (user is null)
            return Ok(new LoginCodePollResponse("notfound"));

        // Redeem the approval: register the device, mint tokens, and consume the code.
        entry.IsConsumed = true;

        db.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = entry.DeviceName,
            Platform = entry.Platform,
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        await db.SaveChangesAsync();

        return Ok(new LoginCodePollResponse("approved", accessToken, refreshToken, expiresAt));
    }

    // POST /api/devices/login-code/approve  →  an authenticated device approves a login code
    [HttpPost("login-code/approve")]
    public async Task<IActionResult> ApproveLoginCode([FromBody] ApproveLoginCodeRequest request)
    {
        var userId = GetUserId();
        var normalized = NormalizeLoginCode(request.Code);

        var entry = await db.DeviceLoginCodes
            .FirstOrDefaultAsync(c => c.Code == normalized && !c.IsConsumed);

        if (entry is null || entry.IsExpired)
            return NotFound(new ProblemDetails { Title = "Invalid or expired login code." });

        if (entry.IsApproved)
            return Conflict(new ProblemDetails { Title = "This code has already been approved." });

        entry.UserId = userId;
        entry.IsApproved = true;
        await db.SaveChangesAsync();

        // Notify the waiting device over SignalR so it can redeem its tokens immediately.
        await hub.Clients.Group($"login-code:{normalized}")
            .SendAsync(MessageHub.LoginCodeApproved, new { code = normalized });

        return Ok(new ApproveLoginCodeResponse(entry.DeviceName, entry.Platform));
    }

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
        return token.Token;
    }

    private const int LoginCodeTtlMinutes = 5;

    // Unambiguous alphabet (no 0/O/1/I) for codes read and typed by humans.
    private const string LoginCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static string GenerateLoginCode()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = LoginCodeAlphabet[RandomNumberGenerator.GetInt32(LoginCodeAlphabet.Length)];
        return new string(chars);
    }

    private static string NormalizeLoginCode(string code) =>
        code.Trim().ToUpperInvariant();
}
