using System.Data;
using Encina.ADO.Sqlite.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;

namespace Encina.IntegrationTests.Security.Audit.ReadAudit;

/// <summary>
/// Integration tests for <see cref="ReadAuditStoreADO"/> with SQLite.
/// Verifies that the ADO.NET SQLite read audit store correctly persists,
/// queries, and purges read audit entries against a real database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("ADO-Sqlite")]
public class ReadAuditStoreADOSqliteIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private IDbConnection _connection = null!;
    private ReadAuditStoreADO _store = null!;

    public ReadAuditStoreADOSqliteIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Create schema before each test run
        if (_fixture.CreateConnection() is SqliteConnection schemaConnection)
        {
            await CreateReadAuditSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        _store = new ReadAuditStoreADO(_connection);
    }

    public ValueTask DisposeAsync()
    {
        // Do NOT dispose the connection — it's the shared SQLite in-memory connection owned by the fixture.
        return ValueTask.CompletedTask;
    }

    #region Schema Setup

    private static async Task CreateReadAuditSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS ReadAuditEntries;
            CREATE TABLE ReadAuditEntries (
                Id TEXT PRIMARY KEY,
                EntityType TEXT NOT NULL,
                EntityId TEXT,
                UserId TEXT,
                TenantId TEXT,
                AccessedAtUtc TEXT NOT NULL,
                CorrelationId TEXT,
                Purpose TEXT,
                AccessMethod INTEGER NOT NULL,
                EntityCount INTEGER NOT NULL,
                Metadata TEXT
            );
            """;

        await using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_fixture.CreateConnection() is SqliteConnection sqliteConnection)
        {
            await using var command = new SqliteCommand(
                "DELETE FROM ReadAuditEntries;",
                sqliteConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    #endregion

    #region Test Data Helper

    private static ReadAuditEntry CreateTestEntry(
        Guid? id = null,
        string entityType = "Patient",
        string? entityId = "PAT-001",
        string? userId = "user-1",
        string? tenantId = null,
        DateTimeOffset? accessedAtUtc = null,
        string? correlationId = null,
        string? purpose = null,
        ReadAccessMethod accessMethod = ReadAccessMethod.Repository,
        int entityCount = 1) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            TenantId = tenantId,
            AccessedAtUtc = accessedAtUtc ?? DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            Purpose = purpose,
            AccessMethod = accessMethod,
            EntityCount = entityCount
        };

    #endregion

    #region LogReadAsync Tests

    [Fact]
    public async Task LogReadAsync_ValidEntry_ShouldPersist()
    {
        // Arrange
        await ClearDataAsync();
        var entry = CreateTestEntry();

        // Act
        var result = await _store.LogReadAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue("LogReadAsync should succeed for a valid entry");
    }

    [Fact]
    public async Task LogReadAsync_MultipleEntries_ShouldAllPersist()
    {
        // Arrange
        await ClearDataAsync();
        var entry1 = CreateTestEntry(entityId: "PAT-001");
        var entry2 = CreateTestEntry(entityId: "PAT-002");
        var entry3 = CreateTestEntry(entityId: "PAT-003");

        // Act
        await _store.LogReadAsync(entry1);
        await _store.LogReadAsync(entry2);
        await _store.LogReadAsync(entry3);

        // Assert
        var historyResult = await _store.GetAccessHistoryAsync("Patient", "PAT-001");
        historyResult.IsRight.ShouldBeTrue();
        historyResult.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    #endregion

    #region GetAccessHistoryAsync Tests

    [Fact]
    public async Task GetAccessHistoryAsync_ExistingEntity_ShouldReturnEntries()
    {
        // Arrange
        await ClearDataAsync();
        var entry = CreateTestEntry(entityType: "Patient", entityId: "PAT-100");
        await _store.LogReadAsync(entry);

        // Act
        var result = await _store.GetAccessHistoryAsync("Patient", "PAT-100");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].EntityType.ShouldBe("Patient");
            entries[0].EntityId.ShouldBe("PAT-100");
        });
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        await ClearDataAsync();

        // Act
        var result = await _store.GetAccessHistoryAsync("Patient", "NONEXISTENT");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldBeEmpty());
    }

    [Fact]
    public async Task GetAccessHistoryAsync_MultipleEntries_ShouldReturnOrderedByDateDesc()
    {
        // Arrange
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        var older = CreateTestEntry(entityId: "PAT-200", accessedAtUtc: now.AddHours(-2));
        var newer = CreateTestEntry(entityId: "PAT-200", accessedAtUtc: now.AddHours(-1));

        await _store.LogReadAsync(older);
        await _store.LogReadAsync(newer);

        // Act
        var result = await _store.GetAccessHistoryAsync("Patient", "PAT-200");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.Count.ShouldBe(2);
            entries[0].AccessedAtUtc.ShouldBeGreaterThan(entries[1].AccessedAtUtc);
        });
    }

    #endregion

    #region GetUserAccessHistoryAsync Tests

    [Fact]
    public async Task GetUserAccessHistoryAsync_WithinDateRange_ShouldReturnEntries()
    {
        // Arrange
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        var entry = CreateTestEntry(userId: "user-history", accessedAtUtc: now);
        await _store.LogReadAsync(entry);

        // Act
        var result = await _store.GetUserAccessHistoryAsync("user-history", now.AddHours(-1), now.AddHours(1));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_OutsideDateRange_ShouldReturnEmpty()
    {
        // Arrange
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        var entry = CreateTestEntry(userId: "user-outside", accessedAtUtc: now.AddDays(-30));
        await _store.LogReadAsync(entry);

        // Act
        var result = await _store.GetUserAccessHistoryAsync("user-outside", now.AddDays(-1), now);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldBeEmpty());
    }

    #endregion

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_NoFilters_ShouldReturnAllEntries()
    {
        // Arrange
        await ClearDataAsync();
        for (var i = 0; i < 5; i++)
        {
            await _store.LogReadAsync(CreateTestEntry(entityId: $"PAT-Q{i}"));
        }

        var query = ReadAuditQuery.Builder().Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.TotalCount.ShouldBe(5);
            paged.Items.Count.ShouldBe(5);
        });
    }

    [Fact]
    public async Task QueryAsync_WithUserFilter_ShouldFilterResults()
    {
        // Arrange
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(userId: "user-A", entityId: "E1"));
        await _store.LogReadAsync(CreateTestEntry(userId: "user-B", entityId: "E2"));
        await _store.LogReadAsync(CreateTestEntry(userId: "user-A", entityId: "E3"));

        var query = ReadAuditQuery.Builder().ForUser("user-A").Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.TotalCount.ShouldBe(2);
            paged.Items.ShouldAllBe(e => e.UserId == "user-A");
        });
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldRespectPageSize()
    {
        // Arrange
        await ClearDataAsync();
        for (var i = 0; i < 10; i++)
        {
            await _store.LogReadAsync(CreateTestEntry(entityId: $"PAT-P{i}"));
        }

        var query = ReadAuditQuery.Builder().OnPage(1).WithPageSize(3).Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(3);
            paged.TotalCount.ShouldBe(10);
            paged.TotalPages.ShouldBe(4); // ceil(10/3) = 4
            paged.HasNextPage.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task QueryAsync_WithEntityTypeFilter_ShouldFilterResults()
    {
        // Arrange
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(entityType: "Patient", entityId: "P1"));
        await _store.LogReadAsync(CreateTestEntry(entityType: "Order", entityId: "O1"));
        await _store.LogReadAsync(CreateTestEntry(entityType: "Patient", entityId: "P2"));

        var query = ReadAuditQuery.Builder().ForEntityType("Patient").Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.TotalCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task QueryAsync_WithAccessMethodFilter_ShouldFilterResults()
    {
        // Arrange
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(accessMethod: ReadAccessMethod.Repository, entityId: "R1"));
        await _store.LogReadAsync(CreateTestEntry(accessMethod: ReadAccessMethod.Api, entityId: "A1"));
        await _store.LogReadAsync(CreateTestEntry(accessMethod: ReadAccessMethod.Export, entityId: "E1"));

        var query = ReadAuditQuery.Builder().WithAccessMethod(ReadAccessMethod.Api).Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.TotalCount.ShouldBe(1);
            paged.Items[0].AccessMethod.ShouldBe(ReadAccessMethod.Api);
        });
    }

    [Fact]
    public async Task QueryAsync_WithDateRange_ShouldFilterResults()
    {
        // Arrange
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(entityId: "OLD", accessedAtUtc: now.AddDays(-10)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "RECENT", accessedAtUtc: now.AddHours(-1)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "FUTURE", accessedAtUtc: now.AddDays(1)));

        var query = ReadAuditQuery.Builder()
            .InDateRange(now.AddDays(-2), now)
            .Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.TotalCount.ShouldBe(1);
            paged.Items[0].EntityId.ShouldBe("RECENT");
        });
    }

    #endregion

    #region PurgeEntriesAsync Tests

    [Fact]
    public async Task PurgeEntriesAsync_OldEntries_ShouldRemoveOnlyOldOnes()
    {
        // Arrange
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(entityId: "OLD1", accessedAtUtc: now.AddDays(-100)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "OLD2", accessedAtUtc: now.AddDays(-50)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "RECENT", accessedAtUtc: now));

        // Act
        var result = await _store.PurgeEntriesAsync(now.AddDays(-30));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBe(2, "Should purge exactly 2 old entries"));

        // Verify the recent entry survived
        var remaining = await _store.QueryAsync(ReadAuditQuery.Builder().Build());
        remaining.IsRight.ShouldBeTrue();
        remaining.IfRight(paged =>
        {
            paged.TotalCount.ShouldBe(1);
            paged.Items[0].EntityId.ShouldBe("RECENT");
        });
    }

    [Fact]
    public async Task PurgeEntriesAsync_NoOldEntries_ShouldReturnZero()
    {
        // Arrange
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: DateTimeOffset.UtcNow));

        // Act
        var result = await _store.PurgeEntriesAsync(DateTimeOffset.UtcNow.AddDays(-365));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBe(0));
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public async Task LogRead_ThenQuery_ShouldPreserveAllFields()
    {
        // Arrange
        await ClearDataAsync();
        var entry = CreateTestEntry(
            entityType: "FinancialRecord",
            entityId: "FR-999",
            userId: "auditor-1",
            tenantId: "tenant-A",
            correlationId: "corr-123",
            purpose: "Annual audit review",
            accessMethod: ReadAccessMethod.Export,
            entityCount: 42);

        // Act
        await _store.LogReadAsync(entry);
        var result = await _store.GetAccessHistoryAsync("FinancialRecord", "FR-999");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            var retrieved = entries[0];
            retrieved.Id.ShouldBe(entry.Id);
            retrieved.EntityType.ShouldBe("FinancialRecord");
            retrieved.EntityId.ShouldBe("FR-999");
            retrieved.UserId.ShouldBe("auditor-1");
            retrieved.TenantId.ShouldBe("tenant-A");
            retrieved.CorrelationId.ShouldBe("corr-123");
            retrieved.Purpose.ShouldBe("Annual audit review");
            retrieved.AccessMethod.ShouldBe(ReadAccessMethod.Export);
            retrieved.EntityCount.ShouldBe(42);
        });
    }

    #endregion
}
