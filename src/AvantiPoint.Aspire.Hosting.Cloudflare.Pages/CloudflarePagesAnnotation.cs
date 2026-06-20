using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages;

/// <summary>Marks a JavaScript app for deployment to Cloudflare Pages within an environment.</summary>
internal sealed class CloudflarePagesAnnotation(CloudflareEnvironmentResource environment) : ICloudflareTargetAnnotation
{
    public CloudflareEnvironmentResource Environment { get; } = environment;

    public required string ProjectName { get; init; }
    public required string OutputDirectory { get; init; }
    public required string Branch { get; init; }
    public required string BuildCommand { get; init; }
    public bool SkipBuild { get; init; }

    /// <summary>The app's working directory (where the build runs and the output dir is rooted).</summary>
    public string? WorkingDirectory { get; init; }
}
