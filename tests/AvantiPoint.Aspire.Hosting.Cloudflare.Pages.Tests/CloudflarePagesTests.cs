using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Pages.Tests;

public class CloudflarePagesTests
{
    private static (IDistributedApplicationBuilder Builder, IResourceBuilder<CloudflareEnvironmentResource> Env) Create()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        return (builder, builder.AddCloudflareEnvironment());
    }

    [Fact]
    public void PublishAsCloudflarePages_Adds_Pages_Annotation_With_Env()
    {
        var (builder, cf) = Create();
        var web = builder.AddViteApp("web", AppContext.BaseDirectory).PublishAsCloudflarePages(cf);

        var annotation = Assert.Single(web.Resource.Annotations.OfType<ICloudflareTargetAnnotation>());
        Assert.Same(cf.Resource, annotation.Environment);
    }

    [Fact]
    public void PublishAsCloudflarePages_Defaults_Project_And_Output()
    {
        var (builder, cf) = Create();
        var web = builder.AddViteApp("Frontend", AppContext.BaseDirectory).PublishAsCloudflarePages(cf);

        var annotation = Assert.Single(web.Resource.Annotations.OfType<CloudflarePagesAnnotation>());
        Assert.Equal("frontend", annotation.ProjectName);
        Assert.Equal("dist", annotation.OutputDirectory);
        Assert.Equal("main", annotation.Branch);
        Assert.Equal("npm run build", annotation.BuildCommand);
    }

    [Fact]
    public void PublishAsCloudflarePages_Honors_Options()
    {
        var (builder, cf) = Create();
        var web = builder.AddViteApp("web", AppContext.BaseDirectory)
            .PublishAsCloudflarePages(cf, o =>
            {
                o.ProjectName = "custom-site";
                o.OutputDirectory = "build";
                o.Branch = "production";
                o.BuildCommand = "pnpm build";
            });

        var annotation = Assert.Single(web.Resource.Annotations.OfType<CloudflarePagesAnnotation>());
        Assert.Equal("custom-site", annotation.ProjectName);
        Assert.Equal("build", annotation.OutputDirectory);
        Assert.Equal("production", annotation.Branch);
        Assert.Equal("pnpm build", annotation.BuildCommand);
    }

    [Fact]
    public void PublishAsCloudflarePages_Requires_Pages_Scope_And_Registers_Target()
    {
        var (builder, cf) = Create();
        builder.AddViteApp("web", AppContext.BaseDirectory).PublishAsCloudflarePages(cf);

        Assert.Contains(CloudflareScopes.PagesEdit, cf.Resource.RequiredScopes);
        Assert.Contains(builder.Services, d =>
            d.ServiceType == typeof(ICloudflarePublishTarget) &&
            d.ImplementationType == typeof(PagesPublishTarget));
    }

    [Theory]
    [InlineData("web", "web")]
    [InlineData("My.Web.App", "my-web-app")]
    [InlineData("Frontend_UI", "frontend-ui")]
    public void SanitizeProjectName_Produces_Valid_Pages_Names(string input, string expected)
        => Assert.Equal(expected, CloudflarePagesExtensions.SanitizeProjectName(input));

    [Theory]
    [InlineData("npm run build", "npm", new[] { "run", "build" })]
    [InlineData("pnpm build", "pnpm", new[] { "build" })]
    [InlineData("yarn", "yarn", new string[0])]
    public void SplitCommand_Splits_Executable_And_Args(string command, string file, string[] args)
    {
        var (actualFile, actualArgs) = PagesPublishTarget.SplitCommand(command);
        Assert.Equal(file, actualFile);
        Assert.Equal(args, actualArgs);
    }
}
