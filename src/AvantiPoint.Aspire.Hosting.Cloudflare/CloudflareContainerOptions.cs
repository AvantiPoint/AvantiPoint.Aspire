namespace AvantiPoint.Aspire.Hosting.Cloudflare;

/// <summary>Options for deploying a .NET project as a Cloudflare Container.</summary>
public sealed class CloudflareContainerOptions
{
    /// <summary>The Worker name. Defaults to a sanitized form of the resource name.</summary>
    public string? WorkerName { get; set; }

    /// <summary>The port the container listens on. The generated app is configured to listen here. Default 8080.</summary>
    public int Port { get; set; } = 8080;

    /// <summary>Maximum number of container instances. Default 3.</summary>
    public int MaxInstances { get; set; } = 3;

    /// <summary>Optional Cloudflare container instance type (e.g. <c>dev</c>, <c>basic</c>, <c>standard</c>).</summary>
    public string? InstanceType { get; set; }

    /// <summary>How long an idle container instance stays alive before sleeping. Default <c>10m</c>.</summary>
    public string SleepAfter { get; set; } = "10m";

    /// <summary>The Workers <c>compatibility_date</c> for the generated Worker. Must be a past date.</summary>
    public string CompatibilityDate { get; set; } = "2025-06-01";
}
