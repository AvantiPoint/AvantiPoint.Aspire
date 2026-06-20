using Aspire.Hosting;
using Aspire.Hosting.Pipelines;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pipeline;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class CloudflarePipelineTests
{
    [Fact]
    public void AddCloudflareEnvironment_Attaches_PipelineStepAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());

        var cf = builder.AddCloudflareEnvironment();

        Assert.Contains(cf.Resource.Annotations, a => a is PipelineStepAnnotation);
    }

    [Fact]
    public void CreateSteps_Produces_Validate_Publish_Deploy_Destroy()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var env = builder.AddCloudflareEnvironment("cf").Resource;

        var steps = CloudflarePipelineSteps.CreateSteps(env).ToList();

        Assert.Equal(4, steps.Count);
        Assert.Contains(steps, s => s.Name == CloudflarePipelineSteps.ValidateTokenStepName(env));
        Assert.Contains(steps, s => s.Name == CloudflarePipelineSteps.PublishStepName(env));
        Assert.Contains(steps, s => s.Name == CloudflarePipelineSteps.DeployStepName(env));
        Assert.Contains(steps, s => s.Name == CloudflarePipelineSteps.DestroyStepName(env));
    }

    [Fact]
    public void ValidateToken_Gates_All_Phase_Prereqs()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var env = builder.AddCloudflareEnvironment("cf").Resource;

        var validate = CloudflarePipelineSteps.CreateSteps(env)
            .Single(s => s.Name == CloudflarePipelineSteps.ValidateTokenStepName(env));

        Assert.Contains(WellKnownPipelineSteps.ProcessParameters, validate.DependsOnSteps);
        Assert.Contains(WellKnownPipelineSteps.PublishPrereq, validate.RequiredBySteps);
        Assert.Contains(WellKnownPipelineSteps.DeployPrereq, validate.RequiredBySteps);
        Assert.Contains(WellKnownPipelineSteps.DestroyPrereq, validate.RequiredBySteps);
    }

    [Fact]
    public void Deploy_Step_Runs_In_Deploy_Phase_After_Publish()
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var env = builder.AddCloudflareEnvironment("cf").Resource;

        var deploy = CloudflarePipelineSteps.CreateSteps(env)
            .Single(s => s.Name == CloudflarePipelineSteps.DeployStepName(env));

        Assert.Contains(WellKnownPipelineSteps.DeployPrereq, deploy.DependsOnSteps);
        Assert.Contains(WellKnownPipelineSteps.Publish, deploy.DependsOnSteps);
        Assert.Contains(WellKnownPipelineSteps.Deploy, deploy.RequiredBySteps);
    }
}
