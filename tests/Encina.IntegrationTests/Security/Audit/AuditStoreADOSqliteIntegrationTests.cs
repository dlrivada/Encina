using System.Data;
using Encina.ADO.Sqlite.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Integration tests for <see cref="AuditStoreADO"/> with SQLite.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("ADO-Sqlite")]
public class AuditStoreADOSqliteIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private IDbConnection _connection = null!;
    private AuditStoreADO _store = null!;

    public AuditStoreADOSqliteIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Create test schema
        if (_fixture.CreateConnection() is SqliteConnection schemaConnection)
        {
            await CreateAuditSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        _store = new AuditStoreADO(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        // Do NOT dispose _connection - it's the shared SQLite in-memory connection owned by the fixture.
        await _fixture.ClearAllDataAsync();
    }

    private static async Task CreateAuditSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS SecurityAuditEntries;
            CREATE TABLE SecurityAuditEntries (
                Id TEXT PRIMARY KEY,
                CorrelationId TEXT NOT NULL,
                UserId TEXT,
                TenantId TEXT,
                Action TEXT NOT NULL,
                EntityType TEXT NOT NULL,
                EntityId TEXT,
                Outcome INTEGER NOT NULL,
                ErrorMessage TEXT,
                TimestampUtc TEXT NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                CompletedAtUtc TEXT NOT NULL,
                IpAddress TEXT,
                UserAgent TEXT,
                RequestPayloadHash TEXT,
                RequestPayload TEXT,
                ResponsePayload TEXT,
                Metadata TEXT
            );
            """;

        await using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqliteConnection sqliteConnection)
        {
            await using var command = new SqliteCommand(
                "DELETE FROM SecurityAuditEntries;",
                sqliteConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static AuditEntry CreateTestEntry(
        string? userId = "test-user",
        string? tenantId = null,
        string action = "Create",
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

        // Verify by querying back
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
        var entry = new AuditEntry
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

        // Act - Query last 5 days
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

        var entry1 = new AuditEntry
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

        var entry2 = new AuditEntry
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
        var query = new AuditQuery { PageNumber = 1, PageSize = 10 };
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

        // Act - Query for user-1 with failures
        var query = new AuditQuery { UserId = "user-1", Outcome = AuditOutcome.Failure };
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
        var query = new AuditQuery { EntityType = "Order" };
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult =>
        {
            pagedResult.Items.Count.ShouldBe(2);
            pagedResult.Items.ShouldAllBe(e => e.EntityType == "Order");
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

        // Act - Purge entries older than 30 days
        var result = await _store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));

        // Verify only recent entry remains
        var queryResult = await _store.QueryAsync(new AuditQuery());
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

        // Act - Purge entries older than 30 days
        var result = await _store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    #endregion
}
