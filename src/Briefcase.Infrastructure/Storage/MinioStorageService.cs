using Amazon.S3;
using Amazon.S3.Model;
using Briefcase.Domain.Interfaces;

namespace Briefcase.Infrastructure.Storage;

public class MinioStorageService(IAmazonS3 s3, string bucketName = "briefcase") : IFileStorageService
{
    private readonly string normalizedBucketName = NormalizeBucketName(bucketName);

    public async Task UploadAsync(string blobPath, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = normalizedBucketName,
            Key = blobPath,
            ContentType = contentType,
            InputStream = content,
        };

        await s3.PutObjectAsync(request, cancellationToken);
    }

    public Task<Uri> GetDownloadUrlAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = normalizedBucketName,
            Key = blobPath,
            Expires = DateTime.UtcNow.AddMinutes(15),
            Verb = HttpVerb.GET,
        };

        var url = s3.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }

    public async Task<Stream> DownloadAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = normalizedBucketName,
            Key = blobPath,
        };

        var response = await s3.GetObjectAsync(request, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = normalizedBucketName,
            Key = blobPath,
        };

        await s3.DeleteObjectAsync(request, cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await s3.EnsureBucketExistsAsync(normalizedBucketName);
        }
        catch
        {
            // Bucket already exists or check not supported
        }
    }

    private static string NormalizeBucketName(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Storage bucket name cannot be empty.", nameof(value));
        }

        return normalized;
    }
}
