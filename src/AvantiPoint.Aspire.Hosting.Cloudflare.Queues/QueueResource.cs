using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Queues;

/// <summary>
/// A Cloudflare Queue modeled as an Aspire resource. Exposes a connection string that targets the real
/// Queues REST API by default (in run and deploy), or an in-process in-memory queue during
/// <c>aspire run</c> when <see cref="QueuesHostingExtensions.RunAsEmulator"/> is used.
/// </summary>
public sealed class QueueResource : Resource, IResourceWithConnectionString, ICloudflareResource
{
    internal QueueResource(string name, CloudflareEnvironmentResource environment, string queueName)
        : base(name)
    {
        Environment = environment;
        QueueName = queueName;
    }

    /// <summary>The Cloudflare environment that owns this queue.</summary>
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The Queue name (may differ from the Aspire resource name).</summary>
    public string QueueName { get; }

    /// <summary>True when this queue is served by the in-memory emulator (opt-in via <c>RunAsEmulator()</c>).</summary>
    public bool UseEmulator { get; internal set; }

    /// <summary>When true, <c>aspire deploy --destroy</c> will delete the real queue. Off by default (data-loss guard).</summary>
    public bool AllowDestroy { get; internal set; }

    // Optional token override for runtime access; defaults to the environment API token.
    internal ParameterResource? AccessToken { get; set; }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    private ReferenceExpression BuildConnectionString()
    {
        if (UseEmulator)
        {
            return ReferenceExpression.Create($"Provider=Local;Queue={QueueName}");
        }

        var accountId = Environment.AccountId;
        var token = AccessToken ?? Environment.ApiToken;

        // The client resolves the queue id from the name via the Queues list API.
        return ReferenceExpression.Create($"Provider=Queues;AccountId={accountId};Queue={QueueName};Token={token}");
    }
}
