using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// Associates a custom domain (hostname + zone) with a deployable resource (Container or Pages).
/// The deploy pipeline configures DNS/SSL for it. The zone id may be a literal or an Aspire parameter
/// (so it doesn't have to be hard-coded). Repeatable.
/// </summary>
internal sealed class CustomDomainAnnotation : IResourceAnnotation
{
    private readonly string? _literalZoneId;
    private readonly ParameterResource? _zoneIdParameter;

    public CustomDomainAnnotation(string hostname, string zoneId)
    {
        Hostname = hostname;
        _literalZoneId = zoneId;
    }

    public CustomDomainAnnotation(string hostname, ParameterResource zoneId)
    {
        Hostname = hostname;
        _zoneIdParameter = zoneId;
    }

    public string Hostname { get; }

    /// <summary>Resolves the zone id (from the parameter at deploy time, or the literal value).</summary>
    public async ValueTask<string> GetZoneIdAsync(CancellationToken cancellationToken)
        => _zoneIdParameter is not null
            ? await _zoneIdParameter.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty
            : _literalZoneId ?? string.Empty;
}
