using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AvantiPoint.Aspire.Cloudflare.Queues.Backends;

/// <summary>
/// Queue implementation that targets the Cloudflare Queues HTTP API. The queue id is resolved from the
/// queue name on first use (via the Queues list endpoint) and cached. Pull/ack use the HTTP pull-consumer
/// endpoints, which require the queue to have an HTTP pull consumer configured.
/// </summary>
internal sealed class HttpQueueBackend : IQueueBackend
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    private readonly string _queueName;
    private readonly SemaphoreSlim _idLock = new(1, 1);
    private string? _queueId;

    public HttpQueueBackend(HttpClient httpClient, string accountId, string queueName, string apiToken)
    {
        _httpClient = httpClient;
        _accountId = accountId;
        _queueName = queueName;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    }

    public async Task SendAsync(IReadOnlyList<string> bodies, CancellationToken cancellationToken)
    {
        var queueId = await ResolveQueueIdAsync(cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"accounts/{_accountId}/queues/{queueId}/messages/batch")
        {
            Content = JsonContent.Create(new BatchRequest
            {
                Messages = bodies.Select(b => new MessageDto { Body = b, ContentType = "text" }).ToList(),
            }),
        };

        await SendEnvelopeAsync(request, "send messages", cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<QueueMessage>> PullAsync(int batchSize, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        var queueId = await ResolveQueueIdAsync(cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"accounts/{_accountId}/queues/{queueId}/messages/pull")
        {
            Content = JsonContent.Create(new PullRequest
            {
                BatchSize = batchSize,
                VisibilityTimeoutMs = (int)visibilityTimeout.TotalMilliseconds,
            }),
        };

        var result = await SendEnvelopeAsync<PullResult>(request, "pull messages", cancellationToken).ConfigureAwait(false);
        return (result?.Messages ?? [])
            .Select(m => new QueueMessage(m.Id, m.LeaseId, m.Body, m.Attempts))
            .ToList();
    }

    public async Task AckAsync(IReadOnlyList<string> leaseIds, IReadOnlyList<string> retryLeaseIds, CancellationToken cancellationToken)
    {
        var queueId = await ResolveQueueIdAsync(cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"accounts/{_accountId}/queues/{queueId}/messages/ack")
        {
            Content = JsonContent.Create(new AckRequest
            {
                Acks = leaseIds.Select(l => new LeaseDto { LeaseId = l }).ToList(),
                Retries = retryLeaseIds.Select(l => new LeaseDto { LeaseId = l }).ToList(),
            }),
        };

        await SendEnvelopeAsync(request, "ack messages", cancellationToken).ConfigureAwait(false);
    }

    private Task SendEnvelopeAsync(HttpRequestMessage request, string operation, CancellationToken cancellationToken)
        => SendEnvelopeAsync<object>(request, operation, cancellationToken);

    private async Task<T?> SendEnvelopeAsync<T>(HttpRequestMessage request, string operation, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(cancellationToken).ConfigureAwait(false);
        if (envelope is null || !envelope.Success)
        {
            throw new InvalidOperationException($"Queues {operation} failed: {envelope?.Errors.FirstOrDefault()?.Message ?? response.StatusCode.ToString()}");
        }

        return envelope.Result;
    }

    private async Task<string> ResolveQueueIdAsync(CancellationToken cancellationToken)
    {
        if (_queueId is not null)
        {
            return _queueId;
        }

        await _idLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_queueId is not null)
            {
                return _queueId;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"accounts/{_accountId}/queues");
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var envelope = await response.Content.ReadFromJsonAsync<Envelope<List<QueueDto>>>(cancellationToken).ConfigureAwait(false);

            var match = envelope?.Result?.FirstOrDefault(q => string.Equals(q.QueueName, _queueName, StringComparison.Ordinal));
            _queueId = match?.QueueId
                ?? throw new InvalidOperationException(
                    $"Queue '{_queueName}' was not found in account '{_accountId}'. Has it been provisioned (aspire deploy)?");
            return _queueId;
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

    private sealed class MessageDto
    {
        [JsonPropertyName("body")]
        public string Body { get; init; } = string.Empty;

        [JsonPropertyName("content_type")]
        public string ContentType { get; init; } = "text";
    }

    private sealed class BatchRequest
    {
        [JsonPropertyName("messages")]
        public List<MessageDto> Messages { get; init; } = [];
    }

    private sealed class PullRequest
    {
        [JsonPropertyName("batch_size")]
        public int BatchSize { get; init; }

        [JsonPropertyName("visibility_timeout_ms")]
        public int VisibilityTimeoutMs { get; init; }
    }

    private sealed class PullResult
    {
        [JsonPropertyName("messages")]
        public List<PulledMessageDto> Messages { get; init; } = [];
    }

    private sealed class PulledMessageDto
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("lease_id")]
        public string LeaseId { get; init; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; init; } = string.Empty;

        [JsonPropertyName("attempts")]
        public int Attempts { get; init; }
    }

    private sealed class AckRequest
    {
        [JsonPropertyName("acks")]
        public List<LeaseDto> Acks { get; init; } = [];

        [JsonPropertyName("retries")]
        public List<LeaseDto> Retries { get; init; } = [];
    }

    private sealed class LeaseDto
    {
        [JsonPropertyName("lease_id")]
        public string LeaseId { get; init; } = string.Empty;
    }

    private sealed class QueueDto
    {
        [JsonPropertyName("queue_id")]
        public string QueueId { get; init; } = string.Empty;

        [JsonPropertyName("queue_name")]
        public string QueueName { get; init; } = string.Empty;
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
