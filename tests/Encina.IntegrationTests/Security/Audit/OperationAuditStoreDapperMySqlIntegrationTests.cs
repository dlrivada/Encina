using System.Data;
using Encina.Dapper.MySQL.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Integration tests for <see cref="OperationAuditStoreDapper"/> with MySQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("Dapper-MySQL")]
public class OperationAuditStoreDapperMySqlIntegrationTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;
    private IDbConnection _connection = null!;
    private OperationAuditStoreDapper _store = null!;

    public OperationAuditStoreDapperMySqlIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is MySqlConnection mysqlConnection)
            await CreateAuditSchemaAsync(mysqlConnection);

        _store = new OperationAuditStoreDapper(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateAuditSchemaAsync(MySqlConnection connection)
    {
        // The table comes from the script the package ships, so the test proves that DDL.
        var sql = ShippedSqlScript.Read("Encina.Dapper.MySQL", ShippedSqlScript.OperationAuditEntries);
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is MySqlConnection mysqlConnection)
        {
            await using var command = new MySqlCommand("DELETE FROM `OperationAuditEntries`;", mysqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static OperationAuditEntry CreateTestEntry(
        string? userId = "test-user",
        string entityType = "Order",
        string? entityId = null,
        AuditOutcome outcome = AuditOutcome.Success,
        DateTime? timestampUtc = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new OperationAuditEntry
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
        var entry = new OperationAuditEntry
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
        await _store.RecordAsync(new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            Action = "Start",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(50),
            Metadata = new Dictionary<string, object?>()
        });
        await _store.RecordAsync(new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            Action = "Complete",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = DateTime.UtcNow.AddSeconds(1),
            StartedAtUtc = now.AddSeconds(1),
            CompletedAtUtc = now.AddSeconds(1).AddMilliseconds(50),
            Metadata = new Dictionary<string, object?>()
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

        var result = await _store.QueryAsync(new OperationAuditQuery { PageNumber = 1, PageSize = 10 });
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

        var query = new OperationAuditQuery { UserId = "user-1", Outcome = AuditOutcome.Failure };
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

        var query = new OperationAuditQuery { EntityType = "Order" };
        var result = await _store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult => pagedResult.Items.Count.ShouldBe(2));
    }

    [Fact]
    public async Task QueryAsync_WithDurationFilter_FiltersBeforePagingAndCountsTheFilteredSet()
    {
        await ClearDataAsync();
        var now = DateTime.UtcNow;

        // 5 fast entries are the newest and 3 slow ones are older: filtering after paging would return an empty first page
        for (var i = 0; i < 3; i++)
        {
            var slow = CreateTestEntry(timestampUtc: now.AddMinutes(-20 - i));
            await _store.RecordAsync(slow with { CompletedAtUtc = slow.StartedAtUtc.AddMilliseconds(500) });
        }

        for (var i = 0; i < 5; i++)
        {
            var fast = CreateTestEntry(timestampUtc: now.AddMinutes(-i));
            await _store.RecordAsync(fast with { CompletedAtUtc = fast.StartedAtUtc.AddMilliseconds(10) });
        }

        var slowPage1 = await _store.QueryAsync(new OperationAuditQuery { MinDuration = TimeSpan.FromMilliseconds(200), PageNumber = 1, PageSize = 2 });
        var slowPage2 = await _store.QueryAsync(new OperationAuditQuery { MinDuration = TimeSpan.FromMilliseconds(200), PageNumber = 2, PageSize = 2 });
        var fastOnly = await _store.QueryAsync(new OperationAuditQuery { MaxDuration = TimeSpan.FromMilliseconds(100), PageNumber = 1, PageSize = 10 });

        slowPage1.IsRight.ShouldBeTrue();
        slowPage1.IfRight(page =>
        {
            page.TotalCount.ShouldBe(3);
            page.TotalPages.ShouldBe(2);
            page.Items.Count.ShouldBe(2);
            page.Items.ShouldAllBe(e => e.Duration >= TimeSpan.FromMilliseconds(200));
        });
        slowPage2.IfRight(page => page.Items.Count.ShouldBe(1));
        fastOnly.IfRight(page =>
        {
            page.TotalCount.ShouldBe(5);
            page.Items.ShouldAllBe(e => e.Duration <= TimeSpan.FromMilliseconds(100));
        });
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
