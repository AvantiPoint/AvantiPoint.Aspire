using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.AI.Publishing;

/// <summary>
/// Publish target for Cloudflare AI resources. When the resource routes through an AI Gateway, the
/// gateway is provisioned via the REST API during <c>aspire deploy</c> (idempotent). Workers AI itself
/// needs no provisioning, so for gateway-less resources the deploy phase is a no-op.
/// </summary>
internal sealed class AIGatewayPublishTarget(ILogger<AIGatewayPublishTarget> logger) : ICloudflarePublishTarget
{
    public bool CanHandle(IResource resource) => resource is CloudflareAIResource;

    public Task GenerateArtifactsAsync(CloudflarePublishContext context, IResource resource, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task DeployAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        var ai = (CloudflareAIResource)resource;
        if (string.IsNullOrEmpty(ai.Options.GatewayId))
        {
            return; // Workers AI needs no provisioning.
        }

        var apiClient = context.Services.GetRequiredService<ICloudflareApiClient>();
        logger.LogInformation("Provisioning AI Gateway '{Gateway}'...", ai.Options.GatewayId);
        await apiClient.CreateAIGatewayAsync(context.ApiToken, context.AccountId, new CreateAIGatewayRequest
        {
            Id = ai.Options.GatewayId,
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Provisioned AI Gateway '{Gateway}'.", ai.Options.GatewayId);
    }

    public Task DestroyAsync(CloudflareDeployContext context, IResource resource, CancellationToken cancellationToken)
    {
        // An AI Gateway can be shared across apps and holds logs/analytics — never auto-delete it on destroy.
        var ai = (CloudflareAIResource)resource;
        if (!string.IsNullOrEmpty(ai.Options.GatewayId))
        {
            logger.LogInformation("Leaving AI Gateway '{Gateway}' in place (gateways are not auto-deleted).", ai.Options.GatewayId);
        }

        return Task.CompletedTask;
    }
}
