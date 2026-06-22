using System.Data.Common;
using System.Globalization;

namespace AvantiPoint.Aspire.Cloudflare.Vectorize;

/// <summary>
/// Settings for the Cloudflare Vectorize client integration. Values are typically supplied via the
/// Aspire-injected connection string — either <c>Provider=InMemory;Index=...;Dimensions=...;Metric=...</c>
/// (local dev) or <c>Provider=Vectorize;AccountId=...;Index=...;Dimensions=...;Metric=...;Token=...</c>
/// (deployed) — but can be overridden in code.
/// </summary>
public sealed class VectorizeClientSettings
{
    /// <summary>The full connection string. When set, its components populate the other properties.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The backend provider: <c>InMemory</c> (local) or <c>Vectorize</c> (HTTP). Defaults to <c>Vectorize</c>.</summary>
    public string Provider { get; set; } = "Vectorize";

    /// <summary>The Cloudflare account id (when <see cref="Provider"/> is <c>Vectorize</c>).</summary>
    public string? AccountId { get; set; }

    /// <summary>The Vectorize index name.</summary>
    public string? IndexName { get; set; }

    /// <summary>The vector dimensionality.</summary>
    public int Dimensions { get; set; }

    /// <summary>The distance metric (<c>cosine</c>, <c>euclidean</c>, or <c>dot-product</c>).</summary>
    public string Metric { get; set; } = "cosine";

    /// <summary>The Cloudflare API token used for Vectorize access (when <see cref="Provider"/> is <c>Vectorize</c>).</summary>
    public string? Token { get; set; }

    /// <summary>True when the in-memory backend should be used.</summary>
    internal bool IsInMemory => string.Equals(Provider, "InMemory", StringComparison.OrdinalIgnoreCase);

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

        if (TryGet(parsed, "Index", out var index))
        {
            IndexName = index;
        }

        if (TryGet(parsed, "Dimensions", out var dimensions) && int.TryParse(dimensions, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
        {
            Dimensions = d;
        }

        if (TryGet(parsed, "Metric", out var metric))
        {
            Metric = metric;
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
