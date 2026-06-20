using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;

/// <summary>
/// Resolves the environment variables (connection strings, <c>WithEnvironment</c> values) that should be
/// injected into a Cloudflare Container, filtering out variables that are only meaningful for local runs.
/// </summary>
internal static class ContainerEnvironment
{
    /// <summary>Resolves the resource's environment variables to concrete values, filtered for container use.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> ResolveAsync(IResource resource, ILogger logger)
    {
        if (resource is not IResourceWithEnvironment environmentResource)
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var values = await environmentResource
                .GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Run)
                .ConfigureAwait(false);
            return Filter(values);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not resolve environment variables for '{Resource}'. Ensure deploy credentials " +
                "(CLOUDFLARE_ACCOUNT_ID, R2 keys, ...) are set. The container will start without injected configuration.",
                resource.Name);
            return new Dictionary<string, string>();
        }
    }

    /// <summary>Drops variables that point at local-run-only infrastructure (telemetry, service discovery, the local listen URL).</summary>
    public static IReadOnlyDictionary<string, string> Filter(IReadOnlyDictionary<string, string> values)
        => values
            .Where(kvp => !IsLocalOnly(kvp.Key))
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

    private static bool IsLocalOnly(string key)
        => key.StartsWith("OTEL_", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("services__", StringComparison.OrdinalIgnoreCase)
           || key.Equals("ASPNETCORE_URLS", StringComparison.OrdinalIgnoreCase);
}
