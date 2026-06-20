using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Extension methods for adding a Cloudflare deployment environment to an Aspire app.</summary>
public static class CloudflareEnvironmentExtensions
{
    /// <summary>Default environment-variable name the API token is read from.</summary>
    public const string ApiTokenEnvVar = "CLOUDFLARE_API_TOKEN";

    /// <summary>Default environment-variable name the account id is read from.</summary>
    public const string AccountIdEnvVar = "CLOUDFLARE_ACCOUNT_ID";

    /// <summary>
    /// Adds a Cloudflare deployment environment. Resources targeting this environment are
    /// provisioned/deployed to Cloudflare instead of the default (Azure) compute environment.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The environment resource name.</param>
    /// <param name="apiToken">
    /// Optional API token parameter. When omitted, a secret parameter is created that resolves from
    /// the <c>CLOUDFLARE_API_TOKEN</c> environment variable, then user-secrets/configuration.
    /// </param>
    /// <param name="accountId">
    /// Optional account id parameter. When omitted, a parameter is created that resolves from the
    /// <c>CLOUDFLARE_ACCOUNT_ID</c> environment variable, then user-secrets/configuration.
    /// </param>
    public static IResourceBuilder<CloudflareEnvironmentResource> AddCloudflareEnvironment(
        this IDistributedApplicationBuilder builder,
        string name = "cloudflare",
        IResourceBuilder<ParameterResource>? apiToken = null,
        IResourceBuilder<ParameterResource>? accountId = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddCloudflareHostingServices();

        var token = apiToken ?? builder.CreateDefaultParameter("cloudflare-api-token", ApiTokenEnvVar, secret: true);
        var account = accountId ?? builder.CreateDefaultParameter("cloudflare-account-id", AccountIdEnvVar, secret: false);

        var resource = new CloudflareEnvironmentResource(name, token.Resource, account.Resource);

        // Hijack the publish/deploy/destroy pipeline: contribute validate-token + publish + deploy +
        // destroy steps (ordered via WellKnownPipelineSteps) so resources go to Cloudflare, not Azure.
        resource.Annotations.Add(new PipelineStepAnnotation(_ => CloudflarePipelineSteps.CreateSteps(resource)));

        return builder.AddResource(resource);
    }

    /// <summary>
    /// Registers the shared services used by the Cloudflare hosting integration. Idempotent: safe to
    /// call from multiple <c>Add*</c> methods (core environment, R2 hosting, etc.).
    /// </summary>
    internal static void AddCloudflareHostingServices(this IDistributedApplicationBuilder builder)
    {
        if (builder.Services.Any(d => d.ServiceType == typeof(ICloudflareApiClient)))
        {
            return;
        }

        builder.Services.AddHttpClient<ICloudflareApiClient, CloudflareApiClient>(client =>
        {
            client.BaseAddress = CloudflareApiClient.BaseAddress;
        });

        builder.Services.TryAddSingleton<CloudflareTokenValidator>();
        builder.Services.TryAddSingleton<Cli.IWranglerCli, Cli.WranglerCli>();
    }

    private static IResourceBuilder<ParameterResource> CreateDefaultParameter(
        this IDistributedApplicationBuilder builder,
        string parameterName,
        string environmentVariable,
        bool secret)
    {
        // Reuse an existing default parameter so multiple Cloudflare environments share one token/account.
        var existing = builder.Resources.OfType<ParameterResource>()
            .FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return builder.CreateResourceBuilder(existing);
        }

        // Lazily resolved: only invoked if/when something actually needs the value (e.g. a real
        // R2 provisioning run or a deploy). Pure emulator runs never trigger this.
        return builder.AddParameter(parameterName, () =>
        {
            var fromEnv = Environment.GetEnvironmentVariable(environmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }

            var fromConfig = builder.Configuration[$"Parameters:{parameterName}"];
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig;
            }

            throw new InvalidOperationException(
                $"Cloudflare parameter '{parameterName}' is not set. Provide it via the {environmentVariable} " +
                $"environment variable, user-secrets, or configuration key 'Parameters:{parameterName}'.");
        }, secret: secret);
    }
}
