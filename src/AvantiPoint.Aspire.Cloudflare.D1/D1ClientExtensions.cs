using AvantiPoint.Aspire.Cloudflare.D1.Backends;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace AvantiPoint.Aspire.Cloudflare.D1;

/// <summary>
/// Registers an <see cref="ID1Client"/> from an Aspire-injected connection string. The same client API
/// works against a local SQLite file (dev) and the D1 HTTP query API (deployed).
/// </summary>
public static class D1ClientExtensions
{
    private const string DefaultConfigSectionRoot = "Aspire:Cloudflare:D1";
    private const string HttpClientName = "AvantiPoint.Cloudflare.D1";

    /// <summary>Registers a singleton <see cref="ID1Client"/> for the given connection name.</summary>
    public static void AddD1Client(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<D1ClientSettings>? configureSettings = null)
        => builder.AddD1ClientInternal(connectionName, serviceKey: null, configureSettings);

    /// <summary>Registers a keyed <see cref="ID1Client"/> (keyed by <paramref name="name"/>).</summary>
    public static void AddKeyedD1Client(
        this IHostApplicationBuilder builder,
        string name,
        Action<D1ClientSettings>? configureSettings = null)
        => builder.AddD1ClientInternal(name, serviceKey: name, configureSettings);

    private static void AddD1ClientInternal(
        this IHostApplicationBuilder builder,
        string connectionName,
        string? serviceKey,
        Action<D1ClientSettings>? configureSettings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        var settings = new D1ClientSettings();
        builder.Configuration.GetSection($"{DefaultConfigSectionRoot}:{connectionName}").Bind(settings);
        settings.ApplyConnectionString(builder.Configuration.GetConnectionString(connectionName));
        configureSettings?.Invoke(settings);

        if (!settings.IsSqlite)
        {
            builder.Services.AddHttpClient(HttpClientName);
        }

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<ID1Client>(sp => CreateClient(sp, settings, connectionName));
        }
        else
        {
            builder.Services.AddKeyedSingleton<ID1Client>(serviceKey, (sp, _) => CreateClient(sp, settings, connectionName));
        }

        if (!settings.DisableHealthChecks)
        {
            var healthCheckName = serviceKey is null ? "D1" : $"D1_{serviceKey}";
            builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
                healthCheckName,
                sp =>
                {
                    var client = serviceKey is null
                        ? sp.GetRequiredService<ID1Client>()
                        : sp.GetRequiredKeyedService<ID1Client>(serviceKey);
                    return new D1HealthCheck(client);
                },
                failureStatus: null,
                tags: ["d1", "cloudflare"]));
        }
    }

    private static D1Client CreateClient(IServiceProvider serviceProvider, D1ClientSettings settings, string connectionName)
    {
        ID1Backend backend;
        if (settings.IsSqlite)
        {
            if (string.IsNullOrWhiteSpace(settings.DataSource))
            {
                throw new InvalidOperationException(
                    $"No SQLite 'Data Source' configured for D1 connection '{connectionName}'. Ensure the AppHost " +
                    $"references the D1 database (e.g. WithReference) and used .RunAsEmulator().");
            }

            backend = new SqliteD1Backend(settings.DataSource);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(settings.AccountId) || string.IsNullOrWhiteSpace(settings.DatabaseName))
            {
                throw new InvalidOperationException(
                    $"Incomplete D1 connection '{connectionName}'. Expected 'AccountId' and 'Database' (and a 'Token'). " +
                    $"Ensure the AppHost references the D1 database (e.g. WithReference).");
            }

            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            backend = new HttpD1Backend(httpClient, settings.AccountId, settings.DatabaseName, settings.Token ?? string.Empty);
        }

        return new D1Client(backend);
    }
}
