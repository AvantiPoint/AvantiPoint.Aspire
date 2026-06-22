using Aspire.Hosting.ApplicationModel;
using AvantiPoint.Aspire.Hosting.Cloudflare;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.D1;

/// <summary>
/// A Cloudflare D1 database modeled as an Aspire resource. Exposes a connection string that targets
/// real D1 over the HTTP query API by default (in run and deploy), or a local SQLite file during
/// <c>aspire run</c> when <see cref="D1HostingExtensions.RunAsEmulator"/> is used. D1 is SQLite under
/// the hood, so the local emulator is a faithful backend.
/// </summary>
public sealed class D1DatabaseResource : Resource, IResourceWithConnectionString, ICloudflareResource
{
    internal D1DatabaseResource(string name, CloudflareEnvironmentResource environment, string databaseName)
        : base(name)
    {
        Environment = environment;
        DatabaseName = databaseName;
    }

    /// <summary>The Cloudflare environment that owns this database.</summary>
    public CloudflareEnvironmentResource Environment { get; }

    /// <summary>The D1 database name (may differ from the Aspire resource name).</summary>
    public string DatabaseName { get; }

    /// <summary>True when this database is served by a local SQLite file (opt-in via <c>RunAsEmulator()</c>).</summary>
    public bool UseEmulator { get; internal set; }

    /// <summary>When true, <c>aspire deploy --destroy</c> will delete the real D1 database. Off by default (data-loss guard).</summary>
    public bool AllowDestroy { get; internal set; }

    /// <summary>Optional D1 primary location hint (e.g. <c>weur</c>, <c>enam</c>) used when provisioning.</summary>
    public string? PrimaryLocationHint { get; internal set; }

    // Emulator wiring (set when UseEmulator is true).
    internal string? SqliteFilePath { get; set; }

    // Optional token override for runtime data access; defaults to the environment API token.
    internal ParameterResource? AccessToken { get; set; }

    /// <inheritdoc />
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    private ReferenceExpression BuildConnectionString()
    {
        if (UseEmulator)
        {
            var path = SqliteFilePath
                ?? throw new InvalidOperationException($"D1 emulator file for database '{Name}' has not been wired.");

            return ReferenceExpression.Create($"Provider=Sqlite;Data Source={path}");
        }

        var accountId = Environment.AccountId;
        var token = AccessToken ?? Environment.ApiToken;

        // The client resolves the database id from the name via the D1 list API, so no provisioned
        // id is needed here — the connection string is fully composable at model-build time.
        return ReferenceExpression.Create(
            $"Provider=D1;AccountId={accountId};Database={DatabaseName};Token={token}");
    }
}
