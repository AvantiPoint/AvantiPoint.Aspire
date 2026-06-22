namespace AvantiPoint.Aspire.Cloudflare.Queues;

/// <summary>A message pulled from a Queue.</summary>
/// <param name="Id">The message id.</param>
/// <param name="LeaseId">The lease id used to acknowledge or retry the message.</param>
/// <param name="Body">The message body.</param>
/// <param name="Attempts">How many times this message has been delivered.</param>
public sealed record QueueMessage(string Id, string LeaseId, string Body, int Attempts);

/// <summary>
/// A client bound to a single Cloudflare Queue. The same API runs against an in-memory queue during
/// development and the Queues HTTP API (push + pull consumer) once deployed — the backend is chosen from
/// the Aspire-injected connection string, so consuming code is identical in both environments.
/// </summary>
public interface IQueueClient
{
    /// <summary>The Queue name this client is bound to.</summary>
    string QueueName { get; }

    /// <summary>Sends a single message.</summary>
    Task SendAsync(string body, CancellationToken cancellationToken = default);

    /// <summary>Sends a batch of messages.</summary>
    Task SendBatchAsync(IEnumerable<string> bodies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls up to <paramref name="batchSize"/> messages, leasing them for <paramref name="visibilityTimeout"/>
    /// (default 30s). Leased messages are hidden from other pulls until acknowledged, retried, or the lease
    /// expires. Requires the queue to have an HTTP pull consumer configured when running against real Queues.
    /// </summary>
    Task<IReadOnlyList<QueueMessage>> PullAsync(int batchSize = 10, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);

    /// <summary>Acknowledges (removes) the messages with the given lease ids.</summary>
    Task AckAsync(IEnumerable<string> leaseIds, CancellationToken cancellationToken = default);

    /// <summary>Marks the messages with the given lease ids for retry (returns them to the queue).</summary>
    Task RetryAsync(IEnumerable<string> leaseIds, CancellationToken cancellationToken = default);
}
