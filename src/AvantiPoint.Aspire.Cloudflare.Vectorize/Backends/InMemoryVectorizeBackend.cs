using System.Collections.Concurrent;

namespace AvantiPoint.Aspire.Cloudflare.Vectorize.Backends;

/// <summary>
/// In-memory vector store used for the local dev loop (and tests). Computes nearest neighbours with the
/// same metric semantics as Vectorize: cosine / dot-product rank highest-first, euclidean ranks
/// lowest-distance-first.
/// </summary>
internal sealed class InMemoryVectorizeBackend(string metric, int dimensions) : IVectorizeBackend
{
    private readonly ConcurrentDictionary<string, VectorRecord> _store = new();
    private readonly string _metric = metric;
    private readonly int _dimensions = dimensions;

    public Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken)
    {
        foreach (var record in records)
        {
            ValidateDimensions(record.Values.Length, $"vector '{record.Id}'");
            _store[record.Id] = record;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK, bool returnValues, bool returnMetadata, CancellationToken cancellationToken)
    {
        ValidateDimensions(vector.Length, "query vector");

        // Materialize the query vector to an array so it can be used inside the scoring lambda (a Span
        // ref-local can't be captured).
        var query = vector.ToArray();
        var euclidean = string.Equals(_metric, "euclidean", StringComparison.OrdinalIgnoreCase);

        var scored = _store.Values
            .Select(record => new
            {
                Record = record,
                Score = Score(query, record.Values.Span),
            })
            .OrderBy(x => euclidean ? x.Score : -x.Score) // euclidean: smaller is better; others: larger is better
            .Take(topK)
            .Select(x => new VectorMatch(
                x.Record.Id,
                x.Score,
                returnValues ? x.Record.Values : ReadOnlyMemory<float>.Empty,
                returnMetadata ? x.Record.Metadata : null))
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorMatch>>(scored);
    }

    public Task<IReadOnlyList<VectorRecord>> GetByIdsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        var found = ids
            .Where(_store.ContainsKey)
            .Select(id => _store[id])
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorRecord>>(found);
    }

    public Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids)
        {
            _store.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    // Real Vectorize indexes are fixed-dimension and reject mismatches, so the emulator does too —
    // otherwise a wrong-length vector would silently pass local testing and fail after deployment.
    private void ValidateDimensions(int length, string what)
    {
        if (_dimensions > 0 && length != _dimensions)
        {
            throw new ArgumentException(
                $"The {what} has {length} dimensions but the index expects {_dimensions}.");
        }
    }

    private double Score(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var length = Math.Min(a.Length, b.Length);
        double dot = 0, magA = 0, magB = 0, sqDist = 0;
        for (var i = 0; i < length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
            var d = a[i] - b[i];
            sqDist += d * d;
        }

        return _metric.ToLowerInvariant() switch
        {
            "euclidean" => Math.Sqrt(sqDist),
            "dot-product" => dot,
            _ => magA == 0 || magB == 0 ? 0 : dot / (Math.Sqrt(magA) * Math.Sqrt(magB)), // cosine
        };
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }
}
