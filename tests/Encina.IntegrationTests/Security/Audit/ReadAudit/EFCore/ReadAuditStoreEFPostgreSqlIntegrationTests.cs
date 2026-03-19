using Encina.EntityFrameworkCore.Auditing;
using Encina.IntegrationTests.Security.Audit.EFCore;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.ReadAudit.EFCore;

/// <summary>
/// Integration tests for <see cref="ReadAuditStoreEF"/> with PostgreSQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public class ReadAuditStoreEFPostgreSqlIntegrationTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public ReadAuditStoreEFPostgreSqlIntegrationTests(EFCorePostgreSqlFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync() => await _fixture.EnsureSchemaCreatedAsync<AuditTestDbContext>();
    public async ValueTask DisposeAsync() => await _fixture.ClearAllDataAsync();

    private ReadAuditStoreEF CreateStore() => new(_fixture.CreateDbContext<AuditTestDbContext>());

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
        await store.LogReadAsync(CreateTestEntry(entityId: "PAT-EF-PG"));
        var result = await store.GetAccessHistoryAsync("Patient", "PAT-EF-PG");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldNotBeEmpty());
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_ShouldFilter()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;
        await store.LogReadAsync(CreateTestEntry(userId: "ef-pg-ruser", accessedAtUtc: now));
        var result = await store.GetUserAccessHistoryAsync("ef-pg-ruser", now.AddHours(-1), now.AddHours(1));
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldNotBeEmpty());
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldWork()
    {
        var store = CreateStore();
        for (var i = 0; i < 10; i++)
            await store.LogReadAsync(CreateTestEntry(entityId: $"PAT-PG-{i}"));
        var query = ReadAuditQuery.Builder().OnPage(1).WithPageSize(3).Build();
        var result = await store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged => paged.Items.Count.ShouldBeLessThanOrEqualTo(3));
    }

    [Fact]
    public async Task PurgeEntriesAsync_ShouldRemoveOld()
    {
        var store = CreateStore();
        await store.LogReadAsync(CreateTestEntry(entityId: "OLD-PG", accessedAtUtc: DateTimeOffset.UtcNow.AddDays(-100)));
        await store.LogReadAsync(CreateTestEntry(entityId: "RECENT-PG", accessedAtUtc: DateTimeOffset.UtcNow));
        var result = await store.PurgeEntriesAsync(DateTimeOffset.UtcNow.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBeGreaterThanOrEqualTo(1));
    }
}
