namespace AvantiPoint.Aspire.Cloudflare.D1;

/// <summary>
/// A single row returned by a D1 query, exposing columns by name. Values are normalized to common CLR
/// types (<see cref="long"/>, <see cref="double"/>, <see cref="string"/>, <see cref="bool"/>,
/// <c>byte[]</c>, or <c>null</c>) so the same row shape comes back from the SQLite and HTTP backends.
/// </summary>
public sealed class D1Row(IReadOnlyDictionary<string, object?> values)
{
    /// <summary>The raw column values keyed by column name (case-insensitive lookup via <see cref="Get"/>).</summary>
    public IReadOnlyDictionary<string, object?> Values { get; } = values;

    /// <summary>Gets a raw column value by name, or <c>null</c> if absent.</summary>
    public object? this[string column] => Values.TryGetValue(column, out var value) ? value : null;

    /// <summary>Gets a column value converted to <typeparamref name="T"/>.</summary>
    public T? Get<T>(string column) => D1RowMapper.ConvertValue<T>(this[column]);
}

/// <summary>Execution metadata returned by D1 (and approximated for the SQLite backend).</summary>
public sealed class D1Meta
{
    /// <summary>Number of rows changed by the statement (INSERT/UPDATE/DELETE).</summary>
    public int RowsWritten { get; init; }

    /// <summary>Number of rows read by the statement.</summary>
    public int RowsRead { get; init; }

    /// <summary>The rowid of the last inserted row, when applicable.</summary>
    public long LastRowId { get; init; }

    /// <summary>Server-reported duration in milliseconds (0 for the SQLite backend).</summary>
    public double Duration { get; init; }
}

/// <summary>The result of a single D1 statement: the returned rows plus execution metadata.</summary>
public sealed class D1Result
{
    /// <summary>The rows returned by the statement (empty for non-query statements).</summary>
    public IReadOnlyList<D1Row> Rows { get; init; } = [];

    /// <summary>Execution metadata.</summary>
    public D1Meta Meta { get; init; } = new();
}

/// <summary>A parameterized SQL statement. Use <c>?</c> placeholders and supply positional parameters.</summary>
public sealed class D1Statement(string sql, params object?[] parameters)
{
    /// <summary>The SQL text, using <c>?</c> positional placeholders.</summary>
    public string Sql { get; } = sql;

    /// <summary>The positional parameter values.</summary>
    public object?[] Parameters { get; } = parameters ?? [];
}
