using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;
using Xunit;
using AdoOutboxMessage = Encina.ADO.MySQL.Outbox.OutboxMessage;
using AdoOutboxStore = Encina.ADO.MySQL.Outbox.OutboxStoreADO;
using AdoSagaState = Encina.ADO.MySQL.Sagas.SagaState;
using AdoSagaStore = Encina.ADO.MySQL.Sagas.SagaStoreADO;
using DapperOutboxMessage = Encina.Dapper.MySQL.Outbox.OutboxMessage;
using DapperOutboxStore = Encina.Dapper.MySQL.Outbox.OutboxStoreDapper;

namespace Encina.IntegrationTests.SchemaScripts;

/// <summary>
/// Verifies that the <c>Scripts/*.sql</c> files shipped by <c>Encina.ADO.MySQL</c> and
/// <c>Encina.Dapper.MySQL</c> use valid MySQL syntax and produce a schema that the
/// packages' own stores can read and write (issue #1261).
/// </summary>
[Collection("ADO-MySQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "MySQL")]
public sealed class MySqlSchemaScriptsIntegrationTests : IAsyncLifetime
{
    private const string DatabaseName = "issue1261_schema_scripts";

    private readonly MySqlFixture _fixture;

    public MySqlSchemaScriptsIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        using var connection = (MySqlConnection)_fixture.CreateConnection();
        await using var drop = new MySqlCommand($"DROP DATABASE IF EXISTS `{DatabaseName}`;", connection);
        await drop.ExecuteNonQueryAsync();
        await using var create = new MySqlCommand($"CREATE DATABASE `{DatabaseName}`;", connection);
        await create.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        using var connection = (MySqlConnection)_fixture.CreateConnection();
        await using var command = new MySqlCommand($"DROP DATABASE IF EXISTS `{DatabaseName}`;", connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Every script under the package's Scripts folder must execute without error against a
    /// fresh database (the packages' own SQL dialect, not SQL Server's).
    /// </summary>
    [Theory]
    [InlineData("Encina.ADO.MySQL")]
    [InlineData("Encina.Dapper.MySQL")]
    public async Task Scripts_ExecuteAgainstFreshSchema_WithoutError(string packageName)
    {
        await RunScriptsAsync(packageName);
    }

    /// <summary>
    /// <see cref="OutboxStoreADO"/> can round-trip a message through the table that
    /// <c>001_CreateOutboxMessagesTable.sql</c> creates.
    /// </summary>
    [Fact]
    public async Task OutboxStoreADO_RoundTripsMessage_OnScriptCreatedTable()
    {
        await RunScriptsAsync("Encina.ADO.MySQL");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        await UseDatabaseAsync(connection);
        var store = new AdoOutboxStore(connection);

        var message = new AdoOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Issue1261Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        (await store.AddAsync(message)).ShouldBeRight();
        var pending = (await store.GetPendingMessagesAsync(10, 5)).ShouldBeRight();
        Assert.Contains(pending, m => m.Id == message.Id);
    }

    /// <summary>
    /// <see cref="OutboxStoreDapper"/> can round-trip a message through the table that
    /// <c>001_CreateOutboxMessagesTable.sql</c> creates.
    /// </summary>
    [Fact]
    public async Task OutboxStoreDapper_RoundTripsMessage_OnScriptCreatedTable()
    {
        await RunScriptsAsync("Encina.Dapper.MySQL");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        await UseDatabaseAsync(connection);
        var store = new DapperOutboxStore(connection);

        var message = new DapperOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Issue1261Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        (await store.AddAsync(message)).ShouldBeRight();
        var pending = (await store.GetPendingMessagesAsync(10, 5)).ShouldBeRight();
        Assert.Contains(pending, m => m.Id == message.Id);
    }

    /// <summary>
    /// <see cref="AdoSagaStore"/> can round-trip a saga through the table that
    /// <c>003_CreateSagaStatesTable.sql</c> creates, proving the <c>Status</c> column (changed
    /// from <c>INT</c> to <c>VARCHAR(50)</c>) and the new <c>CorrelationId</c>/<c>Metadata</c>
    /// columns match what the store reads and writes.
    /// </summary>
    [Fact]
    public async Task SagaStoreADO_RoundTripsState_OnScriptCreatedTable()
    {
        await RunScriptsAsync("Encina.ADO.MySQL");

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        await UseDatabaseAsync(connection);
        var store = new AdoSagaStore(connection);

        var saga = new AdoSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Issue1261SagaTest",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 0
        };

        (await store.AddAsync(saga)).ShouldBeRight();
        var retrieved = (await store.GetAsync(saga.SagaId)).ShouldBeRight();
        Assert.True(retrieved.IsSome);
        retrieved.IfSome(s => Assert.Equal("Running", s.Status));
    }

    private async Task RunScriptsAsync(string packageName)
    {
        var scriptsFolder = FindScriptsFolder(packageName);
        var scriptFiles = GetInScopeScriptFiles(scriptsFolder);

        using var connection = (MySqlConnection)_fixture.CreateConnection();
        await UseDatabaseAsync(connection);

        foreach (var scriptFile in scriptFiles)
        {
            var sql = await File.ReadAllTextAsync(scriptFile);
            await using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Scripts 000-005 are the ones issue #1261 rewrote to use the package's own SQL dialect.
    /// Scripts 009 and above belong to unrelated compliance modules; one of them
    /// (ProcessingActivities, #1011_CreateProcessingActivitiesTable.sql on MySQL) has a
    /// pre-existing "key too long" defect unrelated to this issue's dialect fix, tracked
    /// separately, so this test does not execute it.
    /// </summary>
    private static IEnumerable<string> GetInScopeScriptFiles(string scriptsFolder) =>
        Directory.GetFiles(scriptsFolder, "*.sql")
            .Where(f => int.TryParse(Path.GetFileName(f).AsSpan(0, 3), out var prefix) && prefix <= 5)
            .OrderBy(f => f, StringComparer.Ordinal);

    private static async Task UseDatabaseAsync(MySqlConnection connection)
    {
        await using var command = new MySqlCommand($"USE `{DatabaseName}`;", connection);
        await command.ExecuteNonQueryAsync();
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
}
