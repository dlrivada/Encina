using System.Data;
using Encina.Dapper.MySQL.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.ReadAudit;

/// <summary>
/// Integration tests for <see cref="ReadAuditStoreDapper"/> with MySQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("Dapper-MySQL")]
public class ReadAuditStoreDapperMySqlIntegrationTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;
    private IDbConnection _connection = null!;
    private ReadAuditStoreDapper _store = null!;

    public ReadAuditStoreDapperMySqlIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is MySqlConnection mysqlConnection)
            await CreateSchemaAsync(mysqlConnection);
        _store = new ReadAuditStoreDapper(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS `ReadAuditEntries` (
                `Id` CHAR(36) PRIMARY KEY,
                `EntityType` VARCHAR(256) NOT NULL,
                `EntityId` VARCHAR(256) NULL,
                `UserId` VARCHAR(256) NULL,
                `TenantId` VARCHAR(128) NULL,
                `AccessedAtUtc` DATETIME(6) NOT NULL,
                `CorrelationId` VARCHAR(256) NULL,
                `Purpose` VARCHAR(500) NULL,
                `AccessMethod` INT NOT NULL,
                `EntityCount` INT NOT NULL,
                `Metadata` LONGTEXT NULL
            );
            """;
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is MySqlConnection mysqlConnection)
        {
            await using var command = new MySqlCommand("DELETE FROM `ReadAuditEntries`;", mysqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static ReadAuditEntry CreateTestEntry(
        string entityType = "Patient", string? entityId = "PAT-001",
        string? userId = "user-1", DateTimeOffset? accessedAtUtc = null,
        ReadAccessMethod accessMethod = ReadAccessMethod.Repository, int entityCount = 1) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            AccessedAtUtc = accessedAtUtc ?? DateTimeOffset.UtcNow,
            AccessMethod = accessMethod,
            EntityCount = entityCount
        };

    [Fact]
    public async Task LogReadAsync_ValidEntry_ShouldPersist()
    {
        await ClearDataAsync();
        var result = await _store.LogReadAsync(CreateTestEntry());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAccessHistoryAsync_ShouldReturnEntries()
    {
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(entityId: "PAT-100"));
        var result = await _store.GetAccessHistoryAsync("Patient", "PAT-100");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_ShouldFilter()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(userId: "user-dmy", accessedAtUtc: now));
        var result = await _store.GetUserAccessHistoryAsync("user-dmy", now.AddHours(-1), now.AddHours(1));
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldWork()
    {
        await ClearDataAsync();
        for (var i = 0; i < 10; i++)
            await _store.LogReadAsync(CreateTestEntry(entityId: $"PAT-{i}"));
        var query = ReadAuditQuery.Builder().OnPage(1).WithPageSize(3).Build();
        var result = await _store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged => paged.Items.Count.ShouldBe(3));
    }

    [Fact]
    public async Task PurgeEntriesAsync_ShouldRemoveOld()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(entityId: "OLD", accessedAtUtc: now.AddDays(-100)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "RECENT", accessedAtUtc: now));
        var result = await _store.PurgeEntriesAsync(now.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBe(1));
    }
}
