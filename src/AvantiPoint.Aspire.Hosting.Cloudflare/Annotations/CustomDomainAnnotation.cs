using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// Associates a custom domain (hostname + zone) with a deployable resource (Container or Pages). Both the
/// hostname and the zone id may be a literal or an Aspire parameter, so neither needs to be hard-coded.
/// The deploy pipeline configures DNS/SSL for it. Repeatable.
/// </summary>
internal sealed class CustomDomainAnnotation : IResourceAnnotation
{
    // Each value is either a string (literal) or a ParameterResource (resolved at deploy time).
    private readonly object _hostname;
    private readonly object _zoneId;

    public CustomDomainAnnotation(string hostname, string zoneId)
    {
        _hostname = hostname;
        _zoneId = zoneId;
    }

    public CustomDomainAnnotation(string hostname, ParameterResource zoneId)
    {
        _hostname = hostname;
        _zoneId = zoneId;
    }

    public CustomDomainAnnotation(ParameterResource hostname, ParameterResource zoneId)
    {
        _hostname = hostname;
        _zoneId = zoneId;
    }

    public CustomDomainAnnotation(ParameterResource hostname, string zoneId)
    {
        _hostname = hostname;
        _zoneId = zoneId;
    }

    /// <summary>The hostname as a reference expression (resolves the parameter, or the literal).</summary>
    public ReferenceExpression HostnameExpression => ToExpression(_hostname);

    /// <summary>Resolves the hostname (from the parameter at deploy time, or the literal value).</summary>
    public ValueTask<string> GetHostnameAsync(CancellationToken cancellationToken) => ResolveAsync(_hostname, cancellationToken);

    /// <summary>Resolves the zone id (from the parameter at deploy time, or the literal value).</summary>
    public ValueTask<string> GetZoneIdAsync(CancellationToken cancellationToken) => ResolveAsync(_zoneId, cancellationToken);

    private static ReferenceExpression ToExpression(object value)
        => value is ParameterResource parameter
            ? ReferenceExpression.Create($"{parameter}")
            : ReferenceExpression.Create($"{(string)value}");

    private static async ValueTask<string> ResolveAsync(object value, CancellationToken cancellationToken)
        => value is ParameterResource parameter
            ? await parameter.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty
            : (string)value;
}
