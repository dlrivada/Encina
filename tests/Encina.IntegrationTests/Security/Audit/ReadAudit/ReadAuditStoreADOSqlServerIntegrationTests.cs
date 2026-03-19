using System.Data;
using Encina.ADO.SqlServer.Auditing;
using Encina.Security.Audit;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Security.Audit.ReadAudit;

/// <summary>
/// Integration tests for <see cref="ReadAuditStoreADO"/> with SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public class ReadAuditStoreADOSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbConnection _connection = null!;
    private ReadAuditStoreADO _store = null!;

    public ReadAuditStoreADOSqlServerIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection();
        if (_connection is SqlConnection sqlConnection)
            await CreateReadAuditSchemaAsync(sqlConnection);

        _store = new ReadAuditStoreADO(_connection);
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        _connection.Dispose();
    }

    private static async Task CreateReadAuditSchemaAsync(SqlConnection connection)
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
        string entityType = "Patient",
        string? entityId = "PAT-001",
        string? userId = "user-1",
        DateTimeOffset? accessedAtUtc = null,
        ReadAccessMethod accessMethod = ReadAccessMethod.Repository,
        int entityCount = 1) =>
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
        var entry = CreateTestEntry();
        var result = await _store.LogReadAsync(entry);
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAccessHistoryAsync_ExistingEntity_ShouldReturnEntries()
    {
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(entityId: "PAT-100"));

        var result = await _store.GetAccessHistoryAsync("Patient", "PAT-100");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NoMatches_ShouldReturnEmptyList()
    {
        await ClearDataAsync();
        var result = await _store.GetAccessHistoryAsync("Patient", "NONEXISTENT");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldBeEmpty());
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_WithinDateRange_ShouldReturnEntries()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(userId: "user-history", accessedAtUtc: now));

        var result = await _store.GetUserAccessHistoryAsync("user-history", now.AddHours(-1), now.AddHours(1));
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldHaveSingleItem());
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_OutsideDateRange_ShouldReturnEmpty()
    {
        await ClearDataAsync();
        await _store.LogReadAsync(CreateTestEntry(userId: "user-old", accessedAtUtc: DateTimeOffset.UtcNow.AddDays(-30)));

        var result = await _store.GetUserAccessHistoryAsync("user-old", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries => entries.ShouldBeEmpty());
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldRespectPageSize()
    {
        await ClearDataAsync();
        for (var i = 0; i < 10; i++)
            await _store.LogReadAsync(CreateTestEntry(entityId: $"PAT-P{i}"));

        var query = ReadAuditQuery.Builder().OnPage(1).WithPageSize(3).Build();
        var result = await _store.QueryAsync(query);
        result.IsRight.ShouldBeTrue();
        result.IfRight(paged =>
        {
            paged.Items.Count.ShouldBe(3);
            paged.TotalCount.ShouldBe(10);
        });
    }

    [Fact]
    public async Task PurgeEntriesAsync_OldEntries_ShouldRemoveOnlyOldOnes()
    {
        await ClearDataAsync();
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(entityId: "OLD", accessedAtUtc: now.AddDays(-100)));
        await _store.LogReadAsync(CreateTestEntry(entityId: "RECENT", accessedAtUtc: now));

        var result = await _store.PurgeEntriesAsync(now.AddDays(-30));
        result.IsRight.ShouldBeTrue();
        result.IfRight(purged => purged.ShouldBe(1));
    }

    [Fact]
    public async Task LogRead_ThenQuery_ShouldPreserveAllFields()
    {
        await ClearDataAsync();
        var entry = CreateTestEntry(
            entityType: "FinancialRecord", entityId: "FR-999",
            userId: "auditor-1", accessMethod: ReadAccessMethod.Export,
            entityCount: 42);

        await _store.LogReadAsync(entry);
        var result = await _store.GetAccessHistoryAsync("FinancialRecord", "FR-999");
        result.IsRight.ShouldBeTrue();
        result.IfRight(entries =>
        {
            entries.ShouldHaveSingleItem();
            entries[0].EntityCount.ShouldBe(42);
            entries[0].AccessMethod.ShouldBe(ReadAccessMethod.Export);
        });
    }
}
