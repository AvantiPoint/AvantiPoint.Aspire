using Amazon.S3;
using AvantiPoint.Aspire.Cloudflare.D1;
using AvantiPoint.Aspire.Cloudflare.R2;

var builder = WebApplication.CreateBuilder(args);

// Registers IR2Client (and IAmazonS3) configured for R2 from the "uploads" connection string injected
// by the AppHost. Locally this points at the MinIO emulator; in production at real R2 — no code change.
builder.AddR2Client("uploads");

// Registers ID1Client from the "catalog" connection string. Locally (RunAsEmulator) this is a SQLite
// file; in production it is the D1 HTTP API — the same query code runs against both.
builder.AddD1Client("catalog");

// Allow the Pages frontend (different origin) to call this API.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new { status = "ok" }));

// Returns the seeded data.json from R2 verbatim. The frontend renders the UI from this payload.
app.MapGet("/data", async (IR2Client r2, CancellationToken ct) =>
{
    try
    {
        var response = await r2.GetObjectAsync("data.json", ct);
        return Results.Stream(response.ResponseStream, "application/json");
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { error = "data.json has not been seeded into the R2 bucket yet." });
    }
});

// Generic object access (handy for tests / ad-hoc use) — note: no bucket name needed.
app.MapPut("/files/{key}", async (string key, HttpRequest request, IR2Client r2, CancellationToken ct) =>
{
    await r2.PutObjectAsync(key, request.Body, request.ContentType, ct);
    return Results.Created($"/files/{key}", new { bucket = r2.BucketName, key });
});

app.MapGet("/files/{key}", async (string key, IR2Client r2, CancellationToken ct) =>
{
    try
    {
        var response = await r2.GetObjectAsync(key, ct);
        return Results.Stream(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream");
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
});

// A page-view counter backed by D1 — the same ID1Client code runs against local SQLite and real D1.
app.MapPost("/visits", async (ID1Client d1, CancellationToken ct) =>
{
    await d1.ExecuteAsync("CREATE TABLE IF NOT EXISTS visits (id INTEGER PRIMARY KEY, at TEXT)", parameters: null, ct);
    await d1.ExecuteAsync("INSERT INTO visits (at) VALUES (?)", [DateTimeOffset.UtcNow.ToString("o")], ct);
    var count = await d1.QueryFirstOrDefaultAsync<long>("SELECT COUNT(*) FROM visits", parameters: null, ct);
    return Results.Ok(new { visits = count });
});

app.Run();
