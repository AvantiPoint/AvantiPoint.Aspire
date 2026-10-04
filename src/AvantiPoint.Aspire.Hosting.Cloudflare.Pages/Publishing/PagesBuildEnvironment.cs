using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Publishing;

internal static class PagesBuildEnvironment
{
    public static async Task<IReadOnlyDictionary<string, string?>> ResolveAsync(
        IResource resource, ILogger logger, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        if (resource.TryGetEnvironmentVariables(out var callbacks))
        {
            var context = new EnvironmentCallbackContext(executionContext, resource, values, cancellationToken) { Logger = logger };
            foreach (var callback in callbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }
        }

        var resolved = new Dictionary<string, string?>();
        foreach (var (key, value) in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Aspire's JavaScript integration injects its dev-server listen port. A static build has no allocated endpoint.
            if (value is EndpointReferenceExpression { Property: EndpointProperty.TargetPort } listenPort
                && ReferenceEquals(listenPort.Endpoint.Resource, resource))
            {
                continue;
            }
            resolved[key] = await ResolveValueAsync(value).ConfigureAwait(false);
        }
        return resolved;

        async ValueTask<string?> ResolveValueAsync(object? value)
        {
            switch (value)
            {
                case EndpointReference endpoint:
                    return await ResolveEndpointAsync(endpoint.Property(EndpointProperty.Url)).ConfigureAwait(false);
                case EndpointReferenceExpression endpoint:
                    return await ResolveEndpointAsync(endpoint).ConfigureAwait(false);
                case ReferenceExpression expression:
                    if (expression.IsConditional)
                    {
                        var condition = await ResolveValueAsync(expression.Condition).ConfigureAwait(false);
                        return await ResolveValueAsync(string.Equals(condition, expression.MatchValue, StringComparison.OrdinalIgnoreCase)
                            ? expression.WhenTrue : expression.WhenFalse).ConfigureAwait(false);
                    }
                    var arguments = new object?[expression.ValueProviders.Count];
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        var resolvedValue = await ResolveValueAsync(expression.ValueProviders[i]).ConfigureAwait(false);
                        var formatted = new ReferenceExpressionBuilder();
                        formatted.AppendFormatted(resolvedValue, expression.StringFormats[i]);
                        arguments[i] = await formatted.Build().GetValueAsync(cancellationToken).ConfigureAwait(false);
                    }
                    return expression.Format.Length == 0 ? null : string.Format(CultureInfo.InvariantCulture, expression.Format, arguments);
                case IResourceBuilder<IResource> builder:
                    return await ResolveValueAsync(builder.Resource).ConfigureAwait(false);
                case IValueProvider provider:
                    return await provider.GetValueAsync(new ValueProviderContext { ExecutionContext = executionContext, Caller = resource }, cancellationToken).ConfigureAwait(false);
                default:
                    return value?.ToString();
            }
        }

        async ValueTask<string?> ResolveEndpointAsync(EndpointReferenceExpression endpoint)
        {
            var target = endpoint.Endpoint.Resource;
            var environment = target.Annotations.OfType<ICloudflareTargetAnnotation>().LastOrDefault()?.Environment;
            if (environment is null || !target.Annotations.OfType<CustomDomainAnnotation>().Any())
            {
                throw new InvalidOperationException($"Pages build endpoint '{target.Name}' requires a Cloudflare target with a custom domain. Use WithCustomDomain or supply a production URL explicitly.");
            }
#pragma warning disable ASPIRECOMPUTE002 // Cloudflare's existing compute-environment contract supplies production addresses.
            var expression = ((IComputeEnvironmentResource)environment).GetEndpointPropertyExpression(endpoint);
#pragma warning restore ASPIRECOMPUTE002
            return await ResolveValueAsync(expression).ConfigureAwait(false);
        }
    }
}
