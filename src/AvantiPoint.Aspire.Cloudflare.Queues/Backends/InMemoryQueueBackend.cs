namespace AvantiPoint.Aspire.Cloudflare.Queues.Backends;

/// <summary>
/// In-memory queue used for the local dev loop (and tests). Models lease/visibility semantics: pulled
/// messages are hidden until acknowledged, retried, or their lease expires.
/// </summary>
internal sealed class InMemoryQueueBackend : IQueueBackend
{
    private readonly object _gate = new();
    private readonly List<Message> _messages = [];
    private long _sequence;

    public Task SendAsync(IReadOnlyList<string> bodies, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            foreach (var body in bodies)
            {
                var id = $"msg-{++_sequence}";
                _messages.Add(new Message { Id = id, Body = body });
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<QueueMessage>> PullAsync(int batchSize, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var pulled = new List<QueueMessage>();

        lock (_gate)
        {
            foreach (var message in _messages)
            {
                if (pulled.Count >= batchSize)
                {
                    break;
                }

                var visible = message.LeasedUntil is null || message.LeasedUntil <= now;
                if (!visible)
                {
                    continue;
                }

                message.Attempts++;
                message.LeaseId = $"lease-{Guid.NewGuid():N}";
                message.LeasedUntil = now.Add(visibilityTimeout);
                pulled.Add(new QueueMessage(message.Id, message.LeaseId, message.Body, message.Attempts));
            }
        }

        return Task.FromResult<IReadOnlyList<QueueMessage>>(pulled);
    }

    public Task AckAsync(IReadOnlyList<string> leaseIds, IReadOnlyList<string> retryLeaseIds, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (leaseIds.Count > 0)
            {
                var acked = new HashSet<string>(leaseIds, StringComparer.Ordinal);
                _messages.RemoveAll(m => m.LeaseId is not null && acked.Contains(m.LeaseId));
            }

            if (retryLeaseIds.Count > 0)
            {
                var retried = new HashSet<string>(retryLeaseIds, StringComparer.Ordinal);
                foreach (var message in _messages.Where(m => m.LeaseId is not null && retried.Contains(m.LeaseId)))
                {
                    message.LeaseId = null;
                    message.LeasedUntil = null; // make visible again immediately
                }
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }

    private sealed class Message
    {
        public string Id { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public int Attempts { get; set; }
        public string? LeaseId { get; set; }
        public DateTimeOffset? LeasedUntil { get; set; }
    }
}
