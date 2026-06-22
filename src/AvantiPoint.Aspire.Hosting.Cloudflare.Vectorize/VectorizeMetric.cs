namespace AvantiPoint.Aspire.Hosting.Cloudflare.Vectorize;

/// <summary>The distance metric a Vectorize index uses to compare vectors.</summary>
public enum VectorizeMetric
{
    /// <summary>Cosine similarity (higher is more similar). The common default for text embeddings.</summary>
    Cosine,

    /// <summary>Euclidean (L2) distance (lower is more similar).</summary>
    Euclidean,

    /// <summary>Dot-product similarity (higher is more similar).</summary>
    DotProduct,
}

internal static class VectorizeMetricExtensions
{
    /// <summary>The Cloudflare API string for the metric.</summary>
    public static string ToApiValue(this VectorizeMetric metric) => metric switch
    {
        VectorizeMetric.Cosine => "cosine",
        VectorizeMetric.Euclidean => "euclidean",
        VectorizeMetric.DotProduct => "dot-product",
        _ => "cosine",
    };
}
