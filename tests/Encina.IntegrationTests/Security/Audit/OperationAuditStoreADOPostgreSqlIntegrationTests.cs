using System.Data;
using Encina.ADO.PostgreSQL.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit;

/// <summary>
/// Integration tests for <see cref="OperationAuditStoreADO"/> with PostgreSQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public class OperationAuditStoreADOPostgreSqlIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private IDbConnection _connection = null!;
    private OperationAuditStoreADO _store = null!;

    public OperationAuditStoreADOPostgreSqlIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await CreateAuditSchemaAsync(npgsqlConnection);
        }

        _store = new OperationAuditStoreADO(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateAuditSchemaAsync(NpgsqlConnection connection)
    {
        // The table comes from the script the package ships, so the test proves that DDL.
        var sql = ShippedSqlScript.Read("Encina.ADO.PostgreSQL", ShippedSqlScript.OperationAuditEntries);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = new NpgsqlCommand(
                """DELETE FROM "OperationAuditEntries";""",
                npgsqlConnection);
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
        await ClearDataAsync();
        var entry = CreateTestEntry();

        var result = await _store.RecordAsync(entry);

        result.IsRight.ShouldBeTrue();

        var queryResult = await _store.GetByEntityAsync(entry.EntityType, entry.EntityId);
        queryResult.IsRight.ShouldBeTrue();
        queryResult.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Id.ShouldBe(entry.Id);
        });
    }

    [Fact]
    public async Task RecordAsync_EntryWithAllFields_ShouldPersistAllFields()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        var entry = new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-pg",
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

        var queryResult = await _store.GetByCorrelationIdAsync("test-correlation-pg");
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
        });
    }

    #endregion

    #region GetByEntityAsync Tests

    [Fact]
    public async Task GetByEntityAsync_WithEntityType_ShouldReturnMatchingEntries()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Product"));

        var result = await _store.GetByEntityAsync("Order", null);

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

    #endregion

    #region GetByUserAsync Tests

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
    public async Task GetByUserAsync_WithDateRange_ShouldFilterByTimestamp()
    {
        await ClearDataAsync();
        var oldEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-1));

        await _store.RecordAsync(oldEntry);
        await _store.RecordAsync(recentEntry);

        var result = await _store.GetByUserAsync("user-1", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);

        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Id.ShouldBe(recentEntry.Id);
        });
    }

    [Fact]
    public async Task GetByUserAsync_WithFromOnly_ShouldReturnEntriesAfterFrom()
    {
        await ClearDataAsync();
        var oldEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-1));

        await _store.RecordAsync(oldEntry);
        await _store.RecordAsync(recentEntry);

        var result = await _store.GetByUserAsync("user-1", DateTime.UtcNow.AddDays(-5), null);

        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Id.ShouldBe(recentEntry.Id);
        });
    }

    [Fact]
    public async Task GetByUserAsync_WithToOnly_ShouldReturnEntriesBeforeTo()
    {
        await ClearDataAsync();
        var oldEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "user-1", timestampUtc: DateTime.UtcNow.AddDays(-1));

        await _store.RecordAsync(oldEntry);
        await _store.RecordAsync(recentEntry);

        var result = await _store.GetByUserAsync("user-1", null, DateTime.UtcNow.AddDays(-5));

        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Id.ShouldBe(oldEntry.Id);
        });
    }

    #endregion

    #region GetByCorrelationIdAsync Tests

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnAllEntriesWithSameCorrelation()
    {
        await ClearDataAsync();
        var correlationId = "correlation-pg";
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

    #endregion

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnPagedResults()
    {
        await ClearDataAsync();
        for (var i = 0; i < 25; i++)
        {
            await _store.RecordAsync(CreateTestEntry());
        }

        var query = new OperationAuditQuery { PageNumber = 1, PageSize = 10 };
        var result = await _store.QueryAsync(query);

        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult =>
        {
            pagedResult.TotalCount.ShouldBe(25);
            pagedResult.Items.Count.ShouldBe(10);
            pagedResult.TotalPages.ShouldBe(3);
        });
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
        result.IfRight(pagedResult =>
        {
            pagedResult.Items.ShouldHaveSingleItem();
            pagedResult.Items[0].Id.ShouldBe(entry2.Id);
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
    public async Task QueryAsync_WithEntityTypeFilter_ShouldReturnMatchingEntries()
    {
        await ClearDataAsync();
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Product"));
        await _store.RecordAsync(CreateTestEntry(entityType: "Order"));

        var query = new OperationAuditQuery { EntityType = "Order" };
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
        var oldEntry = CreateTestEntry(timestampUtc: DateTime.UtcNow.AddDays(-100));
        var recentEntry = CreateTestEntry(timestampUtc: DateTime.UtcNow);

        await _store.RecordAsync(oldEntry);
        await _store.RecordAsync(recentEntry);

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

    #endregion

    #region Time zone handling

    private static DateTime TruncatedUtcNow()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task RecordAsync_NonUtcKinds_StoreTheSameInstant(DateTimeKind kind)
    {
        await ClearDataAsync();
        var instant = TruncatedUtcNow();
        var timestamp = kind == DateTimeKind.Local
            ? instant.ToLocalTime()
            : DateTime.SpecifyKind(instant, DateTimeKind.Unspecified);
        var started = new DateTimeOffset(instant.AddMilliseconds(-100)).ToOffset(TimeSpan.FromHours(5));
        var entry = CreateTestEntry(timestampUtc: timestamp) with { StartedAtUtc = started, CompletedAtUtc = started.AddMilliseconds(100) };

        var recorded = await _store.RecordAsync(entry);
        var read = await _store.GetByEntityAsync(entry.EntityType, entry.EntityId!);

        recorded.IsRight.ShouldBeTrue();
        read.IsRight.ShouldBeTrue();
        read.IfRight(entries =>
        {
            var stored = entries.ShouldHaveSingleItem();
            stored.TimestampUtc.ShouldBe(instant);
            stored.StartedAtUtc.UtcDateTime.ShouldBe(instant.AddMilliseconds(-100));
            stored.CompletedAtUtc.UtcDateTime.ShouldBe(instant);
        });
    }

    [Fact]
    public async Task QueryAsync_LocalAndUnspecifiedBounds_FilterByInstant()
    {
        await ClearDataAsync();
        var now = TruncatedUtcNow();
        await _store.RecordAsync(CreateTestEntry(timestampUtc: now.AddHours(-3)));
        var recent = CreateTestEntry(timestampUtc: now);
        await _store.RecordAsync(recent);

        var query = new OperationAuditQuery
        {
            FromUtc = now.AddHours(-1).ToLocalTime(),
            ToUtc = DateTime.SpecifyKind(now.AddHours(1), DateTimeKind.Unspecified)
        };
        var result = await _store.QueryAsync(query);
        var byUser = await _store.GetByUserAsync("test-user", now.AddHours(-1).ToLocalTime(), DateTime.SpecifyKind(now.AddHours(1), DateTimeKind.Unspecified));

        result.IsRight.ShouldBeTrue();
        byUser.IsRight.ShouldBeTrue();
        result.IfRight(page => page.Items.ShouldHaveSingleItem().Id.ShouldBe(recent.Id));
        byUser.IfRight(entries => entries.ShouldHaveSingleItem().Id.ShouldBe(recent.Id));
    }

    [Fact]
    public async Task PurgeEntriesAsync_LocalCutoff_PurgesByInstant()
    {
        await ClearDataAsync();
        var now = TruncatedUtcNow();
        await _store.RecordAsync(CreateTestEntry(timestampUtc: now.AddDays(-100)));
        await _store.RecordAsync(CreateTestEntry(timestampUtc: now));

        var result = await _store.PurgeEntriesAsync(now.AddDays(-30).ToLocalTime());

        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));
    }

    #endregion
}
