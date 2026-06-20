using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;

var builder = DistributedApplication.CreateBuilder(args);

// The Cloudflare deployment environment. Set CLOUDFLARE_API_TOKEN / CLOUDFLARE_ACCOUNT_ID
// (env vars or user-secrets) for `aspire deploy`. Local `aspire run` needs no credentials.
var cloudflare = builder.AddCloudflareEnvironment();

// During `aspire run` this bucket is served by a local MinIO emulator (auto-created).
// During `aspire deploy` it is provisioned in real R2.
var uploads = cloudflare.AddR2Bucket("uploads");

builder.AddProject<Projects.CloudflarePlayground_Api>("api")
    .WithReference(uploads)
    .WaitFor(uploads);

// A JavaScript (Vite) frontend deployed to Cloudflare Pages on `aspire deploy`.
// Run `npm install` in ../CloudflarePlayground.Web before `aspire run`.
builder.AddViteApp("web", "../CloudflarePlayground.Web")
    .PublishAsCloudflarePages(cloudflare);

builder.Build().Run();
