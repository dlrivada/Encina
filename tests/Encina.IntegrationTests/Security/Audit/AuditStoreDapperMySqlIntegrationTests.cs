using System.Data;
using Encina.Dapper.MySQL.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Integration tests for <see cref="AuditStoreDapper"/> with MySQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("Dapper-MySQL")]
public class AuditStoreDapperMySqlIntegrationTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;
    private IDbConnection _connection = null!;
    private AuditStoreDapper _store = null!;

    public AuditStoreDapperMySqlIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is MySqlConnection mysqlConnection)
            await CreateAuditSchemaAsync(mysqlConnection);

        _store = new AuditStoreDapper(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateAuditSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS `SecurityAuditEntries` (
                `Id` CHAR(36) PRIMARY KEY,
                `CorrelationId` VARCHAR(256) NOT NULL,
                `UserId` VARCHAR(256) NULL,
                `TenantId` VARCHAR(128) NULL,
                `Action` VARCHAR(128) NOT NULL,
                `EntityType` VARCHAR(256) NOT NULL,
                `EntityId` VARCHAR(256) NULL,
                `Outcome` INT NOT NULL,
                `ErrorMessage` VARCHAR(2048) NULL,
                `TimestampUtc` DATETIME(6) NOT NULL,
                `StartedAtUtc` DATETIME(6) NOT NULL,
                `CompletedAtUtc` DATETIME(6) NOT NULL,
                `IpAddress` VARCHAR(45) NULL,
                `UserAgent` VARCHAR(512) NULL,
                `RequestPayloadHash` VARCHAR(64) NULL,
                `RequestPayload` LONGTEXT NULL,
                `ResponsePayload` LONGTEXT NULL,
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
            await using var command = new MySqlCommand("DELETE FROM `SecurityAuditEntries`;", mysqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static AuditEntry CreateTestEntry(
        string? userId = "test-user",
        string entityType = "Order",
        string? entityId = null,
        AuditOutcome outcome = AuditOutcome.Success,
        DateTime? timestampUtc = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString(),
            UserId = userId,
            Action = "Create",
            EntityType = entityType,
            EntityId = entityId ?? Guid.NewGuid().ToString(),
            Outcome = outcome,
            TimestampUtc = timestampUtc ?? DateTime.UtcNow,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(100),
            Metadata = new Dictionary<string, object?>()
        };
    }

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldPersist()
    {
        await ClearDataAsync();
        var entry = CreateTestEntry();
        var result = await _store.RecordAsync(entry);
        result.IsRight.ShouldBeTrue();

        var q = await _store.GetByEntityAsync(entry.EntityType, entry.EntityId);
        q.IsRight.ShouldBeTrue();
        q.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task RecordAsync_EntryWithAllFields_ShouldPersistAllFields()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-dapper-mysql",
            UserId = "user-456",
            TenantId = "tenant-789",
            Action = "Update",
            EntityType = "Product",
            EntityId = "prod-001",
            Outcome = AuditOutcome.Failure,
            ErrorMessage = "Test error message",
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(250),
            IpAddress = "192.168.1.100",
            UserAgent = "TestAgent/1.0",
            RequestPayloadHash = "abc123hash",
            RequestPayload = """{"name":"test"}""",
            ResponsePayload = """{"result":"error"}""",
            Metadata = new Dictionary<string, object?> { ["key1"] = "value1" }
        };

        var result = await _store.RecordAsync(entry);
        result.IsRight.ShouldBeTrue();

        var queryResult = await _store.GetByCorrelationIdAsync("test-correlation-dapper-mysql");
        queryResult.IsRight.ShouldBeTrue();
        queryResult.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Outcome.ShouldBe(AuditOutcome.Failure);
            entries[0].ErrorMessage.ShouldBe("Test error message");
        });
    }

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnMatchingEntries()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Product"));

        var result = await _store.GetByEntityAsync("Order", null);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBe(2));
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRange_ShouldFilter()
    {
        await ClearDataAsync();
        var oldEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-1));
        await _store.RecordAsync(oldEntry);
        await _store.RecordAsync(recentEntry);

        var result = await _store.GetByUserAsync("user-1", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task GetByEntityAsync_WithEntityId_ShouldReturnSpecificEntry()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(entityType: "Order", entityId: "order-1"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Order", entityId: "order-2"));

        var result = await _store.GetByEntityAsync("Order", "order-1");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].EntityId.ShouldBe("order-1");
        });
    }

    [Fact]
    public async Task GetByUserAsync_WithUserId_ShouldReturnUserEntries()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(userId: "user-1"));
        await _store.RecordAsync(CreateTestEntry(userId: "user-1"));
        await _store.RecordAsync(CreateTestEntry(userId: "user-2"));

        var result = await _store.GetByUserAsync("user-1", null, null);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBe(2));
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnEntries()
    {
        await ClearDataAsync();
        var correlationId = "corr-dapper-mysql";
        var now = DateTimeOffset.UtcNow;
        await _store.RecordAsync(new AuditEntry
        {
            Id = Guid.NewGuid(), CorrelationId = correlationId, Action = "Start",
            EntityType = "Order", Outcome = AuditOutcome.Success,
            TimestampUtc = DateTime.UtcNow, StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(50), Metadata = new Dictionary<string, object?>()
        });
        await _store.RecordAsync(new AuditEntry
        {
            Id = Guid.NewGuid(), CorrelationId = correlationId, Action = "Complete",
            EntityType = "Order", Outcome = AuditOutcome.Success,
            TimestampUtc = DateTime.UtcNow.AddSeconds(1), StartedAtUtc = now.AddSeconds(1),
            CompletedAtUtc = now.AddSeconds(1).AddMilliseconds(50), Metadata = new Dictionary<string, object?>()
        });

        var result = await _store.GetByCorrelationIdAsync(correlationId);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBe(2));
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnPagedResults()
    {
        await ClearDataAsync();
        for (var i = 0; i < 25; i++)
            await _store.RecordAsync(CreateTestEntry());

        var result = await _store.QueryAsync(new AuditQuery { PageNumber = 1, PageSize = 10 });
        result.IsRight.ShouldBeTrue();
        result.IfRight(p => { p.TotalCount.ShouldBe(25); p.Items.Count.ShouldBe(10); });
    }

    [Fact]
    public async Task QueryAsync_WithFilters_ShouldReturnMatchingEntries()
    {
        await ClearDataAsync();
        var entry2 = CreateTestEntry(userId: "user-1", outcome: AuditOutcome.Failure);
        await _store.RecordAsync(CreateTestEntry(userId: "user-1", outcome: AuditOutcome.Success));
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(CreateTestEntry(userId: "user-2", outcome: AuditOutcome.Success));

        var query = new AuditQuery { UserId = "user-1", Outcome = AuditOutcome.Failure };
        var result = await _store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult => pagedResult.Items.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task QueryAsync_WithEntityTypeFilter_ShouldReturnMatchingEntries()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Product"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));

        var query = new AuditQuery { EntityType = "Order" };
        var result = await _store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult => pagedResult.Items.Count.ShouldBe(2));
    }

    [Fact]
    public async Task PurgeEntriesAsync_ShouldDeleteOldEntries()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(timestampUtc: DateTime.UtcNow.AddDays(-100)));
        await _store.RecordAsync(CreateTestEntry(timestampUtc: DateTime.UtcNow));

        var result = await _store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));
    }

    [Fact]
    public async Task PurgeEntriesAsync_WithNoOldEntries_ShouldReturnZero()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(timestampUtc: DateTime.UtcNow));

        var result = await _store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }
}
