using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// Associates a custom domain (hostname + zone) with a deployable resource (Container or Pages).
/// The deploy pipeline configures DNS/SSL for it. Repeatable.
/// </summary>
internal sealed class CustomDomainAnnotation(string zoneId, string hostname) : IResourceAnnotation
{
    public string ZoneId { get; } = zoneId;
    public string Hostname { get; } = hostname;
}
