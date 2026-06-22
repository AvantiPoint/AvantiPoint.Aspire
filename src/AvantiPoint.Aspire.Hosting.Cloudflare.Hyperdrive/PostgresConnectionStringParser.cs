using System.Data.Common;
using System.Globalization;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive;

/// <summary>
/// Parses a PostgreSQL connection string — either URI form (<c>postgres://user:pass@host:port/db</c>) or
/// Npgsql key-value form (<c>Host=...;Port=...;Database=...;Username=...;Password=...</c>) — into the
/// Hyperdrive origin fields used when provisioning a configuration.
/// </summary>
internal static class PostgresConnectionStringParser
{
    public static HyperdriveOrigin Parse(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("The Hyperdrive production connection string is empty.", nameof(connectionString));
        }

        var trimmed = connectionString.Trim();
        return trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            ? ParseUri(trimmed)
            : ParseKeyValue(trimmed);
    }

    private static HyperdriveOrigin ParseUri(string connectionString)
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        return new HyperdriveOrigin
        {
            Scheme = "postgres",
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            User = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        };
    }

    private static HyperdriveOrigin ParseKeyValue(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        return new HyperdriveOrigin
        {
            Scheme = "postgres",
            Host = Get(builder, "Host", "Server"),
            Port = int.TryParse(Get(builder, "Port"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) ? port : 5432,
            Database = Get(builder, "Database", "Initial Catalog"),
            User = Get(builder, "Username", "User ID", "UserId", "User"),
            Password = Get(builder, "Password", "Pwd"),
        };
    }

    private static string Get(DbConnectionStringBuilder builder, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var value) && value is not null)
            {
                var s = value.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    return s;
                }
            }
        }

        return string.Empty;
    }
}
