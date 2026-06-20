using System.Text.Json;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Publishing.Generators;

/// <summary>Generates the <c>wrangler.jsonc</c> that wires the Worker, the container, and its Durable Object binding.</summary>
internal static class WranglerContainerConfigGenerator
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

    public static string Generate(CloudflareContainerAnnotation annotation)
    {
        var container = new Dictionary<string, object?>
        {
            ["class_name"] = annotation.ClassName,
            ["image"] = "./Dockerfile",
            ["max_instances"] = annotation.MaxInstances,
        };

        if (!string.IsNullOrWhiteSpace(annotation.InstanceType))
        {
            container["instance_type"] = annotation.InstanceType;
        }

        var config = new Dictionary<string, object?>
        {
            ["name"] = annotation.WorkerName,
            ["main"] = "worker.js",
            ["compatibility_date"] = annotation.CompatibilityDate,
            ["containers"] = new object[] { container },
            ["durable_objects"] = new Dictionary<string, object?>
            {
                ["bindings"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["name"] = annotation.BindingName,
                        ["class_name"] = annotation.ClassName,
                    },
                },
            },
            ["migrations"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["tag"] = "v1",
                    ["new_sqlite_classes"] = new[] { annotation.ClassName },
                },
            },
        };

        return JsonSerializer.Serialize(config, s_options) + Environment.NewLine;
    }
}
