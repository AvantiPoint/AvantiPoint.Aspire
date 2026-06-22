using AvantiPoint.Aspire.Cloudflare.KV.Backends;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AvantiPoint.Aspire.Cloudflare.KV;

/// <summary>
/// Registers an <see cref="ICloudflareKVClient"/> from an Aspire-injected connection string. The same
/// client API works against an in-memory store (dev) and the Workers KV HTTP API (deployed).
/// </summary>
public static class KvClientExtensions
{
    private const string DefaultConfigSectionRoot = "Aspire:Cloudflare:KV";
    private const string HttpClientName = "AvantiPoint.Cloudflare.KV";

    /// <summary>Registers a singleton <see cref="ICloudflareKVClient"/> for the given connection name.</summary>
    public static void AddKvClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<KvClientSettings>? configureSettings = null)
        => builder.AddKvClientInternal(connectionName, serviceKey: null, configureSettings);

    /// <summary>Registers a keyed <see cref="ICloudflareKVClient"/> (keyed by <paramref name="name"/>).</summary>
    public static void AddKeyedKvClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<KvClientSettings>? configureSettings = null)
        => builder.AddKvClientInternal(name, serviceKey: name, configureSettings);

    private static void AddKvClientInternal(
        this IHostApplicationBuilder builder,
        string connectionName,
        string? serviceKey,
        Action<KvClientSettings>? configureSettings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new KvClientSettings();
        builder.Configuration.GetSection($"{DefaultConfigSectionRoot}:{connectionName}").Bind(settings);
        settings.ApplyConnectionString(builder.Configuration.GetConnectionString(connectionName));
        configureSettings?.Invoke(settings);

        if (!settings.IsLocal)
        {
            builder.Services.AddHttpClient(HttpClientName);
        }

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<ICloudflareKVClient>(sp => CreateClient(sp, settings, connectionName));
        }
        else
        {
            builder.Services.AddKeyedSingleton<ICloudflareKVClient>(serviceKey, (sp, _) => CreateClient(sp, settings, connectionName));
        }
    }

    private static CloudflareKVClient CreateClient(IServiceProvider serviceProvider, KvClientSettings settings, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.Namespace))
        {
            throw new InvalidOperationException(
                $"No KV namespace configured for connection '{connectionName}'. Ensure the AppHost references " +
                $"the KV namespace (e.g. WithReference).");
        }

        IKvBackend backend;
        if (settings.IsLocal)
        {
            backend = new InMemoryKvBackend();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.AccountId))
            {
                throw new InvalidOperationException(
                    $"Incomplete KV connection '{connectionName}'. Expected 'AccountId' (and a 'Token').");
            }

            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            backend = new HttpKvBackend(httpClient, settings.AccountId, settings.Namespace, settings.Token ?? string.Empty);
        }

        return new CloudflareKVClient(backend, settings.Namespace);
    }
}
