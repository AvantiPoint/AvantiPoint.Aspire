using AvantiPoint.Aspire.Cloudflare.D1;
using AvantiPoint.Aspire.Cloudflare.D1.Backends;
using Xunit;

namespace AvantiPoint.Aspire.Hosting.Cloudflare.D1.Tests;

public class D1ClientSettingsTests
{
    [Fact]
    public void ApplyConnectionString_Parses_Sqlite_Provider()
    {
        var settings = new D1ClientSettings();
        settings.ApplyConnectionString(@"Provider=Sqlite;Data Source=C:\tmp\catalog.db");

        Assert.True(settings.IsSqlite);
        Assert.Equal(@"C:\tmp\catalog.db", settings.DataSource);
    }

    [Fact]
    public void ApplyConnectionString_Parses_D1_Http_Provider()
    {
        var settings = new D1ClientSettings();
        settings.ApplyConnectionString("Provider=D1;AccountId=acc123;Database=catalog;Token=secrettoken");

        Assert.False(settings.IsSqlite);
        Assert.Equal("acc123", settings.AccountId);
        Assert.Equal("catalog", settings.DatabaseName);
        Assert.Equal("secrettoken", settings.Token);
    }
}

public class D1PositionalParameterTests
{
    [Fact]
    public void Rewrites_Placeholders_To_Named_Parameters()
    {
        var sql = SqliteD1Backend.RewritePositionalParameters(
            "INSERT INTO items (id, name) VALUES (?, ?)", out var names);

        Assert.Equal("INSERT INTO items (id, name) VALUES (@p0, @p1)", sql);
        Assert.Equal(["@p0", "@p1"], names);
    }

    [Fact]
    public void Skips_Question_Marks_Inside_String_Literals()
    {
        var sql = SqliteD1Backend.RewritePositionalParameters(
            "SELECT * FROM faq WHERE q = 'why?' AND id = ?", out var names);

        Assert.Equal("SELECT * FROM faq WHERE q = 'why?' AND id = @p0", sql);
        Assert.Single(names);
    }
}

public class D1RowMapperTests
{
    private sealed class Item
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public bool Active { get; set; }
    }

    [Fact]
    public void Maps_Poco_By_Column_Name_CaseInsensitive()
    {
        var row = new D1Row(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = 7L,
            ["name"] = "widget",
            ["active"] = 1L, // SQLite/D1 store booleans as integers
        });

        var item = D1RowMapper.Map<Item>(row);

        Assert.Equal(7, item.Id);
        Assert.Equal("widget", item.Name);
        Assert.True(item.Active);
    }

    [Fact]
    public void Maps_Scalar_From_First_Column()
    {
        var row = new D1Row(new Dictionary<string, object?> { ["count"] = 42L });

        Assert.Equal(42, D1RowMapper.Map<int>(row));
    }
}

public class SqliteD1BackendTests
{
    [Fact]
    public async Task RoundTrips_Insert_And_Query_Via_Client()
    {
        var ct = TestContext.Current.CancellationToken;
        var dbPath = Path.Combine(Path.GetTempPath(), $"d1-test-{Guid.NewGuid():N}.db");
        try
        {
            ID1Client client = new D1Client(new SqliteD1Backend(dbPath));

            await client.ExecuteAsync("CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT)", parameters: null, ct);
            var affected = await client.ExecuteAsync(
                "INSERT INTO items (name) VALUES (?)", ["widget"], ct);
            Assert.Equal(1, affected);

            var names = await client.QueryAsync<string>("SELECT name FROM items WHERE id = ?", [1], ct);
            Assert.Equal("widget", Assert.Single(names));

            var count = await client.QueryFirstOrDefaultAsync<long>("SELECT COUNT(*) FROM items", parameters: null, ct);
            Assert.Equal(1, count);
        }
        finally
        {
            // Microsoft.Data.Sqlite pools connections, which keeps the file handle open — clear the
            // pool before deleting the temp database.
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup of a temp file.
            }
        }
    }

    [Fact]
    public async Task Batch_Runs_Statements_In_A_Transaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var dbPath = Path.Combine(Path.GetTempPath(), $"d1-test-{Guid.NewGuid():N}.db");
        try
        {
            ID1Client client = new D1Client(new SqliteD1Backend(dbPath));
            await client.ExecuteAsync("CREATE TABLE nums (n INTEGER)", parameters: null, ct);

            var results = await client.BatchAsync(
            [
                new D1Statement("INSERT INTO nums (n) VALUES (?)", 1),
                new D1Statement("INSERT INTO nums (n) VALUES (?)", 2),
            ], ct);

            Assert.Equal(2, results.Count);
            var total = await client.QueryFirstOrDefaultAsync<long>("SELECT SUM(n) FROM nums", parameters: null, ct);
            Assert.Equal(3, total);
        }
        finally
        {
            // Microsoft.Data.Sqlite pools connections, which keeps the file handle open — clear the
            // pool before deleting the temp database.
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup of a temp file.
            }
        }
    }
}
