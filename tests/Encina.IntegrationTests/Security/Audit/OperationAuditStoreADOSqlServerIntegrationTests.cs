using System.Data;
using Encina.ADO.SqlServer.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Integration tests for <see cref="OperationAuditStoreADO"/> with SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public class OperationAuditStoreADOSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbConnection _connection = null!;
    private OperationAuditStoreADO _store = null!;

    public OperationAuditStoreADOSqlServerIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is SqlConnection sqlConnection)
        {
            await CreateAuditSchemaAsync(sqlConnection);
        }

        _store = new OperationAuditStoreADO(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateAuditSchemaAsync(SqlConnection connection)
    {
        // The table comes from the script the package ships, so the test proves that DDL.
        var sql = ShippedSqlScript.Read("Encina.ADO.SqlServer", ShippedSqlScript.OperationAuditEntries);
        foreach (var batch in ShippedSqlScript.SplitBatches(sql))
        {
            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand(
                "DELETE FROM OperationAuditEntries;",
                sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static OperationAuditEntry CreateTestEntry(
        string? userId = "test-user",
        string? tenantId = null,
        string action = "Create",
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
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId ?? Guid.NewGuid().ToString(),
            Outcome = outcome,
            TimestampUtc = timestampUtc ?? DateTime.UtcNow,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(100),
            Metadata = new Dictionary<string, object?>()
        };
    }

    #region RecordAsync Tests

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldPersist()
    {
        // Arrange
        await ClearDataAsync();
        var entry = CreateTestEntry();

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue();

        var queryResult = await _store.GetByEntityAsync(entry.EntityType, entry.EntityId);
        queryResult.IsRight.ShouldBeTrue();
        queryResult.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Id.ShouldBe(entry.Id);
            entries[0].UserId.ShouldBe(entry.UserId);
            entries[0].Action.ShouldBe(entry.Action);
        });
    }

    [Fact]
    public async Task RecordAsync_EntryWithAllFields_ShouldPersistAllFields()
    {
        // Arrange
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        var entry = new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-123",
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
            Metadata = new Dictionary<string, object?>
            {
                ["key1"] = "value1",
                ["key2"] = 42
            }
        };

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue();

        var queryResult = await _store.GetByCorrelationIdAsync("test-correlation-123");
        queryResult.IsRight.ShouldBeTrue();
        queryResult.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            var retrieved = entries[0];
            retrieved.UserId.ShouldBe("user-456");
            retrieved.TenantId.ShouldBe("tenant-789");
            retrieved.Outcome.ShouldBe(AuditOutcome.Failure);
            retrieved.ErrorMessage.ShouldBe("Test error message");
            retrieved.IpAddress.ShouldBe("192.168.1.100");
            retrieved.UserAgent.ShouldBe("TestAgent/1.0");
            retrieved.RequestPayloadHash.ShouldBe("abc123hash");
            retrieved.RequestPayload.ShouldBe("""{"name":"test"}""");
            retrieved.ResponsePayload.ShouldBe("""{"result":"error"}""");
        });
    }

    #endregion

    #region GetByEntityAsync Tests

    [Fact]
    public async Task GetByEntityAsync_WithEntityType_ShouldReturnMatchingEntries()
    {
        // Arrange
        await ClearDataAsync();
        var entry1 = CreateTestEntry(entityType: "Order");
        var entry2 = CreateTestEntry(entityType: "Order");
        var entry3 = CreateTestEntry(entityType: "Product");

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act
        var result = await _store.GetByEntityAsync("Order", null);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.Count.ShouldBe(2);
            entries.ShouldAllBe(e => e.EntityType == "Order");
        });
    }

    [Fact]
    public async Task GetByEntityAsync_WithEntityId_ShouldReturnSpecificEntry()
    {
        // Arrange
        await ClearDataAsync();
        var entry1 = CreateTestEntry(entityType: "Order", entityId: "order-1");
        var entry2 = CreateTestEntry(entityType: "Order", entityId: "order-2");

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Act
        var result = await _store.GetByEntityAsync("Order", "order-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].EntityId.ShouldBe("order-1");
        });
    }

    #endregion

    #region GetByUserAsync Tests

    [Fact]
    public async Task GetByUserAsync_WithUserId_ShouldReturnUserEntries()
    {
        // Arrange
        await ClearDataAsync();
        var entry1 = CreateTestEntry(userId: "user-1");
        var entry2 = CreateTestEntry(userId: "user-1");
        var entry3 = CreateTestEntry(userId: "user-2");

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act
        var result = await _store.GetByUserAsync("user-1", null, null);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.Count.ShouldBe(2);
            entries.ShouldAllBe(e => e.UserId == "user-1");
        });
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRange_ShouldFilterByTimestamp()
    {
        // Arrange
        await ClearDataAsync();
        var oldEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-1));

        await _store.RecordAsync(oldEntry);
        await _store.RecordAsync(recentEntry);

        // Act
        var result = await _store.GetByUserAsync("user-1", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Id.ShouldBe(recentEntry.Id);
        });
    }

    #endregion

    #region GetByCorrelationIdAsync Tests

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnAllEntriesWithSameCorrelation()
    {
        // Arrange
        await ClearDataAsync();
        var correlationId = "correlation-abc";
        var now = DateTimeOffset.UtcNow;

        var entry1 = new OperationAuditEntry
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
        };

        var entry2 = new OperationAuditEntry
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
        };

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Act
        var result = await _store.GetByCorrelationIdAsync(correlationId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.Count.ShouldBe(2);
            entries.ShouldAllBe(e => e.CorrelationId == correlationId);
        });
    }

    #endregion

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await ClearDataAsync();
        for (var i = 0; i < 25; i++)
        {
            await _store.RecordAsync(CreateTestEntry());
        }

        // Act
        var query = new OperationAuditQuery { PageNumber = 1, PageSize = 10 };
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult =>
        {
            pagedResult.TotalCount.ShouldBe(25);
            pagedResult.Items.Count.ShouldBe(10);
            pagedResult.TotalPages.ShouldBe(3);
            pagedResult.HasNextPage.ShouldBeTrue();
            pagedResult.HasPreviousPage.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task QueryAsync_WithFilters_ShouldReturnMatchingEntries()
    {
        // Arrange
        await ClearDataAsync();
        var entry1 = CreateTestEntry(userId: "user-1", action: "Create", outcome: AuditOutcome.Success);
        var entry2 = CreateTestEntry(userId: "user-1", action: "Update", outcome: AuditOutcome.Failure);
        var entry3 = CreateTestEntry(userId: "user-2", action: "Create", outcome: AuditOutcome.Success);

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act
        var query = new OperationAuditQuery { UserId = "user-1", Outcome = AuditOutcome.Failure };
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult =>
        {
            pagedResult.Items.ShouldHaveSingleItem();
            pagedResult.Items[0].Id.ShouldBe(entry2.Id);
        });
    }

    [Fact]
    public async Task QueryAsync_WithEntityTypeFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        await ClearDataAsync();
        var entry1 = CreateTestEntry(entityType: "Order");
        var entry2 = CreateTestEntry(entityType: "Product");
        var entry3 = CreateTestEntry(entityType: "Order");

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry3);

        // Act
        var query = new OperationAuditQuery { EntityType = "Order" };
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult =>
        {
            pagedResult.Items.Count.ShouldBe(2);
            pagedResult.Items.ShouldAllBe(e => e.EntityType == "Order");
        });
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
        slowPage2.IsRight.ShouldBeTrue();
        fastOnly.IsRight.ShouldBeTrue();
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

    #endregion

    #region PurgeEntriesAsync Tests

    [Fact]
    public async Task PurgeEntriesAsync_ShouldDeleteOldEntries()
    {
        // Arrange
        await ClearDataAsync();
        var oldEntry = CreateTestEntry(timestampUtc: DateTime.UtcNow.AddDays(-100));
        var recentEntry = CreateTestEntry(timestampUtc: DateTime.UtcNow);

        await _store.RecordAsync(oldEntry);
        await _store.RecordAsync(recentEntry);

        // Act
        var result = await _store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));

        var queryResult = await _store.QueryAsync(new OperationAuditQuery());
        queryResult.IfRight(pagedResult =>
        {
            pagedResult.Items.ShouldHaveSingleItem();
            pagedResult.Items[0].Id.ShouldBe(recentEntry.Id);
        });
    }

    [Fact]
    public async Task PurgeEntriesAsync_WithNoOldEntries_ShouldReturnZero()
    {
        // Arrange
        await ClearDataAsync();
        var recentEntry = CreateTestEntry(timestampUtc: DateTime.UtcNow);
        await _store.RecordAsync(recentEntry);

        // Act
        var result = await _store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    #endregion
}
