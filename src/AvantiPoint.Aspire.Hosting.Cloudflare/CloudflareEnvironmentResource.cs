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

    // IComputeEnvironmentResource — exercised once compute resources (Containers) target this
    // environment (milestone M4). Until then there are no compute targets to resolve.
    ReferenceExpression IComputeEnvironmentResource.GetHostAddressExpression(EndpointReference endpointReference)
        => throw new NotSupportedException(
            "Cloudflare compute (Container) endpoints are not yet supported. This arrives with container support.");

    ReferenceExpression IComputeEnvironmentResource.GetEndpointPropertyExpression(EndpointReferenceExpression endpointReferenceExpression)
        => throw new NotSupportedException(
            "Cloudflare compute (Container) endpoints are not yet supported. This arrives with container support.");
}
