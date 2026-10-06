using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.EFCore;

/// <summary>
/// Integration tests for <see cref="OperationAuditStoreEF"/> with SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public class OperationAuditStoreEFSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public OperationAuditStoreEFSqlServerIntegrationTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.EnsureSchemaCreatedAsync<AuditTestDbContext>();
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    private OperationAuditStoreEF CreateStore()
    {
        var context = _fixture.CreateDbContext<AuditTestDbContext>();
        return new OperationAuditStoreEF(context);
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
        var store = CreateStore();
        var entry = CreateTestEntry();

        var result = await store.RecordAsync(entry);
        result.IsRight.ShouldBeTrue();

        var queryResult = await store.GetByEntityAsync(entry.EntityType, entry.EntityId);
        queryResult.IsRight.ShouldBeTrue();
        queryResult.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task RecordAsync_EntryWithAllFields_ShouldPersistAllFields()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;
        var entry = new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-ef-ss",
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

        var result = await store.RecordAsync(entry);
        result.IsRight.ShouldBeTrue();

        var queryResult = await store.GetByCorrelationIdAsync("test-correlation-ef-ss");
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
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await store.RecordAsync(CreateTestEntry(entityType: "Product"));

        var result = await store.GetByEntityAsync("Order", null);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBeGreaterThanOrEqualTo(2));
    }

    [Fact]
    public async Task GetByEntityAsync_WithEntityId_ShouldReturnSpecificEntry()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(entityType: "Order", entityId: "order-ss-1"));
        await store.RecordAsync(CreateTestEntry(entityType: "Order", entityId: "order-ss-2"));

        var result = await store.GetByEntityAsync("Order", "order-ss-1");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].EntityId.ShouldBe("order-ss-1");
        });
    }

    [Fact]
    public async Task GetByUserAsync_WithUserId_ShouldReturnUserEntries()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(userId: "ef-ss-u1"));
        await store.RecordAsync(CreateTestEntry(userId: "ef-ss-u1"));
        await store.RecordAsync(CreateTestEntry(userId: "ef-ss-u2"));

        var result = await store.GetByUserAsync("ef-ss-u1", null, null);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBe(2));
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRange_ShouldFilter()
    {
        var store = CreateStore();
        var oldEntry = CreateTestEntry(userId: "ef-ss-user", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "ef-ss-user", timestampUtc: DateTime.UtcNow.AddDays(-1));
        await store.RecordAsync(oldEntry);
        await store.RecordAsync(recentEntry);

        var result = await store.GetByUserAsync("ef-ss-user", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].Id.ShouldBe(recentEntry.Id);
        });
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnEntries()
    {
        var store = CreateStore();
        var correlationId = "corr-ef-ss";
        var now = DateTimeOffset.UtcNow;
        await store.RecordAsync(new OperationAuditEntry
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
        await store.RecordAsync(new OperationAuditEntry
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

        var result = await store.GetByCorrelationIdAsync(correlationId);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBe(2));
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnPagedResults()
    {
        var store = CreateStore();
        for (var i = 0; i < 15; i++)
            await store.RecordAsync(CreateTestEntry());

        var result = await store.QueryAsync(new OperationAuditQuery { PageNumber = 1, PageSize = 5 });
        result.IsRight.ShouldBeTrue();
        result.IfRight(p => p.Items.Count.ShouldBe(5));
    }

    [Fact]
    public async Task QueryAsync_WithFilters_ShouldReturnMatchingEntries()
    {
        var store = CreateStore();
        var entry2 = CreateTestEntry(userId: "ef-ss-filter", outcome: AuditOutcome.Failure);
        await store.RecordAsync(CreateTestEntry(userId: "ef-ss-filter", outcome: AuditOutcome.Success));
        await store.RecordAsync(entry2);

        var query = new OperationAuditQuery { UserId = "ef-ss-filter", Outcome = AuditOutcome.Failure };
        var result = await store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult => pagedResult.Items.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task QueryAsync_WithEntityTypeFilter_ShouldReturnMatchingEntries()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(entityType: "EFSSOrder"));
        await store.RecordAsync(CreateTestEntry(entityType: "EFSSProduct"));
        await store.RecordAsync(CreateTestEntry(entityType: "EFSSOrder"));

        var query = new OperationAuditQuery { EntityType = "EFSSOrder" };
        var result = await store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult => pagedResult.Items.Count.ShouldBeGreaterThanOrEqualTo(2));
    }

    [Fact]
    public async Task QueryAsync_WithDurationFilter_FiltersBeforePagingAndCountsTheFilteredSet()
    {
        var store = CreateStore();
        var now = DateTime.UtcNow;

        // 5 fast entries are the newest and 3 slow ones are older: filtering after paging would return an empty first page
        for (var i = 0; i < 3; i++)
        {
            var slow = CreateTestEntry(timestampUtc: now.AddMinutes(-20 - i));
            await store.RecordAsync(slow with { CompletedAtUtc = slow.StartedAtUtc.AddMilliseconds(500) });
        }

        for (var i = 0; i < 5; i++)
        {
            var fast = CreateTestEntry(timestampUtc: now.AddMinutes(-i));
            await store.RecordAsync(fast with { CompletedAtUtc = fast.StartedAtUtc.AddMilliseconds(10) });
        }

        var slowPage1 = await store.QueryAsync(new OperationAuditQuery { MinDuration = TimeSpan.FromMilliseconds(200), PageNumber = 1, PageSize = 2 });
        var slowPage2 = await store.QueryAsync(new OperationAuditQuery { MinDuration = TimeSpan.FromMilliseconds(200), PageNumber = 2, PageSize = 2 });
        var fastOnly = await store.QueryAsync(new OperationAuditQuery { MaxDuration = TimeSpan.FromMilliseconds(100), PageNumber = 1, PageSize = 10 });

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

    [Fact]
    public async Task PurgeEntriesAsync_ShouldDeleteOldEntries()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(timestampUtc: DateTime.UtcNow.AddDays(-100)));
        await store.RecordAsync(CreateTestEntry(timestampUtc: DateTime.UtcNow));

        var result = await store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBeGreaterThanOrEqualTo(1));
    }

    [Fact]
    public async Task PurgeEntriesAsync_WithNoOldEntries_ShouldReturnZero()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(timestampUtc: DateTime.UtcNow));

        var result = await store.PurgeEntriesAsync(DateTime.UtcNow.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

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
        var instant = TruncatedUtcNow();
        var timestamp = kind == DateTimeKind.Local
            ? instant.ToLocalTime()
            : DateTime.SpecifyKind(instant, DateTimeKind.Unspecified);
        var started = new DateTimeOffset(instant.AddMilliseconds(-100)).ToOffset(TimeSpan.FromHours(5));
        var entry = CreateTestEntry(timestampUtc: timestamp) with { StartedAtUtc = started, CompletedAtUtc = started.AddMilliseconds(100) };

        var recorded = await CreateStore().RecordAsync(entry);
        var read = await CreateStore().GetByEntityAsync(entry.EntityType, entry.EntityId!);

        recorded.IsRight.ShouldBeTrue();
        read.IsRight.ShouldBeTrue();
        read.IfRight(entries =>
        {
            var stored = entries.ShouldHaveSingleItem();
            // The tolerance covers the column precision; a Kind or zone error is hours off.
            stored.TimestampUtc.ShouldBe(instant, TimeSpan.FromMilliseconds(10));
            stored.StartedAtUtc.UtcDateTime.ShouldBe(instant.AddMilliseconds(-100), TimeSpan.FromMilliseconds(10));
            stored.CompletedAtUtc.UtcDateTime.ShouldBe(instant, TimeSpan.FromMilliseconds(10));
        });
    }

    [Fact]
    public async Task QueryAsync_LocalAndUnspecifiedBounds_FilterByInstant()
    {
        var now = TruncatedUtcNow();
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(timestampUtc: now.AddHours(-3)));
        var recent = CreateTestEntry(timestampUtc: now);
        await store.RecordAsync(recent);

        var query = new OperationAuditQuery
        {
            FromUtc = now.AddHours(-1).ToLocalTime(),
            ToUtc = DateTime.SpecifyKind(now.AddHours(1), DateTimeKind.Unspecified)
        };
        var result = await CreateStore().QueryAsync(query);
        var byUser = await CreateStore().GetByUserAsync("test-user", now.AddHours(-1).ToLocalTime(), DateTime.SpecifyKind(now.AddHours(1), DateTimeKind.Unspecified));

        result.IsRight.ShouldBeTrue();
        byUser.IsRight.ShouldBeTrue();
        result.IfRight(page => page.Items.ShouldHaveSingleItem().Id.ShouldBe(recent.Id));
        byUser.IfRight(entries => entries.ShouldHaveSingleItem().Id.ShouldBe(recent.Id));
    }

    [Fact]
    public async Task PurgeEntriesAsync_LocalCutoff_PurgesByInstant()
    {
        var now = TruncatedUtcNow();
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(timestampUtc: now.AddDays(-100)));
        await store.RecordAsync(CreateTestEntry(timestampUtc: now));

        var result = await CreateStore().PurgeEntriesAsync(now.AddDays(-30).ToLocalTime());

        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));
    }

    #endregion
}
