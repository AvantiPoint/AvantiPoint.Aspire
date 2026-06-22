using AvantiPoint.Aspire.Cloudflare.KV.Backends;

namespace AvantiPoint.Aspire.Cloudflare.KV;

/// <summary>
/// Default <see cref="ICloudflareKVClient"/>. Delegates to a backend (in-memory locally, the Workers KV
/// HTTP API when deployed) selected from the connection string.
/// </summary>
internal sealed class CloudflareKVClient(IKvBackend backend, string @namespace) : ICloudflareKVClient, IDisposable
{
    public string Namespace { get; } = @namespace;

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        => backend.GetAsync(key, cancellationToken);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => await backend.GetAsync(key, cancellationToken).ConfigureAwait(false) is not null;

    public Task PutAsync(string key, string value, TimeSpan? expirationTtl = null, CancellationToken cancellationToken = default)
        => backend.PutAsync(key, value, expirationTtl, cancellationToken);

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => backend.DeleteAsync(key, cancellationToken);

    public Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken cancellationToken = default)
        => backend.ListKeysAsync(prefix, cancellationToken);

    public void Dispose() => backend.Dispose();
}
