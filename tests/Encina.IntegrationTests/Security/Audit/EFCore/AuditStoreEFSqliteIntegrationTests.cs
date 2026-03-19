using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.EFCore;

/// <summary>
/// Integration tests for <see cref="AuditStoreEF"/> with SQLite.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("EFCore-Sqlite")]
public class AuditStoreEFSqliteIntegrationTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;

    public AuditStoreEFSqliteIntegrationTests(EFCoreSqliteFixture fixture)
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

    private AuditStoreEF CreateStore()
    {
        var context = _fixture.CreateDbContext<AuditTestDbContext>();
        return new AuditStoreEF(context);
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
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "test-correlation-ef-sqlite",
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

        var queryResult = await store.GetByCorrelationIdAsync("test-correlation-ef-sqlite");
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
        await store.RecordAsync(CreateTestEntry(entityType: "Order", entityId: "order-1"));
        await store.RecordAsync(CreateTestEntry(entityType: "Order", entityId: "order-2"));

        var result = await store.GetByEntityAsync("Order", "order-1");
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
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(userId: "ef-sqlite-u1"));
        await store.RecordAsync(CreateTestEntry(userId: "ef-sqlite-u1"));
        await store.RecordAsync(CreateTestEntry(userId: "ef-sqlite-u2"));

        var result = await store.GetByUserAsync("ef-sqlite-u1", null, null);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBe(2));
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRange_ShouldFilter()
    {
        var store = CreateStore();
        var oldEntry = CreateTestEntry(userId: "ef-user-1", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "ef-user-1", timestampUtc: DateTime.UtcNow.AddDays(-1));
        await store.RecordAsync(oldEntry);
        await store.RecordAsync(recentEntry);

        var result = await store.GetByUserAsync("ef-user-1", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);
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
        var correlationId = "corr-ef-sqlite";
        var now = DateTimeOffset.UtcNow;
        await store.RecordAsync(new AuditEntry
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
        await store.RecordAsync(new AuditEntry
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

        var result = await store.QueryAsync(new AuditQuery { PageNumber = 1, PageSize = 5 });
        result.IsRight.ShouldBeTrue();
        result.IfRight(p =>
        {
            p.Items.Count.ShouldBe(5);
            p.TotalCount.ShouldBeGreaterThanOrEqualTo(15);
        });
    }

    [Fact]
    public async Task QueryAsync_WithFilters_ShouldReturnMatchingEntries()
    {
        var store = CreateStore();
        var entry2 = CreateTestEntry(userId: "ef-sqlite-filter", outcome: AuditOutcome.Failure);
        await store.RecordAsync(CreateTestEntry(userId: "ef-sqlite-filter", outcome: AuditOutcome.Success));
        await store.RecordAsync(entry2);

        var query = new AuditQuery { UserId = "ef-sqlite-filter", Outcome = AuditOutcome.Failure };
        var result = await store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult => pagedResult.Items.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task QueryAsync_WithEntityTypeFilter_ShouldReturnMatchingEntries()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(entityType: "EFOrder"));
        await store.RecordAsync(CreateTestEntry(entityType: "EFProduct"));
        await store.RecordAsync(CreateTestEntry(entityType: "EFOrder"));

        var query = new AuditQuery { EntityType = "EFOrder" };
        var result = await store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(pagedResult => pagedResult.Items.Count.ShouldBeGreaterThanOrEqualTo(2));
    }

    [Fact]
    public async Task PurgeEntriesAsync_ShouldDeleteOldEntries()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(timestampUtc: DateTime.UtcNow.AddDays(-100)));
        var recentEntry = CreateTestEntry(timestampUtc: DateTime.UtcNow);
        await store.RecordAsync(recentEntry);

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
}
