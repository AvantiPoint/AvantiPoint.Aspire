using Amazon.S3;
using Amazon.S3.Model;

namespace AvantiPoint.Aspire.Cloudflare.R2;

/// <summary>
/// A thin, bucket-bound wrapper over <see cref="IAmazonS3"/> for a single R2 bucket. Mirrors the common
/// S3 operations but removes the need to pass the bucket name on every call. Use <see cref="S3"/> for
/// any operation not surfaced here.
/// </summary>
public interface IR2Client
{
    /// <summary>The bucket this client is bound to.</summary>
    string BucketName { get; }

    /// <summary>The underlying S3 client, for operations not exposed on this wrapper.</summary>
    IAmazonS3 S3 { get; }

    /// <summary>Gets an object from the bucket.</summary>
    Task<GetObjectResponse> GetObjectAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Opens a read stream for an object.</summary>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Puts an object into the bucket from a stream.</summary>
    Task<PutObjectResponse> PutObjectAsync(string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>Puts an object into the bucket from a string.</summary>
    Task<PutObjectResponse> PutObjectAsync(string key, string content, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes an object from the bucket.</summary>
    Task<DeleteObjectResponse> DeleteObjectAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns whether an object exists in the bucket.</summary>
    Task<bool> ObjectExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Lists object keys in the bucket, optionally filtered by prefix.</summary>
    Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken cancellationToken = default);
}
