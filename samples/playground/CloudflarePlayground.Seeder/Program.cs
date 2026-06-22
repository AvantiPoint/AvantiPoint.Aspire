using AvantiPoint.Aspire.Cloudflare.R2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// A local-only provisioning step: connects to the R2 bucket and uploads the data file the API
// serves. Think of it as a migration/seed task. It runs to completion during `aspire run`; it is
// NOT deployed to Cloudflare (excluded from the manifest in the AppHost).

var builder = Host.CreateApplicationBuilder(args);
builder.AddR2Client("uploads");

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
var r2 = host.Services.GetRequiredService<IR2Client>();

// The data the API will serve and the frontend will render.
const string Key = "data.json";
const string Data = """
{
  "title": "AvantiPoint Aspire for Cloudflare",
  "tagline": "Seeded into R2, served by the API, rendered by Pages.",
  "features": [
    { "name": "R2 Storage", "description": "S3-compatible object storage, emulated locally with MinIO." },
    { "name": "Containers", "description": "Your .NET API runs as a Cloudflare Container." },
    { "name": "Pages", "description": "Your JavaScript frontend is deployed to Cloudflare Pages." }
  ]
}
""";

logger.LogInformation("Uploading '{Key}' to R2 bucket '{Bucket}'...", Key, r2.BucketName);

await r2.PutObjectAsync(Key, Data, "application/json");

logger.LogInformation("Seed complete: '{Key}' uploaded to '{Bucket}'.", Key, r2.BucketName);
