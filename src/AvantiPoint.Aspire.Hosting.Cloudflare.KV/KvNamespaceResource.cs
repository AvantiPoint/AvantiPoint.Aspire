using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.KV;

/// <summary>
/// A Cloudflare Workers KV namespace modeled as an Aspire resource. Exposes a connection string that
/// targets the real KV REST API by default (in run and deploy), or an in-process in-memory store during
/// <c>aspire run</c> when <see cref="KvHostingExtensions.RunAsEmulator"/> is used.
/// </summary>
[AspireExport]
public sealed class KvNamespaceResource : Resource, IResourceWithConnectionString, ICloudflareResource
{
    internal KvNamespaceResource(string name, CloudflareEnvironmentResource environment, string title)
        : base(name)
    {
        Environment = environment;
        Title = title;
    }

    /// <summary>The Cloudflare environment that owns this namespace.</summary>
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The KV namespace title (may differ from the Aspire resource name).</summary>
    public string Title { get; }

    /// <summary>True when this namespace is served by the in-memory emulator (opt-in via <c>RunAsEmulator()</c>).</summary>
    public bool UseEmulator { get; internal set; }

    /// <summary>When true, <c>aspire destroy</c> will delete the real namespace. Off by default (data-loss guard).</summary>
    public bool AllowDestroy { get; internal set; }

    // Optional token override for runtime access; defaults to the environment API token.
    internal ParameterResource? AccessToken { get; set; }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    private ReferenceExpression BuildConnectionString()
    {
        if (UseEmulator)
        {
            return ReferenceExpression.Create($"Provider=Local;Namespace={Title}");
        }

        var accountId = Environment.AccountId;
        var token = AccessToken ?? Environment.ApiToken;

        // The client resolves the namespace id from the title via the KV list API.
        return ReferenceExpression.Create($"Provider=KV;AccountId={accountId};Namespace={Title};Token={token}");
    }
}
