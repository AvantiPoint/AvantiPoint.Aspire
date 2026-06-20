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

// Local-only provisioning step: uploads the data file into the bucket, then exits.
// Excluded from the manifest so it is never deployed to Cloudflare.
var seeder = builder.AddProject<Projects.CloudflarePlayground_Seeder>("seeder")
    .WithReference(uploads)
    .WaitFor(uploads)
    .ExcludeFromManifest();

// The API reads data.json from R2 and serves it, deployed as a Cloudflare Container.
var api = builder.AddProject<Projects.CloudflarePlayground_Api>("api")
    .WithReference(uploads)
    .WaitFor(uploads)
    .WaitForCompletion(seeder)
    .PublishAsCloudflareContainer(cloudflare);

// A hand-authored Cloudflare Worker: runs locally via `wrangler dev`, deploys via `wrangler deploy`.
cloudflare.AddCloudflareWorker("worker", "../CloudflarePlayground.Worker");

// A JavaScript (Vite) frontend deployed to Cloudflare Pages on `aspire deploy`.
// Run `npm install` in ../CloudflarePlayground.Web before `aspire run`.
builder.AddViteApp("web", "../CloudflarePlayground.Web")
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .PublishAsCloudflarePages(cloudflare)
    .WaitFor(api);

builder.Build().Run();
