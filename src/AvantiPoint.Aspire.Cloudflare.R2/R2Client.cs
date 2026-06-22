using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace AvantiPoint.Aspire.Cloudflare.R2;

/// <summary>Default <see cref="IR2Client"/> — binds an <see cref="IAmazonS3"/> to a single bucket.</summary>
internal sealed class R2Client(IAmazonS3 s3, string bucketName) : IR2Client
{
    public string BucketName { get; } = bucketName;

    public IAmazonS3 S3 { get; } = s3;

    public Task<GetObjectResponse> GetObjectAsync(string key, CancellationToken cancellationToken = default)
        => S3.GetObjectAsync(BucketName, key, cancellationToken);

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await S3.GetObjectAsync(BucketName, key, cancellationToken).ConfigureAwait(false);
        return response.ResponseStream;
    }

    public Task<PutObjectResponse> PutObjectAsync(string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
        => S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        }, cancellationToken);

    public Task<PutObjectResponse> PutObjectAsync(string key, string content, string? contentType = null, CancellationToken cancellationToken = default)
        => S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            ContentBody = content,
            ContentType = contentType,
        }, cancellationToken);

    public Task<DeleteObjectResponse> DeleteObjectAsync(string key, CancellationToken cancellationToken = default)
        => S3.DeleteObjectAsync(BucketName, key, cancellationToken);

    public async Task<bool> ObjectExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await S3.GetObjectMetadataAsync(BucketName, key, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        string? continuationToken = null;
        do
        {
            var response = await S3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = prefix,
                ContinuationToken = continuationToken,
            }, cancellationToken).ConfigureAwait(false);

            keys.AddRange(response.S3Objects.Select(o => o.Key));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return keys;
    }
}
