using System.Text;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Tests;

internal sealed class ServiceBuildValue : IValueProvider
{
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("This provider requires the AppHost services.");

    public ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<string?>(context.ExecutionContext!.Services.GetRequiredService<StringBuilder>().ToString());
}
