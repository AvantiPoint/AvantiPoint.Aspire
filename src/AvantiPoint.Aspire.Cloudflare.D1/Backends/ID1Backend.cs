namespace AvantiPoint.Aspire.Cloudflare.D1.Backends;

/// <summary>
/// The storage backend behind an <see cref="ID1Client"/>. Two implementations exist — local SQLite
/// (dev) and the D1 HTTP query API (deployed) — selected from the connection string's <c>Provider</c>.
/// Both produce the same normalized <see cref="D1Result"/> so the client surface is identical.
/// </summary>
internal interface ID1Backend : IAsyncDisposable
{
    /// <summary>Runs a single statement and returns its rows + metadata.</summary>
    Task<D1Result> ExecuteAsync(string sql, object?[] parameters, CancellationToken cancellationToken);

    /// <summary>Runs several statements in order, returning one result per statement.</summary>
    Task<IReadOnlyList<D1Result>> BatchAsync(IReadOnlyList<D1Statement> statements, CancellationToken cancellationToken);
}
