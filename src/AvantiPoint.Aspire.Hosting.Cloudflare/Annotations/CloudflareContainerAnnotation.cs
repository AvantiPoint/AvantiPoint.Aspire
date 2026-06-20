namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Marks a project for deployment as a Cloudflare Container within an environment.</summary>
internal sealed class CloudflareContainerAnnotation(CloudflareEnvironmentResource environment) : ICloudflareTargetAnnotation
{
    public CloudflareEnvironmentResource Environment { get; } = environment;

    public required string WorkerName { get; init; }
    public required string ClassName { get; init; }
    public required string BindingName { get; init; }
    public required int Port { get; init; }
    public required int MaxInstances { get; init; }
    public string? InstanceType { get; init; }
    public required string SleepAfter { get; init; }
    public required string CompatibilityDate { get; init; }

    /// <summary>Directory the generated artifacts (Dockerfile, worker.js, wrangler.jsonc) are written to.</summary>
    public string? WorkingDirectory { get; set; }
}
