using System.Reflection;
using Aspire.Hosting;
using AvantiPoint.Aspire.Hosting.Cloudflare.AI;
using AvantiPoint.Aspire.Hosting.Cloudflare.D1;
using AvantiPoint.Aspire.Hosting.Cloudflare.Hyperdrive;
using AvantiPoint.Aspire.Hosting.Cloudflare.KV;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages;
using AvantiPoint.Aspire.Hosting.Cloudflare.Queues;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;
using AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize;
using AvantiPoint.Aspire.Hosting.Extensions;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Tests;

public class TypeScriptExportTests
{
    private static readonly Assembly[] Integrations =
    [
        typeof(CloudflareEnvironmentExtensions).Assembly,
        typeof(CloudflareAIExtensions).Assembly,
        typeof(D1HostingExtensions).Assembly,
        typeof(HyperdriveExtensions).Assembly,
        typeof(KvHostingExtensions).Assembly,
        typeof(CloudflarePagesExtensions).Assembly,
        typeof(QueuesHostingExtensions).Assembly,
        typeof(R2HostingExtensions).Assembly,
        typeof(VectorizeHostingExtensions).Assembly,
        typeof(DeploymentParameterExtensions).Assembly
    ];

    [Fact]
    public void EveryIntegration_ExportsUniqueCapabilitiesAndResourceTypes()
    {
        foreach (var assembly in Integrations)
        {
            var exports = assembly.GetExportedTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => m.GetCustomAttribute<AspireExportAttribute>() is not null).ToList();
            Assert.NotEmpty(exports);
            var ids = exports.Select(m => m.GetCustomAttribute<AspireExportAttribute>()!.Id ?? m.Name).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
            Assert.All(assembly.GetExportedTypes().Where(t => t.IsClass && t.Name.EndsWith("Resource", StringComparison.Ordinal)),
                t => Assert.NotNull(t.GetCustomAttribute<AspireExportAttribute>()));
        }

        Assert.All(new[] { typeof(CloudflareWorkerOptions), typeof(CloudflareContainerOptions), typeof(CloudflareAIOptions), typeof(CloudflarePagesOptions) },
            t => Assert.True(t.GetCustomAttribute<AspireExportAttribute>()!.ExposeProperties));
        Assert.NotNull(typeof(D1HostingExtensions).GetMethods().Single(m => m.Name == "AddD1Database" && m.GetParameters()[0].ParameterType == typeof(IDistributedApplicationBuilder)).GetCustomAttribute<AspireExportAttribute>());
        Assert.NotNull(typeof(R2HostingExtensions).GetMethods().Single(m => m.Name == "AddR2Bucket" && m.GetParameters()[0].ParameterType == typeof(IDistributedApplicationBuilder)).GetCustomAttribute<AspireExportAttribute>());
    }

    [Fact]
    public void SynchronousConfigurationCallbacks_AllowReentrantRemoteCalls()
    {
        var callbackMethods = Integrations.SelectMany(a => a.GetExportedTypes())
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.GetCustomAttribute<AspireExportAttribute>() is not null)
            .Where(m => m.GetParameters().Any(p => p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Action<>)))
            .ToList();

        Assert.NotEmpty(callbackMethods);
        Assert.All(callbackMethods, m => Assert.True(m.GetCustomAttribute<AspireExportAttribute>()!.RunSyncOnBackgroundThread, m.Name));
    }
}
