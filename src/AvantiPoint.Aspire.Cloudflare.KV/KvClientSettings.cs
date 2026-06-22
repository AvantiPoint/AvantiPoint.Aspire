using System.Data.Common;

namespace AvantiPoint.Aspire.Cloudflare.KV;

/// <summary>
/// Settings for the Cloudflare Workers KV client integration. Values are typically supplied via the
/// Aspire-injected connection string — either <c>Provider=Local;Namespace=...</c> (local dev) or
/// <c>Provider=KV;AccountId=...;Namespace=...;Token=...</c> (deployed) — but can be overridden in code.
/// </summary>
public sealed class KvClientSettings
{
    /// <summary>The full connection string. When set, its components populate the other properties.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The backend provider: <c>Local</c> (in-memory) or <c>KV</c> (HTTP). Defaults to <c>KV</c>.</summary>
    public string Provider { get; set; } = "KV";

    /// <summary>The Cloudflare account id (when <see cref="Provider"/> is <c>KV</c>).</summary>
    public string? AccountId { get; set; }

    /// <summary>The KV namespace title.</summary>
    public string? Namespace { get; set; }

    /// <summary>The Cloudflare API token used for KV access (when <see cref="Provider"/> is <c>KV</c>).</summary>
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

        if (TryGet(parsed, "Namespace", out var ns))
        {
            Namespace = ns;
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
