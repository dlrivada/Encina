using System.Data;
using Encina.Dapper.Sqlite.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;

namespace Encina.IntegrationTests.Security.Audit.ReadAudit;

/// <summary>
/// Integration tests for <see cref="ReadAuditStoreDapper"/> with SQLite.
/// Verifies that the Dapper SQLite read audit store correctly persists,
/// queries, and purges read audit entries against a real database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("Dapper-Sqlite")]
public class ReadAuditStoreDapperSqliteIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private IDbConnection _connection = null!;
    private ReadAuditStoreDapper _store = null!;

    public ReadAuditStoreDapperSqliteIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (_fixture.CreateConnection() is SqliteConnection schemaConnection)
        {
            await CreateReadAuditSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        _store = new ReadAuditStoreDapper(_connection);
    }

    public ValueTask DisposeAsync()
    {
        // Do NOT dispose _connection — it's the shared SQLite in-memory connection owned by the fixture.
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
        if (_connection is SqliteConnection sqliteConnection)
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
            paged.HasNextPage.ShouldBeTrue();
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
        await _store.LogReadAsync(CreateTestEntry(entityId: "OLD", accessedAtUtc: now.AddDays(-100)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "RECENT", accessedAtUtc: now));

        // Act
        var result = await _store.PurgeEntriesAsync(now.AddDays(-30));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBe(1));

        var remaining = await _store.QueryAsync(ReadAuditQuery.Builder().Build());
        remaining.IsRight.ShouldBeTrue();
        remaining.IfRight(paged => paged.TotalCount.ShouldBe(1));
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
