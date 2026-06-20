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
