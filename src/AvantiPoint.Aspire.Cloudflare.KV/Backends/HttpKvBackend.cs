using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AvantiPoint.Aspire.Cloudflare.KV.Backends;

/// <summary>
/// KV implementation that targets the Cloudflare Workers KV HTTP API. The namespace id is resolved from
/// the namespace title on first use (via the KV list endpoint) and cached.
/// </summary>
internal sealed class HttpKvBackend : IKvBackend
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    private readonly string _title;
    private readonly SemaphoreSlim _idLock = new(1, 1);
    private string? _namespaceId;

    public HttpKvBackend(HttpClient httpClient, string accountId, string title, string apiToken)
    {
        _httpClient = httpClient;
        _accountId = accountId;
        _title = title;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var nsId = await ResolveNamespaceIdAsync(cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, ValuePath(nsId, key));
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        // The KV value endpoint returns the raw value body, not the standard JSON envelope.
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PutAsync(string key, string value, TimeSpan? expirationTtl, CancellationToken cancellationToken)
    {
        var nsId = await ResolveNamespaceIdAsync(cancellationToken).ConfigureAwait(false);
        var path = ValuePath(nsId, key);
        if (expirationTtl is { } ttl)
        {
            path += $"?expiration_ttl={(int)ttl.TotalSeconds}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = new StringContent(value),
        };
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "put KV value", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var nsId = await ResolveNamespaceIdAsync(cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Delete, ValuePath(nsId, key));
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await EnsureSuccessAsync(response, "delete KV value", cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string? prefix, CancellationToken cancellationToken)
    {
        var nsId = await ResolveNamespaceIdAsync(cancellationToken).ConfigureAwait(false);
        var keys = new List<string>();
        string? cursor = null;

        // KV /keys is paginated (default 1000 per page); follow the cursor until it is empty so callers
        // get every matching key, not just the first page.
        do
        {
            var path = $"accounts/{_accountId}/storage/kv/namespaces/{nsId}/keys";
            var query = new List<string>();
            if (!string.IsNullOrEmpty(prefix))
            {
                query.Add($"prefix={Uri.EscapeDataString(prefix)}");
            }

            if (!string.IsNullOrEmpty(cursor))
            {
                query.Add($"cursor={Uri.EscapeDataString(cursor)}");
            }

            if (query.Count > 0)
            {
                path += "?" + string.Join('&', query);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var envelope = await response.Content.ReadFromJsonAsync<KeysEnvelope>(cancellationToken).ConfigureAwait(false);
            if (envelope is null || !envelope.Success)
            {
                throw new InvalidOperationException($"KV list keys failed: {envelope?.Errors.FirstOrDefault()?.Message ?? response.StatusCode.ToString()}");
            }

            keys.AddRange(envelope.Result.Select(k => k.Name));
            cursor = envelope.ResultInfo?.Cursor;
        }
        while (!string.IsNullOrEmpty(cursor));

        return keys;
    }

    private string ValuePath(string namespaceId, string key)
        => $"accounts/{_accountId}/storage/kv/namespaces/{namespaceId}/values/{Uri.EscapeDataString(key)}";

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<object>>(cancellationToken).ConfigureAwait(false);
        if (envelope is null || !envelope.Success)
        {
            throw new InvalidOperationException($"KV {operation} failed: {envelope?.Errors.FirstOrDefault()?.Message ?? response.StatusCode.ToString()}");
        }
    }

    private async Task<string> ResolveNamespaceIdAsync(CancellationToken cancellationToken)
    {
        if (_namespaceId is not null)
        {
            return _namespaceId;
        }

        await _idLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_namespaceId is not null)
            {
                return _namespaceId;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"accounts/{_accountId}/storage/kv/namespaces?per_page=100");
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var envelope = await response.Content.ReadFromJsonAsync<Envelope<List<NamespaceDto>>>(cancellationToken).ConfigureAwait(false);

            var match = envelope?.Result?.FirstOrDefault(n => string.Equals(n.Title, _title, StringComparison.Ordinal));
            _namespaceId = match?.Id
                ?? throw new InvalidOperationException(
                    $"KV namespace '{_title}' was not found in account '{_accountId}'. Has it been provisioned (aspire deploy)?");
            return _namespaceId;
        }
        finally
        {
            _idLock.Release();
        }
    }

    public void Dispose()
    {
        _idLock.Dispose();
    }

    private sealed class NamespaceDto
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;
    }

    private sealed class KeyDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }

    private sealed class KeysEnvelope
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("errors")]
        public List<ErrorDto> Errors { get; init; } = [];

        [JsonPropertyName("result")]
        public List<KeyDto> Result { get; init; } = [];

        [JsonPropertyName("result_info")]
        public ResultInfoDto? ResultInfo { get; init; }
    }

    private sealed class ResultInfoDto
    {
        [JsonPropertyName("cursor")]
        public string? Cursor { get; init; }
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
