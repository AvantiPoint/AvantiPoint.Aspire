namespace AvantiPoint.Aspire.Cloudflare.Vectorize.Backends;

/// <summary>
/// The storage backend behind an <see cref="IVectorizeClient"/>. Two implementations exist — in-memory
/// (dev) and the Vectorize v2 HTTP API (deployed) — selected from the connection string's <c>Provider</c>.
/// </summary>
internal interface IVectorizeBackend : IDisposable
{
    Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken);

    Task<IReadOnlyList<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK, bool returnValues, bool returnMetadata, CancellationToken cancellationToken);

    Task<IReadOnlyList<VectorRecord>> GetByIdsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken);

    Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken);
}
