using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Extension methods for attaching custom domains to Cloudflare-deployed resources.</summary>
public static class CustomDomainExtensions
{
    /// <summary>
    /// Attaches a custom domain to this resource (a Cloudflare Container or Pages app). During deploy,
    /// Cloudflare DNS/SSL is configured for <paramref name="hostname"/> in the given zone. Repeatable.
    /// Call after <c>PublishAsCloudflareContainer</c> / <c>PublishAsCloudflarePages</c>.
    /// </summary>
    /// <param name="builder">The resource builder (must already target a Cloudflare environment).</param>
    /// <param name="zoneId">The Cloudflare Zone ID that owns <paramref name="hostname"/>.</param>
    /// <param name="hostname">The fully-qualified hostname, e.g. <c>api.example.com</c>.</param>
    public static IResourceBuilder<T> WithCustomDomain<T>(
        this IResourceBuilder<T> builder,
        string zoneId,
        string hostname)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);

        if (!builder.Resource.TryGetLastAnnotation<ICloudflareTargetAnnotation>(out var target))
        {
            throw new InvalidOperationException(
                $"Call PublishAsCloudflareContainer(...) or PublishAsCloudflarePages(...) on '{builder.Resource.Name}' " +
                "before WithCustomDomain, so the resource targets a Cloudflare environment.");
        }

        // Custom domains need DNS edit + zone read on the token.
        target.Environment.RequireScopes(CloudflareScopes.DnsRecordsEdit, CloudflareScopes.ZoneRead);

        builder.Resource.Annotations.Add(new CustomDomainAnnotation(zoneId, hostname));
        return builder;
    }
}
