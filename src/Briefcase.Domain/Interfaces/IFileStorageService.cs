namespace Briefcase.Domain.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to blob storage.
    /// </summary>
    /// <param name="blobPath">The full blob path (e.g. "{userId}/{fileId}/{originalName}").</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="content">The file content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadAsync(string blobPath, string contentType, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited download URL (SAS) for the specified blob.
    /// </summary>
    Task<Uri> GetDownloadUrlAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the blob content as a stream.
    /// </summary>
    Task<Stream> DownloadAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default);
}
