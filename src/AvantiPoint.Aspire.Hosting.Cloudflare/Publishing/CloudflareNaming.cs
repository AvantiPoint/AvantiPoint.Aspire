using System.Text;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.Publishing;

/// <summary>Derives Cloudflare-valid names (Worker name, Durable Object class, binding) from a resource name.</summary>
internal static class CloudflareNaming
{
    /// <summary>A Workers-valid name: lowercase, alphanumeric or hyphen.</summary>
    public static string WorkerName(string resourceName)
    {
        var sb = new StringBuilder(resourceName.Length);
        foreach (var ch in resourceName.ToLowerInvariant())
        {
            sb.Append(char.IsAsciiLetterOrDigit(ch) ? ch : '-');
        }

        var name = sb.ToString().Trim('-');
        return name.Length == 0 ? "app" : name;
    }

    /// <summary>A valid JS class identifier for the Durable Object, e.g. <c>my-api</c> → <c>MyApiContainer</c>.</summary>
    public static string ClassName(string resourceName)
    {
        var sb = new StringBuilder(resourceName.Length + "Container".Length);
        var capitalizeNext = true;
        foreach (var ch in resourceName)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                sb.Append(capitalizeNext ? char.ToUpperInvariant(ch) : ch);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        var name = sb.ToString();
        if (name.Length == 0 || !char.IsAsciiLetter(name[0]))
        {
            name = "App" + name;
        }

        return name + "Container";
    }

    /// <summary>A valid binding identifier, e.g. <c>MyApiContainer</c> → <c>MY_API_CONTAINER</c>.</summary>
    public static string BindingName(string className)
    {
        var sb = new StringBuilder(className.Length + 8);
        for (var i = 0; i < className.Length; i++)
        {
            var ch = className[i];
            if (i > 0 && char.IsAsciiLetterUpper(ch) && !char.IsAsciiLetterUpper(className[i - 1]))
            {
                sb.Append('_');
            }

            sb.Append(char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }
}
