using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvantiPoint.Aspire.Cloudflare.D1.Backends;

/// <summary>
/// D1 implementation that targets the Cloudflare D1 HTTP query API. The database id is resolved from
/// the database name on first use (via the D1 list endpoint) and cached, so the connection string only
/// needs the account id, database name and an API token.
/// </summary>
internal sealed class HttpD1Backend : ID1Backend
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    private readonly string _databaseName;
    private readonly SemaphoreSlim _idLock = new(1, 1);
    private string? _databaseId;

    public HttpD1Backend(HttpClient httpClient, string accountId, string databaseName, string apiToken)
    {
        _httpClient = httpClient;
        _accountId = accountId;
        _databaseName = databaseName;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    }

    public async Task<D1Result> ExecuteAsync(string sql, object?[] parameters, CancellationToken cancellationToken)
    {
        var databaseId = await ResolveDatabaseIdAsync(cancellationToken).ConfigureAwait(false);
        var results = await QueryAsync(databaseId, [new D1Statement(sql, parameters)], cancellationToken).ConfigureAwait(false);
        return results.Count > 0 ? results[0] : new D1Result();
    }

    public async Task<IReadOnlyList<D1Result>> BatchAsync(IReadOnlyList<D1Statement> statements, CancellationToken cancellationToken)
    {
        var databaseId = await ResolveDatabaseIdAsync(cancellationToken).ConfigureAwait(false);

        // D1's /query endpoint binds a single params array, so multi-statement batches are sent one at a
        // time to keep parameter binding unambiguous.
        var results = new List<D1Result>(statements.Count);
        foreach (var statement in statements)
        {
            var single = await QueryAsync(databaseId, [statement], cancellationToken).ConfigureAwait(false);
            results.Add(single.Count > 0 ? single[0] : new D1Result());
        }

        return results;
    }

    private async Task<IReadOnlyList<D1Result>> QueryAsync(string databaseId, IReadOnlyList<D1Statement> statements, CancellationToken cancellationToken)
    {
        var statement = statements[0];
        using var request = new HttpRequestMessage(HttpMethod.Post, $"accounts/{_accountId}/d1/database/{databaseId}/query")
        {
            Content = JsonContent.Create(new D1QueryRequest
            {
                Sql = statement.Sql,
                Params = statement.Parameters.Select(p => p?.ToString()).ToArray(),
            }),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var envelope = await response.Content.ReadFromJsonAsync<D1Envelope>(cancellationToken).ConfigureAwait(false);

        if (envelope is null || !envelope.Success)
        {
            var message = envelope?.Errors.FirstOrDefault()?.Message ?? $"HTTP {(int)response.StatusCode}";
            throw new InvalidOperationException($"D1 query failed: {message}");
        }

        return envelope.Result.Select(MapStatementResult).ToList();
    }

    private static D1Result MapStatementResult(D1StatementResult statement)
    {
        var rows = new List<D1Row>(statement.Results.Count);
        foreach (var rawRow in statement.Results)
        {
            var values = new Dictionary<string, object?>(rawRow.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in rawRow)
            {
                values[pair.Key] = ToClrValue(pair.Value);
            }

            rows.Add(new D1Row(values));
        }

        return new D1Result
        {
            Rows = rows,
            Meta = new D1Meta
            {
                RowsRead = statement.Meta?.RowsRead ?? rows.Count,
                RowsWritten = statement.Meta?.RowsWritten ?? 0,
                LastRowId = statement.Meta?.LastRowId ?? 0,
                Duration = statement.Meta?.Duration ?? 0,
            },
        };
    }

    private static object? ToClrValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        _ => element.GetRawText(),
    };

    private async Task<string> ResolveDatabaseIdAsync(CancellationToken cancellationToken)
    {
        if (_databaseId is not null)
        {
            return _databaseId;
        }

        await _idLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_databaseId is not null)
            {
                return _databaseId;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"accounts/{_accountId}/d1/database?name={Uri.EscapeDataString(_databaseName)}");
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var envelope = await response.Content.ReadFromJsonAsync<D1ListEnvelope>(cancellationToken).ConfigureAwait(false);

            var match = envelope?.Result.FirstOrDefault(d => string.Equals(d.Name, _databaseName, StringComparison.Ordinal));
            _databaseId = match?.Uuid
                ?? throw new InvalidOperationException(
                    $"D1 database '{_databaseName}' was not found in account '{_accountId}'. Has it been provisioned (aspire deploy)?");
            return _databaseId;
        }
        finally
        {
            _idLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _idLock.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class D1QueryRequest
    {
        [JsonPropertyName("sql")]
        public string Sql { get; init; } = string.Empty;

        [JsonPropertyName("params")]
        public string?[] Params { get; init; } = [];
    }

    private sealed class D1Envelope
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("errors")]
        public List<D1Error> Errors { get; init; } = [];

        [JsonPropertyName("result")]
        public List<D1StatementResult> Result { get; init; } = [];
    }

    private sealed class D1ListEnvelope
    {
        [JsonPropertyName("result")]
        public List<D1DatabaseInfo> Result { get; init; } = [];
    }

    private sealed class D1DatabaseInfo
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }

    private sealed class D1StatementResult
    {
        [JsonPropertyName("results")]
        public List<Dictionary<string, JsonElement>> Results { get; init; } = [];

        [JsonPropertyName("meta")]
        public D1MetaDto? Meta { get; init; }
    }

    private sealed class D1MetaDto
    {
        [JsonPropertyName("rows_read")]
        public int RowsRead { get; init; }

        [JsonPropertyName("rows_written")]
        public int RowsWritten { get; init; }

        [JsonPropertyName("last_row_id")]
        public long LastRowId { get; init; }

        [JsonPropertyName("duration")]
        public double Duration { get; init; }
    }

    private sealed class D1Error
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
