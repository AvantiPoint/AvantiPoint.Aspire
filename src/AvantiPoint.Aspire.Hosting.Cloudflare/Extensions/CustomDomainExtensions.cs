using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Extension methods for attaching custom domains to Cloudflare-deployed resources.</summary>
public static class CustomDomainExtensions
{
    /// <summary>
    /// Attaches a custom domain to this resource (a Cloudflare Container or Pages app), with both the
    /// hostname and Zone Id supplied as Aspire parameters (recommended — keeps deploy-specific config out
    /// of source). Pair with <c>AddDeploymentParameter</c> so they're only required at deploy time. Call
    /// after <c>PublishAsCloudflareContainer</c> / <c>PublishAsCloudflarePages</c>. Repeatable.
    /// </summary>
    public static IResourceBuilder<T> WithCustomDomain<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ParameterResource> hostname,
        IResourceBuilder<ParameterResource> zoneId)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(hostname);
        ArgumentNullException.ThrowIfNull(zoneId);
        RequireCustomDomainScopes(builder);
        builder.Resource.Annotations.Add(new CustomDomainAnnotation(hostname.Resource, zoneId.Resource));
        return builder;
    }

    /// <summary>Attaches a custom domain with a parameter hostname and a literal Zone Id.</summary>
    public static IResourceBuilder<T> WithCustomDomain<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ParameterResource> hostname,
        string zoneId)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(hostname);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
        RequireCustomDomainScopes(builder);
        builder.Resource.Annotations.Add(new CustomDomainAnnotation(hostname.Resource, zoneId));
        return builder;
    }

    /// <summary>Attaches a custom domain with a literal hostname and a parameter Zone Id.</summary>
    public static IResourceBuilder<T> WithCustomDomain<T>(
        this IResourceBuilder<T> builder,
        string hostname,
        IResourceBuilder<ParameterResource> zoneId)
        where T : IResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        ArgumentNullException.ThrowIfNull(zoneId);
        RequireCustomDomainScopes(builder);
        builder.Resource.Annotations.Add(new CustomDomainAnnotation(hostname, zoneId.Resource));
        return builder;
    }

    /// <summary>
    /// Attaches a custom domain using literal values. Prefer the parameter overloads for real deployments
    /// so hostnames/zone ids aren't hard-coded; this is handy for quick samples.
    /// </summary>
    public static IResourceBuilder<T> WithCustomDomain<T>(
        this IResourceBuilder<T> builder,
        string hostname,
        string zoneId)
        where T : IResource
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
        RequireCustomDomainScopes(builder);
        builder.Resource.Annotations.Add(new CustomDomainAnnotation(hostname, zoneId));
        return builder;
    }

    private static void RequireCustomDomainScopes<T>(IResourceBuilder<T> builder)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);

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
