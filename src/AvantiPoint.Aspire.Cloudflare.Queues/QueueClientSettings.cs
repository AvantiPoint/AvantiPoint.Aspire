using System.Data.Common;

namespace AvantiPoint.Aspire.Cloudflare.Queues;

/// <summary>
/// Settings for the Cloudflare Queues client integration. Values are typically supplied via the
/// Aspire-injected connection string — either <c>Provider=Local;Queue=...</c> (local dev) or
/// <c>Provider=Queues;AccountId=...;Queue=...;Token=...</c> (deployed) — but can be overridden in code.
/// </summary>
public sealed class QueueClientSettings
{
    /// <summary>The full connection string. When set, its components populate the other properties.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The backend provider: <c>Local</c> (in-memory) or <c>Queues</c> (HTTP). Defaults to <c>Queues</c>.</summary>
    public string Provider { get; set; } = "Queues";

    /// <summary>The Cloudflare account id (when <see cref="Provider"/> is <c>Queues</c>).</summary>
    public string? AccountId { get; set; }

    /// <summary>The Queue name.</summary>
    public string? QueueName { get; set; }

    /// <summary>The Cloudflare API token used for Queue access (when <see cref="Provider"/> is <c>Queues</c>).</summary>
    public string? Token { get; set; }

    /// <summary>True when the in-memory backend should be used.</summary>
    internal bool IsLocal => string.Equals(Provider, "Local", StringComparison.OrdinalIgnoreCase);

    internal void ApplyConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        ConnectionString = connectionString;

        var parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };

        if (TryGet(parsed, "Provider", out var provider))
        {
            Provider = provider;
        }

        if (TryGet(parsed, "AccountId", out var accountId))
        {
            AccountId = accountId;
        }

        if (TryGet(parsed, "Queue", out var queue))
        {
            QueueName = queue;
        }

        if (TryGet(parsed, "Token", out var token))
        {
            Token = token;
        }
    }

    private static bool TryGet(DbConnectionStringBuilder builder, string key, out string value)
    {
        if (builder.TryGetValue(key, out var raw) && raw is not null)
        {
            value = raw.ToString() ?? string.Empty;
            return !string.IsNullOrEmpty(value);
        }

        value = string.Empty;
        return false;
    }
}
