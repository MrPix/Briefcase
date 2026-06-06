using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Briefcase.Domain.Constants;
using Briefcase.Domain.Entities;
using Briefcase.Domain.Interfaces;
using Briefcase.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Briefcase.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public class FilesController(AppDbContext db, IFileStorageService storage) : ControllerBase
{
    private const string PreviewContentType = "image/jpeg";

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static bool IsImage(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

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
        string? previewBlobPath = null;

        await using var stream = file.OpenReadStream();
        await storage.UploadAsync(blobPath, file.ContentType, stream, ct);

        if (IsImage(file.ContentType))
        {
            await using var imageStream = file.OpenReadStream();
            var previewStream = await TryCreatePreviewAsync(imageStream, ct);

            if (previewStream is not null)
            {
                await using (previewStream)
                {
                    previewBlobPath = $"{userId}/{fileId}/preview.jpg";
                    await storage.UploadAsync(previewBlobPath, PreviewContentType, previewStream, ct);
                }
            }
        }

        var attachment = new FileAttachment
        {
            Id = fileId,
            UserId = userId,
            OriginalName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            BlobPath = blobPath,
            PreviewBlobPath = previewBlobPath,
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

    // GET /api/files/{id}/preview  →  download preview image
    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> DownloadPreview(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var attachment = await db.FileAttachments
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, ct);

        if (attachment is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(attachment.PreviewBlobPath))
            return NotFound();

        var stream = await storage.DownloadAsync(attachment.PreviewBlobPath, ct);
        return File(stream, PreviewContentType);
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

        if (!string.IsNullOrWhiteSpace(attachment.PreviewBlobPath))
            await storage.DeleteAsync(attachment.PreviewBlobPath, ct);

        db.FileAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static async Task<MemoryStream?> TryCreatePreviewAsync(Stream source, CancellationToken ct)
    {
        try
        {
            using Image image = await Image.LoadAsync(source, ct);
            image.Mutate(ctx =>
            {
                ctx.AutoOrient();
                ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(FilePreviewConstants.Width, FilePreviewConstants.Height)
                });
            });

            var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 80 }, ct);
            output.Position = 0;
            return output;
        }
        catch (UnknownImageFormatException)
        {
            return null;
        }
        catch (InvalidImageContentException)
        {
            return null;
        }
    }
}
