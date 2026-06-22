namespace AvantiPoint.Aspire.Cloudflare.Queues.Backends;

/// <summary>
/// The backend behind an <see cref="IQueueClient"/>. Two implementations exist — in-memory (dev) and the
/// Queues HTTP API (deployed) — selected from the connection string.
/// </summary>
internal interface IQueueBackend : IDisposable
{
    Task SendAsync(IReadOnlyList<string> bodies, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueMessage>> PullAsync(int batchSize, TimeSpan visibilityTimeout, CancellationToken cancellationToken);

    Task AckAsync(IReadOnlyList<string> leaseIds, IReadOnlyList<string> retryLeaseIds, CancellationToken cancellationToken);
}
