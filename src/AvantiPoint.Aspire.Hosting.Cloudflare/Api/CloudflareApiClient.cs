using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;
using Microsoft.Extensions.Logging;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Api;

/// <summary>Default <see cref="ICloudflareApiClient"/> backed by <see cref="HttpClient"/>.</summary>
internal sealed class CloudflareApiClient(HttpClient httpClient, ILogger<CloudflareApiClient> logger) : ICloudflareApiClient
{
    /// <summary>Base address for the Cloudflare v4 REST API.</summary>
    public static readonly Uri BaseAddress = new("https://api.cloudflare.com/client/v4/");

    // Cloudflare error code returned when creating a bucket that already exists.
    private const int BucketAlreadyExistsCode = 10004;

    public async Task<TokenVerifyResult> VerifyTokenAsync(string apiToken, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "user/tokens/verify", apiToken);
        var result = await SendAsync<TokenVerifyResult>(request, "verify token", cancellationToken).ConfigureAwait(false);
        return result!;
    }

    public async Task<R2Bucket> CreateR2BucketAsync(string apiToken, string accountId, CreateR2BucketRequest body, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"accounts/{accountId}/r2/buckets", apiToken);
        request.Content = JsonContent.Create(body);

        var envelope = await SendRawAsync<R2Bucket>(request, cancellationToken).ConfigureAwait(false);
        if (envelope.Success && envelope.Result is not null)
        {
            return envelope.Result;
        }

        if (envelope.Errors.Any(e => e.Code == BucketAlreadyExistsCode))
        {
            logger.LogInformation("R2 bucket '{Bucket}' already exists; reusing it.", body.Name);
            return await GetR2BucketAsync(apiToken, accountId, body.Name, cancellationToken).ConfigureAwait(false)
                ?? new R2Bucket { Name = body.Name };
        }

        throw CloudflareApiException.FromResponse("create R2 bucket", envelope.Errors);
    }

    public async Task<R2Bucket?> GetR2BucketAsync(string apiToken, string accountId, string bucketName, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"accounts/{accountId}/r2/buckets/{bucketName}", apiToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var envelope = await ReadEnvelopeAsync<R2Bucket>(response, cancellationToken).ConfigureAwait(false);
        if (!envelope.Success)
        {
            if (envelope.Errors.Any(e => e.Code == BucketAlreadyExistsCode))
            {
                return null;
            }

            throw CloudflareApiException.FromResponse("get R2 bucket", envelope.Errors);
        }

        return envelope.Result;
    }

    public async Task DeleteR2BucketAsync(string apiToken, string accountId, string bucketName, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"accounts/{accountId}/r2/buckets/{bucketName}", apiToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        var envelope = await ReadEnvelopeAsync<object>(response, cancellationToken).ConfigureAwait(false);
        if (!envelope.Success)
        {
            throw CloudflareApiException.FromResponse("delete R2 bucket", envelope.Errors);
        }
    }

    public async Task AttachWorkersCustomDomainAsync(string apiToken, string accountId, string zoneId, string hostname, string service, string environment = "production", CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Put, $"accounts/{accountId}/workers/domains", apiToken);
        request.Content = JsonContent.Create(new WorkersDomainRequest
        {
            ZoneId = zoneId,
            Hostname = hostname,
            Service = service,
            Environment = environment,
        });

        await SendAsync<object>(request, "attach Workers custom domain", cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Attached Worker '{Service}' to custom domain '{Hostname}'.", service, hostname);
    }

    public async Task AttachPagesDomainAsync(string apiToken, string accountId, string projectName, string hostname, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"accounts/{accountId}/pages/projects/{projectName}/domains", apiToken);
        request.Content = JsonContent.Create(new PagesDomainRequest { Name = hostname });

        var envelope = await SendRawAsync<object>(request, cancellationToken).ConfigureAwait(false);
        if (envelope.Success)
        {
            logger.LogInformation("Added custom domain '{Hostname}' to Pages project '{Project}'.", hostname, projectName);
            return;
        }

        // Treat an already-existing domain as success.
        if (envelope.Errors.Any(e => e.Message.Contains("already", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        throw CloudflareApiException.FromResponse("attach Pages custom domain", envelope.Errors);
    }

    public async Task UpsertCnameRecordAsync(string apiToken, string zoneId, string name, string content, CancellationToken cancellationToken = default)
    {
        using var lookup = CreateRequest(HttpMethod.Get, $"zones/{zoneId}/dns_records?type=CNAME&name={Uri.EscapeDataString(name)}", apiToken);
        var existing = await SendAsync<List<DnsRecord>>(lookup, "list DNS records", cancellationToken).ConfigureAwait(false);

        var body = new DnsRecordRequest { Type = "CNAME", Name = name, Content = content, Proxied = true };
        var current = existing?.FirstOrDefault();
        if (current is not null)
        {
            using var update = CreateRequest(HttpMethod.Put, $"zones/{zoneId}/dns_records/{current.Id}", apiToken);
            update.Content = JsonContent.Create(body);
            await SendAsync<DnsRecord>(update, "update DNS record", cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var create = CreateRequest(HttpMethod.Post, $"zones/{zoneId}/dns_records", apiToken);
            create.Content = JsonContent.Create(body);
            await SendAsync<DnsRecord>(create, "create DNS record", cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Upserted CNAME '{Name}' -> '{Content}'.", name, content);
    }

    public async Task<D1Database> CreateD1DatabaseAsync(string apiToken, string accountId, CreateD1DatabaseRequest body, CancellationToken cancellationToken = default)
    {
        // D1 has no "create or get" semantics — a second create makes a second database with the same
        // name — so look up by name first to stay idempotent.
        var existing = await GetD1DatabaseByNameAsync(apiToken, accountId, body.Name, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            logger.LogInformation("D1 database '{Database}' already exists; reusing it.", body.Name);
            return existing;
        }

        using var request = CreateRequest(HttpMethod.Post, $"accounts/{accountId}/d1/database", apiToken);
        request.Content = JsonContent.Create(body);

        var result = await SendAsync<D1Database>(request, "create D1 database", cancellationToken).ConfigureAwait(false);
        return result ?? new D1Database { Name = body.Name };
    }

    public async Task<D1Database?> GetD1DatabaseByNameAsync(string apiToken, string accountId, string name, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"accounts/{accountId}/d1/database?name={Uri.EscapeDataString(name)}", apiToken);
        var databases = await SendAsync<List<D1Database>>(request, "list D1 databases", cancellationToken).ConfigureAwait(false);
        return databases?.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.Ordinal));
    }

    public async Task DeleteD1DatabaseAsync(string apiToken, string accountId, string databaseId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"accounts/{accountId}/d1/database/{databaseId}", apiToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        var envelope = await ReadEnvelopeAsync<object>(response, cancellationToken).ConfigureAwait(false);
        if (!envelope.Success)
        {
            throw CloudflareApiException.FromResponse("delete D1 database", envelope.Errors);
        }
    }

    public async Task<AIGateway> CreateAIGatewayAsync(string apiToken, string accountId, CreateAIGatewayRequest body, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"accounts/{accountId}/ai-gateway/gateways", apiToken);
        request.Content = JsonContent.Create(body);

        var envelope = await SendRawAsync<AIGateway>(request, cancellationToken).ConfigureAwait(false);
        if (envelope.Success && envelope.Result is not null)
        {
            return envelope.Result;
        }

        // Treat an already-existing gateway as success (idempotent provisioning).
        if (envelope.Errors.Any(e => e.Message.Contains("already", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogInformation("AI Gateway '{Gateway}' already exists; reusing it.", body.Id);
            return new AIGateway { Id = body.Id };
        }

        throw CloudflareApiException.FromResponse("create AI Gateway", envelope.Errors);
    }

    public async Task DeleteAIGatewayAsync(string apiToken, string accountId, string gatewayId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"accounts/{accountId}/ai-gateway/gateways/{gatewayId}", apiToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        var envelope = await ReadEnvelopeAsync<object>(response, cancellationToken).ConfigureAwait(false);
        if (!envelope.Success)
        {
            throw CloudflareApiException.FromResponse("delete AI Gateway", envelope.Errors);
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri, string apiToken)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task<T?> SendAsync<T>(HttpRequestMessage request, string operation, CancellationToken cancellationToken)
    {
        var envelope = await SendRawAsync<T>(request, cancellationToken).ConfigureAwait(false);
        if (!envelope.Success)
        {
            throw CloudflareApiException.FromResponse(operation, envelope.Errors);
        }

        return envelope.Result;
    }

    private async Task<CloudflareApiResponse<T>> SendRawAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadEnvelopeAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<CloudflareApiResponse<T>> ReadEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content
                .ReadFromJsonAsync<CloudflareApiResponse<T>>(cancellationToken)
                .ConfigureAwait(false);

            return envelope ?? new CloudflareApiResponse<T>
            {
                Errors = [new CloudflareApiError { Message = $"Empty response (HTTP {(int)response.StatusCode})." }],
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            return new CloudflareApiResponse<T>
            {
                Errors = [new CloudflareApiError { Message = $"HTTP {(int)response.StatusCode}: {ex.Message}" }],
            };
        }
    }
}
