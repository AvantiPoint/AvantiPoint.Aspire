using AvantiPoint.Aspire.Cloudflare.D1.Backends;

namespace AvantiPoint.Aspire.Cloudflare.D1;

/// <summary>
/// Default <see cref="ID1Client"/>. Delegates to a backend (SQLite locally, the D1 HTTP API when
/// deployed) and maps the normalized rows to the requested types.
/// </summary>
internal sealed class D1Client(ID1Backend backend) : ID1Client, IAsyncDisposable
{
    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default)
    {
        var result = await backend.ExecuteAsync(sql, parameters ?? [], cancellationToken).ConfigureAwait(false);
        return result.Rows.Select(D1RowMapper.Map<T>).ToList();
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default)
    {
        var result = await backend.ExecuteAsync(sql, parameters ?? [], cancellationToken).ConfigureAwait(false);
        var first = result.Rows.Count > 0 ? result.Rows[0] : null;
        return first is null ? default : D1RowMapper.Map<T>(first);
    }

    public async Task<int> ExecuteAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default)
    {
        var result = await backend.ExecuteAsync(sql, parameters ?? [], cancellationToken).ConfigureAwait(false);
        return result.Meta.RowsWritten;
    }

    public Task<D1Result> QueryResultAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default)
        => backend.ExecuteAsync(sql, parameters ?? [], cancellationToken);

    public Task<IReadOnlyList<D1Result>> BatchAsync(IEnumerable<D1Statement> statements, CancellationToken cancellationToken = default)
        => backend.BatchAsync(statements as IReadOnlyList<D1Statement> ?? statements.ToList(), cancellationToken);

    public ValueTask DisposeAsync() => backend.DisposeAsync();
}
