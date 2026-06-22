using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using AvantiPoint.Aspire.Hosting.Extensions;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Extensions.Tests;

public class DeploymentParameterTests
{
    private sealed class ConstantDefault(string value) : ParameterDefault
    {
        public override string GetDefaultValue() => value;
        public override void WriteToManifest(ManifestPublishingContext context) { }
    }

    [Fact]
    public async Task RunMode_Resolves_To_Empty_And_Is_Not_Required()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        Assert.True(builder.ExecutionContext.IsRunMode);

        var zone = builder.AddDeploymentParameter("zone-id");

        // No configuration supplied — in dev this must not block, resolving to empty.
        var value = await zone.Resource.GetValueAsync(TestContext.Current.CancellationToken);
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public async Task RunMode_Uses_Default_Value_When_Provided()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        var zone = builder.AddDeploymentParameter("zone-id", new ConstantDefault("dev-zone"));

        var value = await zone.Resource.GetValueAsync(TestContext.Current.CancellationToken);
        Assert.Equal("dev-zone", value);
    }

    [Fact]
    public void RunMode_Honors_Secret_Flag()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        var token = builder.AddDeploymentParameter("api-token", secret: true);

        Assert.True(token.Resource.Secret);
    }

    [Fact]
    public void PublishMode_Behaves_Like_A_Normal_Required_Parameter()
    {
        var builder = DistributedApplication.CreateBuilder(["--operation", "publish", "--output-path", "."]);
        Assert.True(builder.ExecutionContext.IsPublishMode);

        var zone = builder.AddDeploymentParameter("zone-id");

        // In publish mode it is a real parameter — its manifest expression references the value
        // (it is not stubbed to empty as in run mode).
        Assert.Equal("{zone-id.value}", zone.Resource.ValueExpression);
    }
}
