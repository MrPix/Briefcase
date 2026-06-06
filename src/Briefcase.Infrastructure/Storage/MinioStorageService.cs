using Amazon.S3;
using Amazon.S3.Model;
using Briefcase.Domain.Interfaces;

namespace Briefcase.Infrastructure.Storage;

public class MinioStorageService(IAmazonS3 s3, string bucketName = "Briefcase") : IFileStorageService
{
    public async Task UploadAsync(string blobPath, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
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
            BucketName = bucketName,
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
            BucketName = bucketName,
            Key = blobPath,
        };

        var response = await s3.GetObjectAsync(request, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = blobPath,
        };

        await s3.DeleteObjectAsync(request, cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await s3.EnsureBucketExistsAsync(bucketName);
        }
        catch
        {
            // Bucket already exists or check not supported
        }
    }
}
