# Quickstart

This ties the pieces together: an R2 bucket, a .NET API that uses it, and a JavaScript frontend on Pages.

## 1. Set your credentials

For local `aspire run` against the MinIO emulator, **nothing is required**. For deploying (or `.RunAsReal()`), set:

```bash
export CLOUDFLARE_API_TOKEN="<your scoped token>"
export CLOUDFLARE_ACCOUNT_ID="<your account id>"
# only if your app accesses real R2 at runtime:
export R2_ACCESS_KEY_ID="<r2 access key id>"
export R2_SECRET_ACCESS_KEY="<r2 secret access key>"
```

In development, prefer [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) on the AppHost over exporting secrets in your shell.

## 2. Wire up the AppHost

```csharp
using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;

var builder = DistributedApplication.CreateBuilder(args);

// The Cloudflare deploy environment. Reads CLOUDFLARE_API_TOKEN / CLOUDFLARE_ACCOUNT_ID.
var cloudflare = builder.AddCloudflareEnvironment();

// An R2 bucket. Local: MinIO emulator. Deploy: real R2.
var uploads = cloudflare.AddR2Bucket("uploads");

// A .NET API that reads/writes the bucket (becomes a Cloudflare Container on deploy).
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(uploads)
    .PublishAsCloudflareContainer(cloudflare)
    .WithCustomDomain(zoneId: "<ZONE_ID>", hostname: "api.example.com");

// A Vite frontend deployed to Cloudflare Pages.
builder.AddViteApp("web", "../web")
    .WithReference(api)
    .PublishAsCloudflarePages(cloudflare)
    .WithCustomDomain(zoneId: "<ZONE_ID>", hostname: "www.example.com");

builder.Build().Run();
```

## 3. Consume R2 in your service

In the API (or any service) project:

```csharp
builder.AddR2Client("uploads");   // registers IAmazonS3 configured for R2
```

```csharp
app.MapGet("/data", async (IAmazonS3 s3, R2ClientSettings settings) =>
{
    var obj = await s3.GetObjectAsync(settings.BucketName, "data.json");
    return Results.Stream(obj.ResponseStream, "application/json");
});
```

## 4. Run locally

```bash
aspire run
```

MinIO starts, the bucket is created in it, and your services run against it — no Cloudflare credentials needed.

## 5. Deploy to Cloudflare

```bash
aspire deploy
```

The token is validated, R2 buckets are provisioned, the API container and Pages site are deployed via wrangler, and any custom domains are configured.

## Provisioning / seeding

Need to populate data before things start (migrations, seed files)? Add a console project that uses `AddR2Client`, and mark it as a local provisioning step that the others wait on:

```csharp
var seeder = builder.AddProject<Projects.Seeder>("seeder")
    .WithReference(uploads)
    .WaitFor(uploads)
    .ExcludeFromManifest();   // runs locally; never deployed to Cloudflare

api.WaitForCompletion(seeder);
```
