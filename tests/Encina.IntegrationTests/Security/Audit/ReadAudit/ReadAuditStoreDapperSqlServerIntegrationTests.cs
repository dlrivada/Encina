using System.Data;
using Encina.Dapper.SqlServer.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.ReadAudit;

/// <summary>
/// Integration tests for <see cref="ReadAuditStoreDapper"/> with SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public class ReadAuditStoreDapperSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbConnection _connection = null!;
    private ReadAuditStoreDapper _store = null!;

    public ReadAuditStoreDapperSqlServerIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is SqlConnection sqlConnection)
            await CreateSchemaAsync(sqlConnection);
        _store = new ReadAuditStoreDapper(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReadAuditEntries')
            CREATE TABLE ReadAuditEntries (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                EntityType NVARCHAR(256) NOT NULL,
                EntityId NVARCHAR(256) NULL,
                UserId NVARCHAR(256) NULL,
                TenantId NVARCHAR(128) NULL,
                AccessedAtUtc DATETIMEOFFSET NOT NULL,
                CorrelationId NVARCHAR(256) NULL,
                Purpose NVARCHAR(500) NULL,
                AccessMethod INT NOT NULL,
                EntityCount INT NOT NULL,
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
            await using var command = new SqlCommand("DELETE FROM ReadAuditEntries;", sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
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
        await _store.LogReadAsync(CreateTestEntry(userId: "user-dss", accessedAtUtc: now));
        var result = await _store.GetUserAccessHistoryAsync("user-dss", now.AddHours(-1), now.AddHours(1));
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
