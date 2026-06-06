using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Models;
using Briefcase.ApiService.Services;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController(AppDbContext db, TokenService tokenService) : ControllerBase
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
}
