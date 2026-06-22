using AvantiPoint.Aspire.Cloudflare.Vectorize.Backends;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AvantiPoint.Aspire.Cloudflare.Vectorize;

/// <summary>
/// Registers an <see cref="IVectorizeClient"/> from an Aspire-injected connection string. The same client
/// API works against an in-memory store (dev) and the Vectorize v2 HTTP API (deployed).
/// </summary>
public static class VectorizeClientExtensions
{
    private const string DefaultConfigSectionRoot = "Aspire:Cloudflare:Vectorize";
    private const string HttpClientName = "AvantiPoint.Cloudflare.Vectorize";

    /// <summary>Registers a singleton <see cref="IVectorizeClient"/> for the given connection name.</summary>
    public static void AddVectorizeClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<VectorizeClientSettings>? configureSettings = null)
        => builder.AddVectorizeClientInternal(connectionName, serviceKey: null, configureSettings);

    /// <summary>Registers a keyed <see cref="IVectorizeClient"/> (keyed by <paramref name="name"/>).</summary>
    public static void AddKeyedVectorizeClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<VectorizeClientSettings>? configureSettings = null)
        => builder.AddVectorizeClientInternal(name, serviceKey: name, configureSettings);

    private static void AddVectorizeClientInternal(
        this IHostApplicationBuilder builder,
        string connectionName,
        string? serviceKey,
        Action<VectorizeClientSettings>? configureSettings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new VectorizeClientSettings();
        builder.Configuration.GetSection($"{DefaultConfigSectionRoot}:{connectionName}").Bind(settings);
        settings.ApplyConnectionString(builder.Configuration.GetConnectionString(connectionName));
        configureSettings?.Invoke(settings);

        if (!settings.IsInMemory)
        {
            builder.Services.AddHttpClient(HttpClientName);
        }

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<IVectorizeClient>(sp => CreateClient(sp, settings, connectionName));
        }
        else
        {
            builder.Services.AddKeyedSingleton<IVectorizeClient>(serviceKey, (sp, _) => CreateClient(sp, settings, connectionName));
        }
    }

    private static VectorizeClient CreateClient(IServiceProvider serviceProvider, VectorizeClientSettings settings, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.IndexName))
        {
            throw new InvalidOperationException(
                $"No Vectorize index configured for connection '{connectionName}'. Ensure the AppHost references " +
                $"the Vectorize index (e.g. WithReference).");
        }

        IVectorizeBackend backend;
        if (settings.IsInMemory)
        {
            backend = new InMemoryVectorizeBackend(settings.Metric);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.AccountId))
            {
                throw new InvalidOperationException(
                    $"Incomplete Vectorize connection '{connectionName}'. Expected 'AccountId' (and a 'Token').");
            }

            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            backend = new HttpVectorizeBackend(httpClient, settings.AccountId, settings.IndexName, settings.Token ?? string.Empty);
        }

        return new VectorizeClient(backend, settings.IndexName, settings.Dimensions);
    }
}
