using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.MongoDB;

/// <summary>
/// Integration tests for <see cref="OperationAuditStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class OperationAuditStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public OperationAuditStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName
        });
    }

    public async ValueTask InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<OperationAuditEntryDocument>(
                _options.Value.Collections.OperationAuditEntries);
            await collection.DeleteManyAsync(Builders<OperationAuditEntryDocument>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private OperationAuditStoreMongoDB CreateStore()
    {
        return new OperationAuditStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<OperationAuditStoreMongoDB>.Instance);
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
    public async Task GetByEntityAsync_ShouldReturnMatchingEntries()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await store.RecordAsync(CreateTestEntry(entityType: "Order"));
        await store.RecordAsync(CreateTestEntry(entityType: "Product"));

        var result = await store.GetByEntityAsync("Order", null);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.Count.ShouldBe(2));
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRange_ShouldFilter()
    {
        var store = CreateStore();
        var oldEntry = CreateTestEntry(userId: "mongo-user", timestampUtc: DateTime.UtcNow.AddDays(-10));
        var recentEntry = CreateTestEntry(userId: "mongo-user", timestampUtc: DateTime.UtcNow.AddDays(-1));
        await store.RecordAsync(oldEntry);
        await store.RecordAsync(recentEntry);

        var result = await store.GetByUserAsync("mongo-user", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);
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
        var correlationId = "corr-mongo";
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
        for (var i = 0; i < 25; i++)
            await store.RecordAsync(CreateTestEntry());

        var result = await store.QueryAsync(new OperationAuditQuery { PageNumber = 1, PageSize = 10 });
        result.IsRight.ShouldBeTrue();
        result.IfRight(p => { p.TotalCount.ShouldBe(25); p.Items.Count.ShouldBe(10); });
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
        result.IfRight(count => count.ShouldBe(1));
    }
}
