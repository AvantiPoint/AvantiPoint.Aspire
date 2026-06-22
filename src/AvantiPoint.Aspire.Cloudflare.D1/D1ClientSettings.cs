using System.Data.Common;

namespace AvantiPoint.Aspire.Cloudflare.D1;

/// <summary>
/// Settings for the Cloudflare D1 client integration. Values are typically supplied via the
/// Aspire-injected connection string — either <c>Provider=Sqlite;Data Source=...</c> (local dev) or
/// <c>Provider=D1;AccountId=...;Database=...;Token=...</c> (deployed) — but can be overridden in code.
/// </summary>
public sealed class D1ClientSettings
{
    /// <summary>The full connection string. When set, its components populate the other properties.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The backend provider: <c>Sqlite</c> (local) or <c>D1</c> (HTTP). Defaults to <c>D1</c>.</summary>
    public string Provider { get; set; } = "D1";

    /// <summary>The SQLite file path (when <see cref="Provider"/> is <c>Sqlite</c>).</summary>
    public string? DataSource { get; set; }

    /// <summary>The Cloudflare account id (when <see cref="Provider"/> is <c>D1</c>).</summary>
    public string? AccountId { get; set; }

    /// <summary>The D1 database name (when <see cref="Provider"/> is <c>D1</c>).</summary>
    public string? DatabaseName { get; set; }

    /// <summary>The Cloudflare API token used for D1 data access (when <see cref="Provider"/> is <c>D1</c>).</summary>
    public string? Token { get; set; }

    /// <summary>Disables the registered health check when <c>true</c>.</summary>
    public bool DisableHealthChecks { get; set; }

    /// <summary>True when the SQLite backend should be used.</summary>
    internal bool IsSqlite => string.Equals(Provider, "Sqlite", StringComparison.OrdinalIgnoreCase);

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

        if (TryGet(parsed, "Data Source", out var dataSource))
        {
            DataSource = dataSource;
        }

        if (TryGet(parsed, "AccountId", out var accountId))
        {
            AccountId = accountId;
        }

        if (TryGet(parsed, "Database", out var database))
        {
            DatabaseName = database;
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
