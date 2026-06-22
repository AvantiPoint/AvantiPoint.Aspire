using AvantiPoint.Aspire.Cloudflare.Queues.Backends;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AvantiPoint.Aspire.Cloudflare.Queues;

/// <summary>
/// Registers an <see cref="IQueueClient"/> from an Aspire-injected connection string. The same client API
/// works against an in-memory queue (dev) and the Queues HTTP API (deployed).
/// </summary>
public static class QueueClientExtensions
{
    private const string DefaultConfigSectionRoot = "Aspire:Cloudflare:Queues";
    private const string HttpClientName = "AvantiPoint.Cloudflare.Queues";

    /// <summary>Registers a singleton <see cref="IQueueClient"/> for the given connection name.</summary>
    public static void AddQueueClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<QueueClientSettings>? configureSettings = null)
        => builder.AddQueueClientInternal(connectionName, serviceKey: null, configureSettings);

    /// <summary>Registers a keyed <see cref="IQueueClient"/> (keyed by <paramref name="name"/>).</summary>
    public static void AddKeyedQueueClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<QueueClientSettings>? configureSettings = null)
        => builder.AddQueueClientInternal(name, serviceKey: name, configureSettings);

    private static void AddQueueClientInternal(
        this IHostApplicationBuilder builder,
        string connectionName,
        string? serviceKey,
        Action<QueueClientSettings>? configureSettings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new QueueClientSettings();
        builder.Configuration.GetSection($"{DefaultConfigSectionRoot}:{connectionName}").Bind(settings);
        settings.ApplyConnectionString(builder.Configuration.GetConnectionString(connectionName));
        configureSettings?.Invoke(settings);

        if (!settings.IsLocal)
        {
            builder.Services.AddHttpClient(HttpClientName);
        }

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<IQueueClient>(sp => CreateClient(sp, settings, connectionName));
        }
        else
        {
            builder.Services.AddKeyedSingleton<IQueueClient>(serviceKey, (sp, _) => CreateClient(sp, settings, connectionName));
        }
    }

    private static QueueClient CreateClient(IServiceProvider serviceProvider, QueueClientSettings settings, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.QueueName))
        {
            throw new InvalidOperationException(
                $"No Queue configured for connection '{connectionName}'. Ensure the AppHost references the Queue " +
                $"(e.g. WithReference).");
        }

        IQueueBackend backend;
        if (settings.IsLocal)
        {
            backend = new InMemoryQueueBackend();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.AccountId))
            {
                throw new InvalidOperationException(
                    $"Incomplete Queue connection '{connectionName}'. Expected 'AccountId' (and a 'Token').");
            }

            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            backend = new HttpQueueBackend(httpClient, settings.AccountId, settings.QueueName, settings.Token ?? string.Empty);
        }

        return new QueueClient(backend, settings.QueueName);
    }
}
