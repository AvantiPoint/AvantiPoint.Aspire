# Cloudflare Containers

Deploy a .NET project as a [Cloudflare Container](https://developers.cloudflare.com/containers/) — fronted by a Worker and a Durable Object, with the image built and pushed by `wrangler`.

## Deploy a project as a container

```csharp
var cloudflare = builder.AddCloudflareEnvironment();

builder.AddProject<Projects.Api>("api")
    .PublishAsCloudflareContainer(cloudflare);
```

- **`aspire run`** → the project runs locally as a normal Aspire project.
- **`aspire deploy`** → the project is published, a Dockerfile + Worker + `wrangler.jsonc` are generated, and `wrangler deploy` builds the image (linux/amd64), pushes it to Cloudflare's registry, and deploys the Worker.

## What gets generated

| File | Purpose |
| --- | --- |
| `Dockerfile` | Runs the published app on `mcr.microsoft.com/dotnet/aspnet:10.0` (linux/amd64), listening on the configured port. |
| `worker.js` | A Durable Object class extending the `@cloudflare/containers` `Container`, plus a fetch handler that forwards requests to a container instance. |
| `wrangler.jsonc` | Wires the `containers`, `durable_objects` binding, and `migrations`. |

## Options

```csharp
.PublishAsCloudflareContainer(cloudflare, options =>
{
    options.WorkerName = "orders-api";   // defaults to a sanitized resource name
    options.Port = 8080;                 // the app is configured to listen here
    options.MaxInstances = 5;
    options.InstanceType = "standard";   // dev | basic | standard (optional)
    options.SleepAfter = "10m";          // idle timeout before an instance sleeps
});
```

## Requirements

- **Docker** running locally (wrangler builds the image).
- **wrangler** installed — see [How wrangler authenticates](../getting-started/wrangler.md).
- API token with *Workers Scripts: Edit* — see [API Tokens](../getting-started/api-tokens.md).

## Using R2 and other references

`WithReference` works as usual; the connection details are made available to the project:

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(uploads)               // an R2 bucket
    .PublishAsCloudflareContainer(cloudflare);
```

## Custom domains

```csharp
builder.AddProject<Projects.Api>("api")
    .PublishAsCloudflareContainer(cloudflare)
    .WithCustomDomain(zoneId: "<ZONE_ID>", hostname: "api.example.com");
```

See [Custom Domains](custom-domains.md).
