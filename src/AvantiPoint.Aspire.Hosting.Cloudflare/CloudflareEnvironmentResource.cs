using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// Represents the Cloudflare account/environment that resources are provisioned into and
/// deployed to. Acts as the Aspire compute environment so that <c>aspire publish</c>/<c>aspire deploy</c>
/// target Cloudflare instead of the default (Azure). Holds the API token and account id used for
/// all Cloudflare operations, and aggregates the permission scopes the application actually needs.
/// </summary>
public sealed class CloudflareEnvironmentResource : Resource, IComputeEnvironmentResource
{
    private readonly HashSet<string> _requiredScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        CloudflareScopes.UserDetailsRead,
    };

    public CloudflareEnvironmentResource(string name, ParameterResource apiToken, ParameterResource accountId)
        : base(name)
    {
        ApiToken = apiToken;
        AccountId = accountId;
    }

    /// <summary>The Cloudflare API token (a secret parameter) used to authenticate all operations.</summary>
    public ParameterResource ApiToken { get; }

    /// <summary>The Cloudflare account id that owns the provisioned resources.</summary>
    public ParameterResource AccountId { get; }

    /// <summary>The permission scopes required by the resources targeting this environment.</summary>
    public IReadOnlyCollection<string> RequiredScopes => _requiredScopes;

    /// <summary>Declares that an operation against this environment needs the given permission scopes.</summary>
    public void RequireScopes(params string[] scopes)
    {
        foreach (var scope in scopes)
        {
            _requiredScopes.Add(scope);
        }
    }

    // IComputeEnvironmentResource — resolves the public address of a compute resource (Container)
    // deployed to this environment, so other resources can reference it (e.g. a Pages app calling the API).
    ReferenceExpression IComputeEnvironmentResource.GetHostAddressExpression(EndpointReference endpointReference)
        => ReferenceExpression.Create($"{HostFor(endpointReference.Resource)}");

    ReferenceExpression IComputeEnvironmentResource.GetEndpointPropertyExpression(EndpointReferenceExpression endpointReferenceExpression)
    {
        var host = HostFor(endpointReferenceExpression.Endpoint.Resource);
        return endpointReferenceExpression.Property switch
        {
            EndpointProperty.Port or EndpointProperty.TargetPort => ReferenceExpression.Create($"443"),
            EndpointProperty.Scheme => ReferenceExpression.Create($"https"),
            EndpointProperty.TlsEnabled => ReferenceExpression.Create($"true"),
            EndpointProperty.Host or EndpointProperty.IPV4Host => ReferenceExpression.Create($"{host}"),
            EndpointProperty.HostAndPort => ReferenceExpression.Create($"{host}:443"),
            _ => ReferenceExpression.Create($"https://{host}"),
        };
    }

    // The public hostname for a deployed resource: its custom domain if one is configured, otherwise a
    // workers.dev placeholder. (Accurate workers.dev URLs require the account subdomain; custom domains
    // give a deterministic address — see WithCustomDomain.)
    private static string HostFor(IResource resource)
    {
        var customDomain = resource.Annotations.OfType<CustomDomainAnnotation>().FirstOrDefault();
        if (customDomain is not null)
        {
            return customDomain.Hostname;
        }

        var container = resource.Annotations.OfType<CloudflareContainerAnnotation>().FirstOrDefault();
        return container is not null ? $"{container.WorkerName}.workers.dev" : $"{resource.Name}.workers.dev";
    }
}
