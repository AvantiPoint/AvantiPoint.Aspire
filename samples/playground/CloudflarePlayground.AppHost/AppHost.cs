using AvantiPoint.Aspire.Hosting.Cloudflare;
using AvantiPoint.Aspire.Hosting.Cloudflare.D1;
using AvantiPoint.Aspire.Hosting.Cloudflare.Pages;
using AvantiPoint.Aspire.Hosting.Cloudflare.R2;

var builder = DistributedApplication.CreateBuilder(args);

// The Cloudflare deployment environment. Set CLOUDFLARE_API_TOKEN / CLOUDFLARE_ACCOUNT_ID
// (env vars or user-secrets) for `aspire deploy`. Local `aspire run` needs no credentials.
// Resources below resolve this environment automatically.
builder.AddCloudflareEnvironment();

// Real R2 by default. RunAsEmulator() backs it with a local MinIO emulator during `aspire run`
// (a credential-free inner loop); `aspire deploy` always provisions real R2.
var uploads = builder.AddR2Bucket("uploads").RunAsEmulator();

// A D1 database. RunAsEmulator() backs it with a local SQLite file during `aspire run`; `aspire deploy`
// provisions a real D1 database and the API talks to it over the D1 HTTP API — same query code.
var catalog = builder.AddD1Database("catalog").RunAsEmulator();

// Local-only provisioning step: uploads the data file into the bucket, then exits.
// Excluded from the manifest so it is never deployed to Cloudflare.
var seeder = builder.AddProject<Projects.CloudflarePlayground_Seeder>("seeder")
    .WithReference(uploads)
    .WaitFor(uploads)
    .ExcludeFromManifest();

// The API reads data.json from R2 and serves it, deployed as a Cloudflare Container.
var api = builder.AddProject<Projects.CloudflarePlayground_Api>("api")
    .WithHttpEndpoint()
    .WithReference(uploads)
    .WithReference(catalog)
    .WaitFor(uploads)
    .WaitForCompletion(seeder)
    .PublishAsCloudflareContainer();

// A hand-authored Cloudflare Worker: runs locally via `wrangler dev`, deploys via `wrangler deploy`.
builder.AddCloudflareWorker("worker", "../CloudflarePlayground.Worker");

// A JavaScript (Vite) frontend deployed to Cloudflare Pages on `aspire deploy`.
// Run `npm install` in ../CloudflarePlayground.Web before `aspire run`.
builder.AddViteApp("web", "../CloudflarePlayground.Web")
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .PublishAsCloudflarePages()
    .WaitFor(api);

builder.Build().Run();
