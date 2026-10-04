---
title: "Quickstart"
---


This ties the pieces together: an R2 bucket, a .NET API that uses it, and a JavaScript frontend on Pages.

## 1. Set your credentials

For local `aspire run` against the MinIO emulator (`.RunAsEmulator()`), **nothing is required**. For deploying (or running against real R2), set:

```bash
export CLOUDFLARE_API_TOKEN="<your scoped token>"
export CLOUDFLARE_ACCOUNT_ID="<your account id>"
# only if your app accesses real R2 at runtime:
export R2_ACCESS_KEY_ID="<r2 access key id>"
export R2_SECRET_ACCESS_KEY="<r2 secret access key>"
```

In development, prefer [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) on the AppHost over exporting secrets in your shell.

## 2. Install packages and wire up the AppHost

In an existing Aspire AppHost, install these packages at the same available 13.6 version from your package feed and add a project reference to your API. Set the version below to the package version you are using:

```bash
export AVANTIPOINT_VERSION="<available-13.6-package-version>"
dotnet add AppHost/AppHost.csproj package AvantiPoint.Aspire.Hosting.Cloudflare --version "$AVANTIPOINT_VERSION"
dotnet add AppHost/AppHost.csproj package AvantiPoint.Aspire.Hosting.Cloudflare.R2 --version "$AVANTIPOINT_VERSION"
dotnet add AppHost/AppHost.csproj package AvantiPoint.Aspire.Hosting.Cloudflare.Pages --version "$AVANTIPOINT_VERSION"
dotnet add AppHost/AppHost.csproj package AvantiPoint.Aspire.Hosting.Extensions --version "$AVANTIPOINT_VERSION"
dotnet add AppHost/AppHost.csproj package Aspire.Hosting.JavaScript --version 13.6.0
dotnet add AppHost/AppHost.csproj reference Api/Api.csproj
dotnet add Api/Api.csproj package AvantiPoint.Aspire.Cloudflare.R2 --version "$AVANTIPOINT_VERSION"
```

Adjust paths to your solution. `Projects.Api` is generated from the AppHost's API project reference. The Vite project at `../web` must contain a build script and have its npm dependencies installed.

```csharp
using Aspire.Hosting;
using Aspire.Hosting.JavaScript;
using AvantiPoint.Aspire.Hosting.Extensions;
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;

var builder = DistributedApplication.CreateBuilder(args);

// The Cloudflare deploy environment. Reads CLOUDFLARE_API_TOKEN / CLOUDFLARE_ACCOUNT_ID.
// Resources below resolve it automatically — no need to pass it around.
builder.AddCloudflareEnvironment();

// Domains and the Zone ID are deploy-time config, not source — supply them as parameters.
var zone    = builder.AddDeploymentParameter("zone-id");
var apiHost = builder.AddDeploymentParameter("api-hostname");
var webHost = builder.AddDeploymentParameter("web-hostname");

// An R2 bucket. RunAsEmulator() → local MinIO during `aspire run`; deploy always uses real R2.
var uploads = builder.AddR2Bucket("uploads").RunAsEmulator();

// A .NET API that reads/writes the bucket (becomes a Cloudflare Container on deploy).
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(uploads)
    .PublishAsCloudflareContainer()
    .WithCustomDomain(apiHost, zone);

// A Vite frontend deployed to Cloudflare Pages.
builder.AddViteApp("web", "../web")
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .PublishAsCloudflarePages()
    .WithCustomDomain(webHost, zone);

builder.Build().Run();
```

:::tip
Each resource resolves the single Cloudflare environment for you. With more than one environment, use the explicit overloads (e.g. `environment.AddR2Bucket(...)`, `PublishAsCloudflareContainer(environment)`).
:::

## 3. Consume R2 in your service

In the API (or any service) project:

```csharp
using AvantiPoint.Aspire.Cloudflare.R2;

builder.AddR2Client("uploads");   // registers IR2Client (and IAmazonS3) configured for R2
```

```csharp
// IR2Client is bound to the bucket — no bucket name to pass around.
app.MapGet("/data", async (IR2Client r2) =>
{
    var obj = await r2.GetObjectAsync("data.json");
    return Results.Stream(obj.ResponseStream, "application/json");
});
```

## 4. Run locally

```bash
aspire run
```

Because the bucket uses `.RunAsEmulator()`, MinIO starts, the bucket is created in it, and your services run against it — no Cloudflare credentials needed.

## 5. Deploy to Cloudflare

```bash
aspire deploy
```

The token is checked for activeness, R2 buckets are provisioned, the API container and Pages site are deployed via wrangler, and any custom domains are configured.

## Provisioning / seeding

Need to populate data before things start (migrations, seed files)? Add a console project that uses `AddR2Client`, and mark it as a local provisioning step that the others wait on:

```csharp
var seeder = builder.AddProject<Projects.Seeder>("seeder")
    .WithReference(uploads)
    .WaitFor(uploads)
    .ExcludeFromManifest();   // runs locally; never deployed to Cloudflare

api.WaitForCompletion(seeder);
```
