using AvantiPoint.Aspire.Cloudflare.Queues.Backends;

namespace AvantiPoint.Aspire.Cloudflare.Queues;

/// <summary>
/// Default <see cref="IQueueClient"/>. Delegates to a backend (in-memory locally, the Queues HTTP API when
/// deployed) selected from the connection string.
/// </summary>
internal sealed class QueueClient(IQueueBackend backend, string queueName) : IQueueClient, IDisposable
{
    private static readonly TimeSpan DefaultVisibilityTimeout = TimeSpan.FromSeconds(30);

    public string QueueName { get; } = queueName;

    public Task SendAsync(string body, CancellationToken cancellationToken = default)
        => backend.SendAsync([body], cancellationToken);

    public Task SendBatchAsync(IEnumerable<string> bodies, CancellationToken cancellationToken = default)
        => backend.SendAsync(bodies as IReadOnlyList<string> ?? bodies.ToList(), cancellationToken);

    public Task<IReadOnlyList<QueueMessage>> PullAsync(int batchSize = 10, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        => backend.PullAsync(batchSize, visibilityTimeout ?? DefaultVisibilityTimeout, cancellationToken);

    public Task AckAsync(IEnumerable<string> leaseIds, CancellationToken cancellationToken = default)
        => backend.AckAsync(leaseIds as IReadOnlyList<string> ?? leaseIds.ToList(), [], cancellationToken);

    public Task RetryAsync(IEnumerable<string> leaseIds, CancellationToken cancellationToken = default)
        => backend.AckAsync([], leaseIds as IReadOnlyList<string> ?? leaseIds.ToList(), cancellationToken);

    public void Dispose() => backend.Dispose();
}
