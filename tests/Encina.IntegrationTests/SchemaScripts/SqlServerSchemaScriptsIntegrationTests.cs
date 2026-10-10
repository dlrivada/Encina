using System.Text.RegularExpressions;
using Encina.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Xunit;
using AdoDeadLetterFactory = Encina.ADO.SqlServer.DeadLetter.DeadLetterMessageFactory;
using AdoDeadLetterStore = Encina.ADO.SqlServer.DeadLetter.DeadLetterStoreADO;
using DapperDeadLetterFactory = Encina.Dapper.SqlServer.DeadLetter.DeadLetterMessageFactory;
using DapperDeadLetterStore = Encina.Dapper.SqlServer.DeadLetter.DeadLetterStoreDapper;

namespace Encina.IntegrationTests.SchemaScripts;

/// <summary>
/// Verifies that the <c>029_CreateDeadLetterMessagesTable.sql</c> file shipped by <c>Encina.ADO.SqlServer</c>
/// and <c>Encina.Dapper.SqlServer</c> is valid SQL Server syntax and produces a schema that the packages' own
/// stores can read and write (#583). The fixture schema copy in <c>SqlServerSchema</c> is not used: the script
/// runs in a database of its own, so that a table created by the fixture cannot hide a broken script.
/// </summary>
[Collection("ADO-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Provider", "SqlServer")]
public sealed partial class SqlServerSchemaScriptsIntegrationTests : IAsyncLifetime
{
    private const string DatabaseName = "issue583_schema_scripts";

    private readonly SqlServerFixture _fixture;

    public SqlServerSchemaScriptsIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(
            connection,
            $"IF DB_ID(N'{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END; CREATE DATABASE [{DatabaseName}];");
    }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(
            connection,
            $"IF DB_ID(N'{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END;");
    }

    /// <summary>
    /// The shipped script creates the table with its six indexes, running it twice is harmless, and the
    /// package's store round-trips on the table with the binary collation the script declares: the unique source
    /// key rejects a second capture of the same source message and keeps ids that differ only in case apart.
    /// </summary>
    [Theory]
    [InlineData("Encina.ADO.SqlServer")]
    [InlineData("Encina.Dapper.SqlServer")]
    public async Task DeadLetterScript_CreatesTheSixIndexes_AndTheStoreRoundTripsOnTheTable(string packageName)
    {
        await using var connection = await OpenScriptDatabaseAsync();
        var script = await File.ReadAllTextAsync(Directory.GetFiles(FindScriptsFolder(packageName), "029_*.sql").Single());
        await RunScriptAsync(connection, script);
        await RunScriptAsync(connection, script);

        var indexes = await QueryIndexNamesAsync(connection);
        string[] expected =
        [
            "UX_DeadLetterMessages_Source",
            "IX_DeadLetterMessages_DeadLetteredAt",
            "IX_DeadLetterMessages_Pending",
            "IX_DeadLetterMessages_ExpiresAt",
            "IX_DeadLetterMessages_CorrelationId",
            "IX_DeadLetterMessages_Tenant"
        ];
        foreach (var name in expected)
        {
            Assert.Contains(name, indexes);
        }

        var dapper = packageName.Contains("Dapper", StringComparison.Ordinal);
        IDeadLetterStore store = dapper
            ? new DapperDeadLetterStore(connection)
            : new AdoDeadLetterStore(connection);
        IDeadLetterMessageFactory factory = dapper
            ? new DapperDeadLetterFactory()
            : new AdoDeadLetterFactory();

        Assert.True((await store.AddAsync(factory.Create(Data("script-1")))).ShouldBeRight());
        Assert.False((await store.AddAsync(factory.Create(Data("script-1")))).ShouldBeRight());
        Assert.True((await store.AddAsync(factory.Create(Data("Script-1")))).ShouldBeRight());
        Assert.Equal(2, (await store.GetCountAsync()).ShouldBeRight());
        Assert.Equal(1, (await store.GetCountAsync(new DeadLetterFilter { SourceMessageId = "Script-1" })).ShouldBeRight());
    }

    private static DeadLetterData Data(string sourceMessageId)
        => new(
            Id: Guid.NewGuid(),
            RequestType: "T",
            RequestContent: "{}",
            ErrorCode: "e",
            SourcePattern: "Outbox",
            SourceMessageId: sourceMessageId,
            TotalRetryAttempts: 1,
            FirstFailedAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DeadLetteredAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc: null);

    private async Task<SqlConnection> OpenScriptDatabaseAsync()
    {
        var builder = new SqlConnectionStringBuilder(_fixture.ConnectionString) { InitialCatalog = DatabaseName };
        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        return connection;
    }

    // The script ends with a GO batch separator, which is a client-tool keyword and not T-SQL.
    private static async Task RunScriptAsync(SqlConnection connection, string script)
    {
        foreach (var batch in GoSeparator().Split(script).Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            await ExecuteAsync(connection, batch);
        }
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> QueryIndexNamesAsync(SqlConnection connection)
    {
        var names = new List<string>();
        await using var command = new SqlCommand(
            "SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'[dbo].[DeadLetterMessages]') AND i.name IS NOT NULL;",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static string FindScriptsFolder(string packageName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Encina.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root (Encina.slnx) from the test base directory.");
        }

        return Path.Combine(directory.FullName, "src", packageName, "Scripts");
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}
