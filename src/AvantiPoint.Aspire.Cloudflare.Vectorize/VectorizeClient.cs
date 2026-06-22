using AvantiPoint.Aspire.Cloudflare.Vectorize.Backends;

namespace AvantiPoint.Aspire.Cloudflare.Vectorize;

/// <summary>
/// Default <see cref="IVectorizeClient"/>. Delegates to a backend (in-memory locally, the Vectorize v2
/// HTTP API when deployed) selected from the connection string.
/// </summary>
internal sealed class VectorizeClient(IVectorizeBackend backend, string indexName, int dimensions) : IVectorizeClient, IDisposable
{
    public string IndexName { get; } = indexName;

    public int Dimensions { get; } = dimensions;

    public Task UpsertAsync(IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default)
        => backend.UpsertAsync(records as IReadOnlyList<VectorRecord> ?? records.ToList(), cancellationToken);

    public Task<IReadOnlyList<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK = 5, VectorizeQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new VectorizeQueryOptions();
        return backend.QueryAsync(vector, topK, options.ReturnValues, options.ReturnMetadata, cancellationToken);
    }

    public Task<IReadOnlyList<VectorRecord>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        => backend.GetByIdsAsync(ids as IReadOnlyList<string> ?? ids.ToList(), cancellationToken);

    public Task DeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        => backend.DeleteAsync(ids as IReadOnlyList<string> ?? ids.ToList(), cancellationToken);

    public void Dispose() => backend.Dispose();
}
