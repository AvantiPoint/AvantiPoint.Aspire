namespace AvantiPoint.Aspire.Cloudflare.Vectorize;

/// <summary>A vector and its id/metadata, as stored in a Vectorize index.</summary>
/// <param name="Id">The unique vector id.</param>
/// <param name="Values">The vector components (length must equal the index dimensions).</param>
/// <param name="Metadata">Optional metadata stored alongside the vector.</param>
public sealed record VectorRecord(string Id, ReadOnlyMemory<float> Values, IReadOnlyDictionary<string, object?>? Metadata = null);

/// <summary>A query result: a matched vector and its similarity score.</summary>
/// <param name="Id">The matched vector id.</param>
/// <param name="Score">The similarity score (semantics depend on the index metric).</param>
/// <param name="Values">The matched vector, when requested.</param>
/// <param name="Metadata">The matched vector's metadata, when requested.</param>
public sealed record VectorMatch(string Id, double Score, ReadOnlyMemory<float> Values, IReadOnlyDictionary<string, object?>? Metadata);

/// <summary>Options for a Vectorize query.</summary>
public sealed class VectorizeQueryOptions
{
    /// <summary>Include the matched vectors' values in the result.</summary>
    public bool ReturnValues { get; set; }

    /// <summary>Include the matched vectors' metadata in the result. Defaults to true.</summary>
    public bool ReturnMetadata { get; set; } = true;
}

/// <summary>
/// A client bound to a single Cloudflare Vectorize index. The same API runs against an in-memory vector
/// store during development and the Vectorize v2 HTTP API once deployed — the backend is chosen from the
/// Aspire-injected connection string, so consuming code is identical in both environments.
/// </summary>
public interface IVectorizeClient
{
    /// <summary>The Vectorize index this client is bound to.</summary>
    string IndexName { get; }

    /// <summary>The index's vector dimensionality.</summary>
    int Dimensions { get; }

    /// <summary>Inserts or updates vectors. On real Vectorize this is asynchronous (eventually consistent).</summary>
    Task UpsertAsync(IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default);

    /// <summary>Returns the <paramref name="topK"/> nearest vectors to <paramref name="vector"/>.</summary>
    Task<IReadOnlyList<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK = 5, VectorizeQueryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Fetches vectors by id.</summary>
    Task<IReadOnlyList<VectorRecord>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);

    /// <summary>Deletes vectors by id.</summary>
    Task DeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}
