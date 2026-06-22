using System.Collections.Concurrent;

namespace AvantiPoint.Aspire.Cloudflare.KV.Backends;

/// <summary>In-memory KV store used for the local dev loop (and tests), with TTL expiry support.</summary>
internal sealed class InMemoryKvBackend : IKvBackend
{
    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt is { } expiry && expiry <= DateTimeOffset.UtcNow)
            {
                _store.TryRemove(key, out _);
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(entry.Value);
        }

        return Task.FromResult<string?>(null);
    }

    public Task PutAsync(string key, string value, TimeSpan? expirationTtl, CancellationToken cancellationToken)
    {
        var expiresAt = expirationTtl is { } ttl ? DateTimeOffset.UtcNow.Add(ttl) : (DateTimeOffset?)null;
        _store[key] = new Entry(value, expiresAt);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string? prefix, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var keys = _store
            .Where(kvp => kvp.Value.ExpiresAt is null || kvp.Value.ExpiresAt > now)
            .Select(kvp => kvp.Key)
            .Where(k => prefix is null || k.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(keys);
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }

    private readonly record struct Entry(string Value, DateTimeOffset? ExpiresAt);
}
