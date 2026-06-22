namespace AvantiPoint.Aspire.Cloudflare.KV.Backends;

/// <summary>
/// The storage backend behind an <see cref="ICloudflareKVClient"/>. Two implementations exist —
/// in-memory (dev) and the Workers KV HTTP API (deployed) — selected from the connection string.
/// </summary>
internal interface IKvBackend : IDisposable
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    Task PutAsync(string key, string value, TimeSpan? expirationTtl, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListKeysAsync(string? prefix, CancellationToken cancellationToken);
}
