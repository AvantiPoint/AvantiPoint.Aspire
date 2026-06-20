using System.Data.Common;

namespace AvantiPoint.Aspire.Cloudflare.R2;

/// <summary>
/// Settings for the Cloudflare R2 client integration. Values are typically supplied via the
/// Aspire-injected connection string (<c>Endpoint=...;AccessKey=...;SecretKey=...;Bucket=...;Region=auto</c>)
/// but can be overridden in code.
/// </summary>
public sealed class R2ClientSettings
{
    /// <summary>The full connection string. When set, its components populate the other properties.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>The S3-compatible endpoint, e.g. <c>https://&lt;account&gt;.r2.cloudflarestorage.com</c> or the local emulator URL.</summary>
    public string? Endpoint { get; set; }

    /// <summary>The R2 / S3 access key id.</summary>
    public string? AccessKey { get; set; }

    /// <summary>The R2 / S3 secret access key.</summary>
    public string? SecretKey { get; set; }

    /// <summary>The bucket name.</summary>
    public string? BucketName { get; set; }

    /// <summary>The S3 region; R2 uses <c>auto</c>.</summary>
    public string Region { get; set; } = "auto";

    /// <summary>Disables the registered health check when <c>true</c>.</summary>
    public bool DisableHealthChecks { get; set; }

    internal void ApplyConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        ConnectionString = connectionString;

        var parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };

        if (TryGet(parsed, "Endpoint", out var endpoint))
        {
            Endpoint = endpoint;
        }

        if (TryGet(parsed, "AccessKey", out var accessKey))
        {
            AccessKey = accessKey;
        }

        if (TryGet(parsed, "SecretKey", out var secretKey))
        {
            SecretKey = secretKey;
        }

        if (TryGet(parsed, "Bucket", out var bucket))
        {
            BucketName = bucket;
        }

        if (TryGet(parsed, "Region", out var region))
        {
            Region = region;
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
