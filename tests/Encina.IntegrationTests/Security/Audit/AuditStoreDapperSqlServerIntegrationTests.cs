using System.Data;
using Encina.Dapper.SqlServer.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Integration tests for <see cref="AuditStoreDapper"/> with SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public class AuditStoreDapperSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbConnection _connection = null!;
    private AuditStoreDapper _store = null!;

    public AuditStoreDapperSqlServerIntegrationTests(SqlServerFixture fixture)
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

        _store = new AuditStoreDapper(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateAuditSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SecurityAuditEntries')
            CREATE TABLE SecurityAuditEntries (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                CorrelationId NVARCHAR(256) NOT NULL,
                UserId NVARCHAR(256) NULL,
                TenantId NVARCHAR(128) NULL,
                [Action] NVARCHAR(128) NOT NULL,
                EntityType NVARCHAR(256) NOT NULL,
                EntityId NVARCHAR(256) NULL,
                Outcome INT NOT NULL,
                ErrorMessage NVARCHAR(2048) NULL,
                TimestampUtc DATETIME2 NOT NULL,
                StartedAtUtc DATETIMEOFFSET NOT NULL,
                CompletedAtUtc DATETIMEOFFSET NOT NULL,
                IpAddress NVARCHAR(45) NULL,
                UserAgent NVARCHAR(512) NULL,
                RequestPayloadHash NVARCHAR(64) NULL,
                RequestPayload NVARCHAR(MAX) NULL,
                ResponsePayload NVARCHAR(MAX) NULL,
                Metadata NVARCHAR(MAX) NULL
            );
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand("DELETE FROM SecurityAuditEntries;", sqlConnection);
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

        var queryResult = await _store.GetByEntityAsync(entry.EntityType, entry.EntityId);
        queryResult.IsRight.ShouldBeTrue();
        queryResult.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task RecordAsync_EntryWithAllFields_ShouldPersistAllFields()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-dapper-ss",
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

        var queryResult = await _store.GetByCorrelationIdAsync("test-correlation-dapper-ss");
        queryResult.IsRight.ShouldBeTrue();
        queryResult.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            var retrieved = entries[0];
            retrieved.Outcome.ShouldBe(AuditOutcome.Failure);
            retrieved.ErrorMessage.ShouldBe("Test error message");
            retrieved.IpAddress.ShouldBe("192.168.1.100");
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
    public async Task GetByCorrelationIdAsync_ShouldReturnEntries()
    {
        await ClearDataAsync();
        var correlationId = "corr-dapper-ss";
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
        result.IfRight(pagedResult =>
        {
            pagedResult.Items.ShouldHaveSingleItem();
            pagedResult.Items[0].Id.ShouldBe(entry2.Id);
        });
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
        result.IfRight(pagedResult =>
        {
            pagedResult.Items.Count.ShouldBe(2);
            pagedResult.Items.ShouldAllBe(e => e.EntityType == "Order");
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
