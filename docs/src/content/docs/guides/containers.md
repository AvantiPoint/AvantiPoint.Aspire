---
title: "Cloudflare Containers"
---


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

## Environment & configuration

The project's resolved environment variables — connection strings from `WithReference`, anything from `WithEnvironment` — are injected into the container via the Worker's `envVars`, so your app reads its configuration exactly as it does locally. Local-only variables (OpenTelemetry endpoints, service-discovery URLs, the local listen URL) are filtered out.

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(uploads)               // an R2 bucket — its connection string is injected
    .WithEnvironment("FEATURE_FLAG", "on")
    .PublishAsCloudflareContainer(cloudflare);
```

At deploy, the R2 connection string resolves to the **real** R2 endpoint and credentials, so the container reaches R2 in production with no code change.

:::note
Values are resolved at deploy time and embedded in the generated Worker (which Cloudflare stores privately per account). Set deploy credentials (`CLOUDFLARE_ACCOUNT_ID`, `R2_ACCESS_KEY_ID`, `R2_SECRET_ACCESS_KEY`) before `aspire deploy` so they resolve. Promoting secret values to Cloudflare Worker secrets is a planned enhancement.
:::


## Custom domains

```csharp
builder.AddProject<Projects.Api>("api")
    .PublishAsCloudflareContainer(cloudflare)
    .WithCustomDomain(zoneId: "<ZONE_ID>", hostname: "api.example.com");
```

See [Custom Domains](custom-domains.md).
