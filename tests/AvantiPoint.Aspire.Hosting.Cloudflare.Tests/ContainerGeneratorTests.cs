using System.Text.Json;
using Aspire.Hosting;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;
using AvantiPoint.Aspire.Hosting.Cloudflare.Publishing.Generators;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class ContainerGeneratorTests
{
    [Theory]
    [InlineData("api", "api")]
    [InlineData("My.Api", "my-api")]
    [InlineData("Orders_Service", "orders-service")]
    public void WorkerName_Is_Cloudflare_Valid(string input, string expected)
        => Assert.Equal(expected, CloudflareNaming.WorkerName(input));

    [Theory]
    [InlineData("api", "ApiContainer")]
    [InlineData("my-api", "MyApiContainer")]
    [InlineData("orders.service", "OrdersServiceContainer")]
    public void ClassName_Is_Valid_Identifier(string input, string expected)
        => Assert.Equal(expected, CloudflareNaming.ClassName(input));

    [Fact]
    public void BindingName_Is_Upper_Snake()
        => Assert.Equal("MY_API_CONTAINER", CloudflareNaming.BindingName("MyApiContainer"));

    [Fact]
    public void Dockerfile_Targets_Amd64_And_Runs_The_Assembly()
    {
        var dockerfile = DockerfileGenerator.Generate("CloudflarePlayground.Api", 8080);

        Assert.Contains("--platform=linux/amd64", dockerfile);
        Assert.Contains("mcr.microsoft.com/dotnet/aspnet:10.0", dockerfile);
        Assert.Contains("ASPNETCORE_URLS=http://+:8080", dockerfile);
        Assert.Contains("EXPOSE 8080", dockerfile);
        Assert.Contains("""ENTRYPOINT ["dotnet", "CloudflarePlayground.Api.dll"]""", dockerfile);
    }

    [Fact]
    public void Worker_Extends_Container_And_Forwards()
    {
        var worker = WorkerShimGenerator.GenerateWorker("ApiContainer", "API_CONTAINER", 8080, "10m");

        Assert.Contains("""import { Container, getContainer } from "@cloudflare/containers";""", worker);
        Assert.Contains("export class ApiContainer extends Container", worker);
        Assert.Contains("defaultPort = 8080;", worker);
        Assert.Contains("""sleepAfter = "10m";""", worker);
        Assert.Contains("getContainer(env.API_CONTAINER).fetch(request)", worker);
    }

    [Fact]
    public void PackageJson_References_Containers_Package()
        => Assert.Contains("@cloudflare/containers", WorkerShimGenerator.GeneratePackageJson("api"));

    [Fact]
    public void Wrangler_Config_Is_Valid_Json_With_Container_Wiring()
    {
        var annotation = NewAnnotation(instanceType: null);
        var json = WranglerContainerConfigGenerator.Generate(annotation);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("api", root.GetProperty("name").GetString());
        Assert.Equal("worker.js", root.GetProperty("main").GetString());

        var container = root.GetProperty("containers")[0];
        Assert.Equal("ApiContainer", container.GetProperty("class_name").GetString());
        Assert.Equal("./Dockerfile", container.GetProperty("image").GetString());
        Assert.Equal(3, container.GetProperty("max_instances").GetInt32());
        Assert.False(container.TryGetProperty("instance_type", out _));

        var binding = root.GetProperty("durable_objects").GetProperty("bindings")[0];
        Assert.Equal("API_CONTAINER", binding.GetProperty("name").GetString());
        Assert.Equal("ApiContainer", binding.GetProperty("class_name").GetString());

        var migration = root.GetProperty("migrations")[0];
        Assert.Equal("v1", migration.GetProperty("tag").GetString());
        Assert.Equal("ApiContainer", migration.GetProperty("new_sqlite_classes")[0].GetString());
    }

    [Fact]
    public void Wrangler_Config_Includes_Instance_Type_When_Set()
    {
        var json = WranglerContainerConfigGenerator.Generate(NewAnnotation(instanceType: "standard"));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("standard", doc.RootElement.GetProperty("containers")[0].GetProperty("instance_type").GetString());
    }

    private static CloudflareContainerAnnotation NewAnnotation(string? instanceType)
    {
        var builder = DistributedApplication.CreateBuilder(Array.Empty<string>());
        var env = builder.AddCloudflareEnvironment().Resource;
        return new CloudflareContainerAnnotation(env)
        {
            WorkerName = "api",
            ClassName = "ApiContainer",
            BindingName = "API_CONTAINER",
            Port = 8080,
            MaxInstances = 3,
            InstanceType = instanceType,
            SleepAfter = "10m",
            CompatibilityDate = "2025-06-01",
        };
    }
}
