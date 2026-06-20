using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace AvantiPoint.Aspire.Cloudflare.R2;

/// <summary>
/// Registers an <see cref="IAmazonS3"/> client configured for Cloudflare R2 from an Aspire-injected
/// connection string. The same configuration works against the local MinIO emulator and real R2.
/// </summary>
public static class R2ClientExtensions
{
    private const string DefaultConfigSectionRoot = "Aspire:Cloudflare:R2";

    /// <summary>Registers a singleton R2-backed <see cref="IAmazonS3"/> for the given connection name.</summary>
    public static void AddR2Client(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<R2ClientSettings>? configureSettings = null)
        => builder.AddR2ClientInternal(connectionName, serviceKey: null, configureSettings);

    /// <summary>Registers a keyed R2-backed <see cref="IAmazonS3"/> (keyed by <paramref name="name"/>).</summary>
    public static void AddKeyedR2Client(
        this IHostApplicationBuilder builder,
        string name,
        Action<R2ClientSettings>? configureSettings = null)
        => builder.AddR2ClientInternal(name, serviceKey: name, configureSettings);

    private static void AddR2ClientInternal(
        this IHostApplicationBuilder builder,
        string connectionName,
        string? serviceKey,
        Action<R2ClientSettings>? configureSettings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new R2ClientSettings();
        builder.Configuration.GetSection($"{DefaultConfigSectionRoot}:{connectionName}").Bind(settings);
        settings.ApplyConnectionString(builder.Configuration.GetConnectionString(connectionName));
        configureSettings?.Invoke(settings);

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException(
                $"No R2 endpoint configured for connection '{connectionName}'. Ensure the AppHost references the " +
                $"R2 bucket (e.g. WithReference) or set '{DefaultConfigSectionRoot}:{connectionName}:Endpoint'.");
        }

        var client = CreateClient(settings);

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<IAmazonS3>(client);
            builder.Services.AddSingleton(settings);
        }
        else
        {
            builder.Services.AddKeyedSingleton<IAmazonS3>(serviceKey, client);
            builder.Services.AddKeyedSingleton(serviceKey, settings);
        }

        if (!settings.DisableHealthChecks)
        {
            var healthCheckName = serviceKey is null ? "R2" : $"R2_{serviceKey}";
            builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
                healthCheckName,
                _ => new R2HealthCheck(client, settings.BucketName),
                failureStatus: null,
                tags: ["r2", "cloudflare"]));
        }
    }

    private static AmazonS3Client CreateClient(R2ClientSettings settings)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = settings.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = settings.Region,
            // R2 rejects the AWS SDK v4 default flexible checksums; only send them when required.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
        return new AmazonS3Client(credentials, config);
    }
}
