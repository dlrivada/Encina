using System.Data;
using Encina.Dapper.PostgreSQL.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.ReadAudit;

/// <summary>
/// Integration tests for <see cref="ReadAuditStoreDapper"/> with PostgreSQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("Dapper-PostgreSQL")]
public class ReadAuditStoreDapperPostgreSqlIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private IDbConnection _connection = null!;
    private ReadAuditStoreDapper _store = null!;

    public ReadAuditStoreDapperPostgreSqlIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is NpgsqlConnection npgsqlConnection)
            await CreateSchemaAsync(npgsqlConnection);
        _store = new ReadAuditStoreDapper(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "ReadAuditEntries" (
                "Id" UUID PRIMARY KEY,
                "EntityType" VARCHAR(256) NOT NULL,
                "EntityId" VARCHAR(256) NULL,
                "UserId" VARCHAR(256) NULL,
                "TenantId" VARCHAR(128) NULL,
                "AccessedAtUtc" TIMESTAMPTZ NOT NULL,
                "CorrelationId" VARCHAR(256) NULL,
                "Purpose" VARCHAR(500) NULL,
                "AccessMethod" INTEGER NOT NULL,
                "EntityCount" INTEGER NOT NULL,
                "Metadata" TEXT NULL
            );
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = new NpgsqlCommand("""DELETE FROM "ReadAuditEntries";""", npgsqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static ReadAuditEntry CreateTestEntry(
        string entityType = "Patient", string? entityId = "PAT-001",
        string? userId = "user-1", DateTimeOffset? accessedAtUtc = null,
        ReadAccessMethod accessMethod = ReadAccessMethod.Repository, int entityCount = 1) =>
        new()
        {
            Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId,
            UserId = userId, AccessedAtUtc = accessedAtUtc ?? DateTimeOffset.UtcNow,
            AccessMethod = accessMethod, EntityCount = entityCount
        };

    [Fact]
    public async Task LogReadAsync_ValidEntry_ShouldPersist()
    {
        await ClearDataAsync();
        var result = await _store.LogReadAsync(CreateTestEntry());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAccessHistoryAsync_ShouldReturnEntries()
    {
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(entityId: "PAT-100"));
        var result = await _store.GetAccessHistoryAsync("Patient", "PAT-100");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_ShouldFilter()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(userId: "user-dpg", accessedAtUtc: now));
        var result = await _store.GetUserAccessHistoryAsync("user-dpg", now.AddHours(-1), now.AddHours(1));
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldWork()
    {
        await ClearDataAsync();
        for (var i = 0; i < 10; i++)
            await _store.LogReadAsync(CreateTestEntry(entityId: $"PAT-{i}"));
        var query = ReadAuditQuery.Builder().OnPage(1).WithPageSize(3).Build();
        var result = await _store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged => paged.Items.Count.ShouldBe(3));
    }

    [Fact]
    public async Task PurgeEntriesAsync_ShouldRemoveOld()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(entityId: "OLD", accessedAtUtc: now.AddDays(-100)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "RECENT", accessedAtUtc: now));
        var result = await _store.PurgeEntriesAsync(now.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBe(1));
    }
}
