using Aspire.Hosting;
namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages;

/// <summary>Options for deploying a JavaScript app to Cloudflare Pages.</summary>
[AspireExport(ExposeProperties = true)]
public sealed class CloudflarePagesOptions
{
    /// <summary>The Cloudflare Pages project name. Defaults to a sanitized form of the resource name.</summary>
    public string? ProjectName { get; set; }

    /// <summary>The build output directory, relative to the app's working directory. Defaults to <c>dist</c>.</summary>
    public string OutputDirectory { get; set; } = "dist";

    /// <summary>The Pages branch to deploy to (controls production vs. preview). Defaults to <c>main</c>.</summary>
    public string Branch { get; set; } = "main";

    /// <summary>The command run to produce the build output during publish. Defaults to <c>npm run build</c>.</summary>
    public string BuildCommand { get; set; } = "npm run build";

    /// <summary>When <c>true</c>, the build is not run during publish (the output directory is assumed to exist).</summary>
    public bool SkipBuild { get; set; }
}
