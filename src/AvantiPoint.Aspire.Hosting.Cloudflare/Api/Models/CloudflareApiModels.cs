using System.Text.Json.Serialization;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Api.Models;

/// <summary>Standard Cloudflare API envelope.</summary>
public sealed class CloudflareApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("errors")]
    public List<CloudflareApiError> Errors { get; init; } = [];

    [JsonPropertyName("messages")]
    public List<CloudflareApiMessage> Messages { get; init; } = [];

    [JsonPropertyName("result")]
    public T? Result { get; init; }
}

public sealed class CloudflareApiError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    public override string ToString() => $"{Code}: {Message}";
}

public sealed class CloudflareApiMessage
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>Result of <c>GET /user/tokens/verify</c>.</summary>
public sealed class TokenVerifyResult
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>"active", "disabled", or "expired".</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

/// <summary>A single R2 bucket as returned by the R2 API.</summary>
public sealed class R2Bucket
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("storage_class")]
    public string? StorageClass { get; init; }

    [JsonPropertyName("creation_date")]
    public string? CreationDate { get; init; }
}

/// <summary>A DNS record (subset of fields we use).</summary>
public sealed class DnsRecord
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("proxied")]
    public bool Proxied { get; init; }
}

/// <summary>Request body for creating/updating a DNS record.</summary>
public sealed class DnsRecordRequest
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "CNAME";

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("proxied")]
    public bool Proxied { get; init; } = true;
}

/// <summary>Request body for attaching a Worker to a custom domain.</summary>
public sealed class WorkersDomainRequest
{
    [JsonPropertyName("zone_id")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; init; } = string.Empty;

    [JsonPropertyName("service")]
    public string Service { get; init; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; init; } = "production";
}

/// <summary>Request body for adding a custom domain to a Pages project.</summary>
public sealed class PagesDomainRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>A D1 database as returned by the D1 API.</summary>
public sealed class D1Database
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; init; }
}

/// <summary>Request body for creating a D1 database.</summary>
public sealed class CreateD1DatabaseRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("primary_location_hint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrimaryLocationHint { get; init; }
}

/// <summary>A Workers KV namespace as returned by the KV API.</summary>
public sealed class KvNamespace
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;
}

/// <summary>Request body for creating a Workers KV namespace.</summary>
public sealed class CreateKvNamespaceRequest
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;
}

/// <summary>A Queue as returned by the Queues API.</summary>
public sealed class CloudflareQueue
{
    [JsonPropertyName("queue_id")]
    public string QueueId { get; init; } = string.Empty;

    [JsonPropertyName("queue_name")]
    public string QueueName { get; init; } = string.Empty;
}

/// <summary>Request body for creating a Queue.</summary>
public sealed class CreateQueueRequest
{
    [JsonPropertyName("queue_name")]
    public string QueueName { get; init; } = string.Empty;
}

/// <summary>A Vectorize index as returned by the Vectorize API.</summary>
public sealed class VectorizeIndex
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>Request body for creating a Vectorize index.</summary>
public sealed class CreateVectorizeIndexRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("config")]
    public VectorizeIndexConfig Config { get; init; } = new();
}

/// <summary>The dimensions + distance metric of a Vectorize index.</summary>
public sealed class VectorizeIndexConfig
{
    [JsonPropertyName("dimensions")]
    public int Dimensions { get; init; }

    [JsonPropertyName("metric")]
    public string Metric { get; init; } = "cosine";
}

/// <summary>An AI Gateway as returned by the AI Gateway API.</summary>
public sealed class AIGateway
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
}

/// <summary>Request body for creating an AI Gateway (minimal required fields).</summary>
public sealed class CreateAIGatewayRequest
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("cache_ttl")]
    public int CacheTtl { get; init; }

    [JsonPropertyName("collect_logs")]
    public bool CollectLogs { get; init; } = true;

    [JsonPropertyName("rate_limiting_interval")]
    public int RateLimitingInterval { get; init; }

    [JsonPropertyName("rate_limiting_limit")]
    public int RateLimitingLimit { get; init; }

    [JsonPropertyName("rate_limiting_technique")]
    public string RateLimitingTechnique { get; init; } = "fixed";
}

/// <summary>Request body for creating an R2 bucket.</summary>
public sealed class CreateR2BucketRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("locationHint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LocationHint { get; init; }

    [JsonPropertyName("storageClass")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StorageClass { get; init; }
}
