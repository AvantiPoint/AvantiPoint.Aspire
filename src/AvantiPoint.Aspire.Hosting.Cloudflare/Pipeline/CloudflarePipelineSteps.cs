using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pipeline;

/// <summary>
/// Builds the pipeline steps that hijack <c>aspire publish</c>/<c>aspire deploy</c> for a Cloudflare
/// environment. Exposed as a static factory (taking just the environment) so the step graph —
/// names, ordering and tags — is unit-testable without running the pipeline.
/// </summary>
internal static class CloudflarePipelineSteps
{
    public static string ValidateTokenStepName(CloudflareEnvironmentResource env) => $"cloudflare-validate-token-{env.Name}";
    public static string PublishStepName(CloudflareEnvironmentResource env) => $"cloudflare-publish-{env.Name}";
    public static string DeployStepName(CloudflareEnvironmentResource env) => $"cloudflare-deploy-{env.Name}";
    public static string DestroyStepName(CloudflareEnvironmentResource env) => $"cloudflare-destroy-{env.Name}";

    /// <summary>Creates the validate/publish/deploy/destroy steps for the environment.</summary>
    public static IEnumerable<PipelineStep> CreateSteps(CloudflareEnvironmentResource env)
    {
        // Token validation runs first and gates every phase (publish, deploy, destroy).
        var validate = new PipelineStep
        {
            Name = ValidateTokenStepName(env),
            Description = $"Validate Cloudflare API token for '{env.Name}'",
            Action = ctx => ValidateTokenAsync(env, ctx),
        };
        validate.DependsOn(WellKnownPipelineSteps.ProcessParameters);
        validate.RequiredBy(WellKnownPipelineSteps.PublishPrereq);
        validate.RequiredBy(WellKnownPipelineSteps.DeployPrereq);
        validate.RequiredBy(WellKnownPipelineSteps.DestroyPrereq);
        yield return validate;

        var publish = new PipelineStep
        {
            Name = PublishStepName(env),
            Description = $"Generate Cloudflare artifacts for '{env.Name}'",
            Action = ctx => PublishAsync(env, ctx),
        };
        publish.DependsOn(WellKnownPipelineSteps.PublishPrereq);
        publish.RequiredBy(WellKnownPipelineSteps.Publish);
        yield return publish;

        var deploy = new PipelineStep
        {
            Name = DeployStepName(env),
            Description = $"Deploy '{env.Name}' resources to Cloudflare",
            Action = ctx => DeployAsync(env, ctx),
        };
        deploy.DependsOn(WellKnownPipelineSteps.DeployPrereq);
        deploy.DependsOn(WellKnownPipelineSteps.Publish);
        deploy.RequiredBy(WellKnownPipelineSteps.Deploy);
        yield return deploy;

        var destroy = new PipelineStep
        {
            Name = DestroyStepName(env),
            Description = $"Destroy '{env.Name}' resources on Cloudflare",
            Action = ctx => DestroyAsync(env, ctx),
        };
        destroy.DependsOn(WellKnownPipelineSteps.DestroyPrereq);
        destroy.RequiredBy(WellKnownPipelineSteps.Destroy);
        yield return destroy;
    }

    private static IEnumerable<(IResource Resource, ICloudflarePublishTarget Target)> ResourcesFor(
        CloudflareEnvironmentResource env,
        PipelineStepContext ctx)
    {
        var targets = ctx.Services.GetServices<ICloudflarePublishTarget>().ToList();
        foreach (var resource in ctx.Model.Resources.Where(r => BelongsTo(r, env)))
        {
            var target = targets.FirstOrDefault(t => t.CanHandle(resource));
            if (target is not null)
            {
                yield return (resource, target);
            }
        }
    }

    // A resource belongs to an environment if it is one of ours (ICloudflareResource, e.g. R2) or an
    // existing Aspire resource we attached to the environment via annotation (e.g. a JS app for Pages).
    private static bool BelongsTo(IResource resource, CloudflareEnvironmentResource env)
    {
        if (resource is ICloudflareResource cloudflareResource && ReferenceEquals(cloudflareResource.Environment, env))
        {
            return true;
        }

        return resource.Annotations.OfType<ICloudflareTargetAnnotation>().Any(a => ReferenceEquals(a.Environment, env));
    }

    private static async Task ValidateTokenAsync(CloudflareEnvironmentResource env, PipelineStepContext ctx)
    {
        var (token, _) = await ResolveCredentialsAsync(env, ctx).ConfigureAwait(false);
        var validator = ctx.Services.GetRequiredService<CloudflareTokenValidator>();
        await validator.ValidateAsync(token, env.RequiredScopes, ctx.CancellationToken).ConfigureAwait(false);
    }

    private static async Task PublishAsync(CloudflareEnvironmentResource env, PipelineStepContext ctx)
    {
        var publishContext = new CloudflarePublishContext { Step = ctx, Environment = env };
        foreach (var (resource, target) in ResourcesFor(env, ctx))
        {
            await target.GenerateArtifactsAsync(publishContext, resource, ctx.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DeployAsync(CloudflareEnvironmentResource env, PipelineStepContext ctx)
    {
        var (token, account) = await ResolveCredentialsAsync(env, ctx).ConfigureAwait(false);
        var deployContext = new CloudflareDeployContext { Step = ctx, Environment = env, ApiToken = token, AccountId = account };
        foreach (var (resource, target) in ResourcesFor(env, ctx))
        {
            await target.DeployAsync(deployContext, resource, ctx.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DestroyAsync(CloudflareEnvironmentResource env, PipelineStepContext ctx)
    {
        var (token, account) = await ResolveCredentialsAsync(env, ctx).ConfigureAwait(false);
        var deployContext = new CloudflareDeployContext { Step = ctx, Environment = env, ApiToken = token, AccountId = account };
        foreach (var (resource, target) in ResourcesFor(env, ctx))
        {
            await target.DestroyAsync(deployContext, resource, ctx.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<(string Token, string AccountId)> ResolveCredentialsAsync(
        CloudflareEnvironmentResource env,
        PipelineStepContext ctx)
    {
        var token = await env.ApiToken.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false) ?? string.Empty;
        var account = await env.AccountId.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false) ?? string.Empty;
        return (token, account);
    }
}
