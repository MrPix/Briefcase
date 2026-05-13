using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedMessages.Domain.Entities;
using SavedMessages.Domain.Interfaces;
using SavedMessages.Infrastructure.Persistence;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public class FilesController(AppDbContext db, IFileStorageService storage) : ControllerBase
{
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    // POST /api/files  →  upload file (multipart)
    [HttpPost]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var userId = GetUserId();
        var fileId = Guid.NewGuid();
        var blobPath = $"{userId}/{fileId}/{file.FileName}";

        await using var stream = file.OpenReadStream();
        await storage.UploadAsync(blobPath, file.ContentType, stream, ct);

        var attachment = new FileAttachment
        {
            Id = fileId,
            UserId = userId,
            OriginalName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            BlobPath = blobPath,
            CreatedAt = DateTime.UtcNow,
        };

        db.FileAttachments.Add(attachment);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(DownloadFile), new { id = fileId }, new
        {
            attachment.Id,
            attachment.OriginalName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.CreatedAt,
        });
    }

    // GET /api/files/{id}  →  download (redirect to presigned URL)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var attachment = await db.FileAttachments
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, ct);

        if (attachment is null)
            return NotFound();

        var stream = await storage.DownloadAsync(attachment.BlobPath, ct);
        return File(stream, attachment.ContentType, attachment.OriginalName);
    }

    // DELETE /api/files/{id}  →  delete file + blob
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFile(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var attachment = await db.FileAttachments
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, ct);

        if (attachment is null)
            return NotFound();

        await storage.DeleteAsync(attachment.BlobPath, ct);
        db.FileAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
