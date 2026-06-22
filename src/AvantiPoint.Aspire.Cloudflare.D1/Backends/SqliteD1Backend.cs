using System.Text;
using Microsoft.Data.Sqlite;

namespace AvantiPoint.Aspire.Cloudflare.D1.Backends;

/// <summary>
/// SQLite-backed D1 implementation used for the local dev loop (and tests). D1 is SQLite under the hood,
/// so this is a faithful local emulator. SQL uses <c>?</c> positional placeholders, which are rewritten
/// to named parameters for <c>Microsoft.Data.Sqlite</c>.
/// </summary>
internal sealed class SqliteD1Backend : ID1Backend
{
    private readonly string _connectionString;

    public SqliteD1Backend(string dataSource)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dataSource,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }

    public async Task<D1Result> ExecuteAsync(string sql, object?[] parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteOnConnectionAsync(connection, sql, parameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<D1Result>> BatchAsync(IReadOnlyList<D1Statement> statements, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var results = new List<D1Result>(statements.Count);
        foreach (var statement in statements)
        {
            results.Add(await ExecuteOnConnectionAsync(connection, statement.Sql, statement.Parameters, cancellationToken, transaction)
                .ConfigureAwait(false));
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return results;
    }

    private static async Task<D1Result> ExecuteOnConnectionAsync(
        SqliteConnection connection,
        string sql,
        object?[] parameters,
        CancellationToken cancellationToken,
        SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = RewritePositionalParameters(sql, out var parameterNames);

        for (var i = 0; i < parameterNames.Count; i++)
        {
            var value = i < parameters.Length ? parameters[i] : null;
            command.Parameters.AddWithValue(parameterNames[i], value ?? DBNull.Value);
        }

        var rows = new List<D1Row>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            var hasColumns = reader.FieldCount > 0;
            while (hasColumns && await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var values = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    values[reader.GetName(i)] = value;
                }

                rows.Add(new D1Row(values));
            }
        }

        var changes = GetScalarLong(connection, transaction, "SELECT changes()");
        var lastRowId = changes > 0 ? GetScalarLong(connection, transaction, "SELECT last_insert_rowid()") : 0;

        return new D1Result
        {
            Rows = rows,
            Meta = new D1Meta
            {
                RowsRead = rows.Count,
                RowsWritten = (int)changes,
                LastRowId = lastRowId,
            },
        };
    }

    private static long GetScalarLong(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        var result = command.ExecuteScalar();
        return result is long l ? l : 0;
    }

    /// <summary>
    /// Rewrites bare <c>?</c> positional placeholders to <c>@p0</c>, <c>@p1</c>, ... so they can be bound
    /// by <c>Microsoft.Data.Sqlite</c>. Skips <c>?</c> inside single-quoted string literals.
    /// </summary>
    internal static string RewritePositionalParameters(string sql, out IReadOnlyList<string> parameterNames)
    {
        var names = new List<string>();
        var builder = new StringBuilder(sql.Length + 8);
        var inString = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            if (c == '\'')
            {
                inString = !inString;
                builder.Append(c);
            }
            else if (c == '?' && !inString)
            {
                var name = $"@p{names.Count}";
                names.Add(name);
                builder.Append(name);
            }
            else
            {
                builder.Append(c);
            }
        }

        parameterNames = names;
        return builder.ToString();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
