namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>
/// Human-readable Cloudflare API token permission scopes required by the integration,
/// keyed by the Cloudflare permission-group names shown when minting a token.
/// The environment aggregates only the scopes actually needed by the resources in the
/// application model, and validates the supplied token has them before provisioning.
/// </summary>
public static class CloudflareScopes
{
    /// <summary>Always required: lets the integration call <c>GET /user/tokens/verify</c>.</summary>
    public const string UserDetailsRead = "User Details Read";

    /// <summary>Required to create/manage R2 buckets.</summary>
    public const string WorkersR2StorageEdit = "Workers R2 Storage Write";

    /// <summary>Required to deploy Workers / Containers.</summary>
    public const string WorkersScriptsEdit = "Workers Scripts Write";

    /// <summary>Required to deploy Cloudflare Pages projects.</summary>
    public const string PagesEdit = "Cloudflare Pages Write";

    /// <summary>Required to create/modify DNS records for custom domains.</summary>
    public const string DnsRecordsEdit = "DNS Write";

    /// <summary>Required (read) to resolve a zone for custom domains.</summary>
    public const string ZoneRead = "Zone Read";

    /// <summary>Required to create/manage and query D1 databases.</summary>
    public const string D1Edit = "D1 Write";

    /// <summary>Required to call Workers AI (chat/embeddings).</summary>
    public const string WorkersAIRead = "Workers AI Read";

    /// <summary>Required to create/manage AI Gateways.</summary>
    public const string AIGatewayEdit = "AI Gateway Write";

    /// <summary>Required to create/manage and query Vectorize indexes.</summary>
    public const string VectorizeEdit = "Vectorize Write";
}
