using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Extension methods for attaching custom domains to Cloudflare-deployed resources.</summary>
public static class CustomDomainExtensions
{
    /// <summary>
    /// Attaches a custom domain to this resource (a Cloudflare Container or Pages app), with the Zone Id
    /// supplied as an Aspire parameter (recommended — keeps the id out of source). Call after
    /// <c>PublishAsCloudflareContainer</c> / <c>PublishAsCloudflarePages</c>. Repeatable.
    /// </summary>
    /// <param name="builder">The resource builder (must already target a Cloudflare environment).</param>
    /// <param name="hostname">The fully-qualified hostname, e.g. <c>api.example.com</c>.</param>
    /// <param name="zoneId">A parameter resolving to the Cloudflare Zone ID that owns <paramref name="hostname"/>.</param>
    public static IResourceBuilder<T> WithCustomDomain<T>(
        this IResourceBuilder<T> builder,
        string hostname,
        IResourceBuilder<ParameterResource> zoneId)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(zoneId);
        RequireCustomDomainScopes(builder, hostname);
        builder.Resource.Annotations.Add(new CustomDomainAnnotation(hostname, zoneId.Resource));
        return builder;
    }

    /// <summary>
    /// Attaches a custom domain using a literal Zone ID. Prefer the parameter overload for real
    /// deployments so the id isn't hard-coded; this is handy for quick samples.
    /// </summary>
    public static IResourceBuilder<T> WithCustomDomain<T>(
        this IResourceBuilder<T> builder,
        string hostname,
        string zoneId)
        where T : IResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
        RequireCustomDomainScopes(builder, hostname);
        builder.Resource.Annotations.Add(new CustomDomainAnnotation(hostname, zoneId));
        return builder;
    }

    private static void RequireCustomDomainScopes<T>(IResourceBuilder<T> builder, string hostname)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);

        if (!builder.Resource.TryGetLastAnnotation<ICloudflareTargetAnnotation>(out var target))
        {
            throw new InvalidOperationException(
                $"Call PublishAsCloudflareContainer(...) or PublishAsCloudflarePages(...) on '{builder.Resource.Name}' " +
                "before WithCustomDomain, so the resource targets a Cloudflare environment.");
        }

        // Custom domains need DNS edit + zone read on the token.
        target.Environment.RequireScopes(CloudflareScopes.DnsRecordsEdit, CloudflareScopes.ZoneRead);
    }
}
