using Amazon.S3;
using Amazon.S3.Model;
using AvantiPoint.Aspire.Cloudflare.R2;

var builder = WebApplication.CreateBuilder(args);

// Registers IAmazonS3 configured for R2 from the "uploads" connection string injected by the AppHost.
// Locally this points at the MinIO emulator; in production at real R2 — no code change.
builder.AddR2Client("uploads");

// Allow the Pages frontend (different origin) to call this API.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new { status = "ok" }));

// Returns the seeded data.json from R2 verbatim. The frontend renders the UI from this payload.
app.MapGet("/data", async (IAmazonS3 s3, R2ClientSettings settings, CancellationToken ct) =>
{
    try
    {
        var response = await s3.GetObjectAsync(settings.BucketName, "data.json", ct);
        return Results.Stream(response.ResponseStream, "application/json");
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { error = "data.json has not been seeded into the R2 bucket yet." });
    }
});

// Generic object access (handy for tests / ad-hoc use).
app.MapPut("/files/{key}", async (string key, HttpRequest request, IAmazonS3 s3, R2ClientSettings settings, CancellationToken ct) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms, ct);
    ms.Position = 0;

    await s3.PutObjectAsync(new PutObjectRequest
    {
        BucketName = settings.BucketName,
        Key = key,
        InputStream = ms,
        ContentType = request.ContentType ?? "application/octet-stream",
    }, ct);

    return Results.Created($"/files/{key}", new { bucket = settings.BucketName, key });
});

app.MapGet("/files/{key}", async (string key, IAmazonS3 s3, R2ClientSettings settings, CancellationToken ct) =>
{
    try
    {
        var response = await s3.GetObjectAsync(settings.BucketName, key, ct);
        return Results.Stream(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream");
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
});

app.Run();
