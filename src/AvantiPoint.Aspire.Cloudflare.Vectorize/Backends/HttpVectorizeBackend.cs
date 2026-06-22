using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvantiPoint.Aspire.Cloudflare.Vectorize.Backends;

/// <summary>
/// Vectorize implementation that targets the Cloudflare Vectorize v2 HTTP API. Upserts use the NDJSON
/// endpoint; queries/gets/deletes use the JSON endpoints. Note Vectorize mutations are asynchronous
/// (eventually consistent), so a query may not immediately reflect a just-completed upsert.
/// </summary>
internal sealed class HttpVectorizeBackend : IVectorizeBackend
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _basePath;

    public HttpVectorizeBackend(HttpClient httpClient, string accountId, string indexName, string apiToken)
    {
        _httpClient = httpClient;
        _basePath = $"accounts/{accountId}/vectorize/v2/indexes/{indexName}";

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    }

    public async Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken)
    {
        // The upsert endpoint takes newline-delimited JSON (one vector per line).
        var ndjson = new StringBuilder();
        foreach (var record in records)
        {
            var dto = new VectorDto
            {
                Id = record.Id,
                Values = record.Values.ToArray(),
                Metadata = record.Metadata?.ToDictionary(p => p.Key, p => p.Value),
            };
            ndjson.Append(JsonSerializer.Serialize(dto, SerializerOptions)).Append('\n');
        }

        using var content = new StringContent(ndjson.ToString(), Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/upsert") { Content = content };
        await SendAsync(request, "upsert vectors", cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK, bool returnValues, bool returnMetadata, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/query")
        {
            Content = JsonContent.Create(new QueryRequest
            {
                Vector = vector.ToArray(),
                TopK = topK,
                ReturnValues = returnValues,
                ReturnMetadata = returnMetadata ? "all" : "none",
            }, options: SerializerOptions),
        };

        var result = await SendAsync<QueryResult>(request, "query vectors", cancellationToken).ConfigureAwait(false);
        return (result?.Matches ?? [])
            .Select(m => new VectorMatch(
                m.Id,
                m.Score,
                m.Values is { Length: > 0 } ? m.Values : ReadOnlyMemory<float>.Empty,
                ConvertMetadata(m.Metadata)))
            .ToList();
    }

    public async Task<IReadOnlyList<VectorRecord>> GetByIdsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/get_by_ids")
        {
            Content = JsonContent.Create(new IdsRequest { Ids = ids }, options: SerializerOptions),
        };

        var result = await SendAsync<List<VectorResponseDto>>(request, "get vectors by id", cancellationToken).ConfigureAwait(false);
        return (result ?? [])
            .Select(v => new VectorRecord(v.Id, v.Values ?? [], ConvertMetadata(v.Metadata)))
            .ToList();
    }

    public async Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_basePath}/delete_by_ids")
        {
            Content = JsonContent.Create(new IdsRequest { Ids = ids }, options: SerializerOptions),
        };

        await SendAsync(request, "delete vectors", cancellationToken).ConfigureAwait(false);
    }

    private async Task SendAsync(HttpRequestMessage request, string operation, CancellationToken cancellationToken)
        => await SendAsync<object>(request, operation, cancellationToken).ConfigureAwait(false);

    private async Task<T?> SendAsync<T>(HttpRequestMessage request, string operation, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(SerializerOptions, cancellationToken).ConfigureAwait(false);
        if (envelope is null || !envelope.Success)
        {
            var message = envelope?.Errors.FirstOrDefault()?.Message ?? $"HTTP {(int)response.StatusCode}";
            throw new InvalidOperationException($"Vectorize {operation} failed: {message}");
        }

        return envelope.Result;
    }

    private static IReadOnlyDictionary<string, object?>? ConvertMetadata(Dictionary<string, JsonElement>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return metadata.ToDictionary(p => p.Key, p => ToClrValue(p.Value));
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

    public void Dispose()
    {
        // The HttpClient is owned by IHttpClientFactory.
    }

    // Used to serialize upsert requests (metadata is a CLR dictionary).
    private sealed class VectorDto
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("values")]
        public float[]? Values { get; init; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object?>? Metadata { get; init; }
    }

    // Used to deserialize get_by_ids responses (metadata values arrive as JSON elements).
    private sealed class VectorResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("values")]
        public float[]? Values { get; init; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, JsonElement>? Metadata { get; init; }
    }

    private sealed class QueryRequest
    {
        [JsonPropertyName("vector")]
        public float[] Vector { get; init; } = [];

        [JsonPropertyName("topK")]
        public int TopK { get; init; }

        [JsonPropertyName("returnValues")]
        public bool ReturnValues { get; init; }

        [JsonPropertyName("returnMetadata")]
        public string ReturnMetadata { get; init; } = "all";
    }

    private sealed class IdsRequest
    {
        [JsonPropertyName("ids")]
        public IReadOnlyList<string> Ids { get; init; } = [];
    }

    private sealed class QueryResult
    {
        [JsonPropertyName("matches")]
        public List<MatchDto> Matches { get; init; } = [];
    }

    private sealed class MatchDto
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; init; }

        [JsonPropertyName("values")]
        public float[]? Values { get; init; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, JsonElement>? Metadata { get; init; }
    }

    private sealed class Envelope<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("errors")]
        public List<ErrorDto> Errors { get; init; } = [];

        [JsonPropertyName("result")]
        public T? Result { get; init; }
    }

    private sealed class ErrorDto
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
