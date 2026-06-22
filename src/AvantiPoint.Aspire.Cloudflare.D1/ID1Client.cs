namespace AvantiPoint.Aspire.Cloudflare.D1;

/// <summary>
/// A client bound to a single Cloudflare D1 database. The same API runs against a local SQLite file
/// during development and the D1 HTTP query API once deployed — the backend is chosen from the
/// Aspire-injected connection string, so consuming code is identical in both environments.
/// SQL uses <c>?</c> positional placeholders; pass values positionally.
/// </summary>
public interface ID1Client
{
    /// <summary>Runs a query and maps each row to <typeparamref name="T"/> (a POCO, a scalar, or <see cref="D1Row"/>).</summary>
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>Runs a query and maps the first row to <typeparamref name="T"/>, or returns <c>default</c> if there are none.</summary>
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>Executes a non-query statement (INSERT/UPDATE/DELETE/DDL) and returns the number of rows affected.</summary>
    Task<int> ExecuteAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>Runs a query and returns the full <see cref="D1Result"/> (rows + metadata).</summary>
    Task<D1Result> QueryResultAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>Runs several statements in order, returning a result per statement.</summary>
    Task<IReadOnlyList<D1Result>> BatchAsync(IEnumerable<D1Statement> statements, CancellationToken cancellationToken = default);
}
