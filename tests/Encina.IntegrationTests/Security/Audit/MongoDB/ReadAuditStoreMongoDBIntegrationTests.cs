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
/// Integration tests for <see cref="ReadAuditStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ReadAuditStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public ReadAuditStoreMongoDBIntegrationTests(MongoDbFixture fixture)
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
            var collection = _fixture.Database!.GetCollection<ReadAuditEntryDocument>(
                _options.Value.Collections.ReadAuditEntries);
            await collection.DeleteManyAsync(Builders<ReadAuditEntryDocument>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private ReadAuditStoreMongoDB CreateStore()
    {
        return new ReadAuditStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<ReadAuditStoreMongoDB>.Instance);
    }

    private static ReadAuditEntry CreateTestEntry(
        string entityType = "Patient", string? entityId = "PAT-001",
        string? userId = "user-1", DateTimeOffset? accessedAtUtc = null,
        ReadAccessMethod accessMethod = ReadAccessMethod.Repository, int entityCount = 1) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            AccessedAtUtc = accessedAtUtc ?? DateTimeOffset.UtcNow,
            AccessMethod = accessMethod,
            EntityCount = entityCount
        };

    [Fact]
    public async Task LogReadAsync_ValidEntry_ShouldPersist()
    {
        var store = CreateStore();
        var result = await store.LogReadAsync(CreateTestEntry());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAccessHistoryAsync_ShouldReturnEntries()
    {
        var store = CreateStore();
        await store.LogReadAsync(CreateTestEntry(entityId: "PAT-MONGO"));
        var result = await store.GetAccessHistoryAsync("Patient", "PAT-MONGO");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_ShouldFilter()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;
        await store.LogReadAsync(CreateTestEntry(userId: "mongo-ruser", accessedAtUtc: now));
        var result = await store.GetUserAccessHistoryAsync("mongo-ruser", now.AddHours(-1), now.AddHours(1));
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldWork()
    {
        var store = CreateStore();
        for (var i = 0; i < 10; i++)
            await store.LogReadAsync(CreateTestEntry(entityId: $"PAT-M{i}"));
        var query = ReadAuditQuery.Builder().OnPage(1).WithPageSize(3).Build();
        var result = await store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged => paged.Items.Count.ShouldBe(3));
    }

    [Fact]
    public async Task PurgeEntriesAsync_ShouldRemoveOld()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;
        await store.LogReadAsync(CreateTestEntry(entityId: "OLD-M", accessedAtUtc: now.AddDays(-100)));
        await store.LogReadAsync(CreateTestEntry(entityId: "RECENT-M", accessedAtUtc: now));
        var result = await store.PurgeEntriesAsync(now.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBe(1));
    }
}
