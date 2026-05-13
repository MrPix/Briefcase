using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SavedMessages.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public class FilesController : ControllerBase
{
    // POST /api/files  →  upload file (multipart)
    [HttpPost]
    public IActionResult UploadFile() => StatusCode(StatusCodes.Status501NotImplemented);

    // GET /api/files/{id}  →  download (redirect to SAS URL)
    [HttpGet("{id:guid}")]
    public IActionResult DownloadFile(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);

    // DELETE /api/files/{id}  →  delete file + blob
    [HttpDelete("{id:guid}")]
    public IActionResult DeleteFile(Guid id) => StatusCode(StatusCodes.Status501NotImplemented);
}
