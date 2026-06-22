namespace AvantiPoint.Aspire.Cloudflare.KV;

/// <summary>
/// A client bound to a single Cloudflare Workers KV namespace. The same API runs against an in-memory
/// store during development and the Workers KV HTTP API once deployed — the backend is chosen from the
/// Aspire-injected connection string, so consuming code is identical in both environments.
/// </summary>
public interface ICloudflareKVClient
{
    /// <summary>The KV namespace title this client is bound to.</summary>
    string Namespace { get; }

    /// <summary>Gets a value by key, or <c>null</c> if the key does not exist.</summary>
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a key exists.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Writes a value, optionally with an expiration TTL.</summary>
    Task PutAsync(string key, string value, TimeSpan? expirationTtl = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes a key. No-op if the key does not exist.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Lists keys, optionally filtered by prefix.</summary>
    Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken cancellationToken = default);
}
