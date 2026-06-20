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
