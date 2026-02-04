using Dapper;
using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Concurrency;

/// <summary>
/// Integration tests for Dapper concurrency handling using real SQL Server.
/// Tests the optimistic concurrency pattern with version-based updates.
/// </summary>
[Collection("ConcurrencyDapperTests")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class DapperConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly ConcurrencyDapperFixture _fixture;

    public DapperConcurrencyIntegrationTests(ConcurrencyDapperFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ClearDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Versioned Update Tests

    [Fact]
    public async Task VersionedUpdate_SuccessfulUpdate_IncrementsVersion()
    {
        // Arrange
        var entity = new DapperVersionedEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        await InsertEntityAsync(entity);

        // Act
        var originalVersion = entity.Version;
        entity.Version = originalVersion + 1;
        entity.Name = "Modified";
        var rowsAffected = await UpdateWithVersionCheckAsync(entity, originalVersion);

        // Assert
        rowsAffected.ShouldBe(1);
        var updated = await GetEntityByIdAsync(entity.Id);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Modified");
        updated.Version.ShouldBe(2);
    }

    [Fact]
    public async Task VersionedUpdate_ConcurrentModification_ReturnsZeroRowsAffected()
    {
        // Arrange
        var entity = new DapperVersionedEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        await InsertEntityAsync(entity);

        // Simulate first user modifying (version becomes 2)
        var firstUserVersion = entity.Version;
        entity.Version = firstUserVersion + 1;
        entity.Name = "Modified by User 1";
        var firstUpdateRows = await UpdateWithVersionCheckAsync(entity, firstUserVersion);
        firstUpdateRows.ShouldBe(1);

        // Act - Second user tries with stale version (version = 1, but DB has 2)
        entity.Name = "Modified by User 2";
        var staleVersion = 1;
        var secondUpdateRows = await UpdateWithVersionCheckAsync(entity, staleVersion);

        // Assert
        secondUpdateRows.ShouldBe(0); // No rows affected - conflict detected
    }

    [Fact]
    public async Task VersionedUpdate_NotFoundVsConflict_CanDistinguish()
    {
        // Arrange - Non-existing entity
        var nonExistingId = Guid.NewGuid();
        var entity = new DapperVersionedEntity
        {
            Id = nonExistingId,
            Name = "Doesn't Exist",
            Version = 2
        };

        // Act
        var rowsAffected = await UpdateWithVersionCheckAsync(entity, originalVersion: 1);

        // Assert - Zero rows, need to check if entity exists
        rowsAffected.ShouldBe(0);
        var exists = await EntityExistsAsync(nonExistingId);
        exists.ShouldBeFalse(); // This is NotFound, not a concurrency conflict
    }

    [Fact]
    public async Task VersionedUpdate_ConflictDetected_EntityExists()
    {
        // Arrange
        var entity = new DapperVersionedEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        await InsertEntityAsync(entity);

        // Modify the entity (version becomes 2)
        await UpdateVersionDirectlyAsync(entity.Id, 2, "Modified externally");

        // Act - Try to update with stale version 1
        entity.Name = "Attempted update";
        var rowsAffected = await UpdateWithVersionCheckAsync(entity, originalVersion: 1);

        // Assert
        rowsAffected.ShouldBe(0);
        var exists = await EntityExistsAsync(entity.Id);
        exists.ShouldBeTrue(); // Entity exists - this IS a concurrency conflict
    }

    #endregion

    #region Concurrency Conflict Info Tests

    [Fact]
    public async Task ConcurrencyConflictInfo_CanCaptureAllStates()
    {
        // Arrange
        var entity = new DapperVersionedEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        await InsertEntityAsync(entity);

        // Current (what we loaded)
        var current = await GetEntityByIdAsync(entity.Id);
        current.ShouldNotBeNull();

        // Simulate external modification
        await UpdateVersionDirectlyAsync(entity.Id, 2, "Externally modified");

        // Proposed (what we want to save)
        var proposed = new DapperVersionedEntity
        {
            Id = current.Id,
            Name = "Our changes",
            Version = 2
        };

        // Database (fresh read after conflict)
        var database = await GetEntityByIdAsync(entity.Id);

        // Act
        var conflictInfo = new ConcurrencyConflictInfo<DapperVersionedEntity>(current, proposed, database);

        // Assert
        conflictInfo.CurrentEntity.Name.ShouldBe("Original");
        conflictInfo.CurrentEntity.Version.ShouldBe(1);
        conflictInfo.ProposedEntity.Name.ShouldBe("Our changes");
        conflictInfo.DatabaseEntity.ShouldNotBeNull();
        conflictInfo.DatabaseEntity.Name.ShouldBe("Externally modified");
        conflictInfo.DatabaseEntity.Version.ShouldBe(2);
        conflictInfo.WasDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ConcurrencyConflictInfo_WhenDeleted_DatabaseEntityIsNull()
    {
        // Arrange
        var entity = new DapperVersionedEntity
        {
            Id = Guid.NewGuid(),
            Name = "To be deleted",
            Version = 1
        };
        await InsertEntityAsync(entity);

        var current = await GetEntityByIdAsync(entity.Id);
        current.ShouldNotBeNull();

        // Delete the entity
        await DeleteEntityAsync(entity.Id);

        // Act
        var database = await GetEntityByIdAsync(entity.Id);
        var proposed = new DapperVersionedEntity
        {
            Id = current.Id,
            Name = "Attempted change",
            Version = current.Version
        };

        var conflictInfo = new ConcurrencyConflictInfo<DapperVersionedEntity>(current, proposed, database);

        // Assert
        conflictInfo.WasDeleted.ShouldBeTrue();
        conflictInfo.DatabaseEntity.ShouldBeNull();
    }

    #endregion

    #region Helper Methods

    private async Task InsertEntityAsync(DapperVersionedEntity entity)
    {
        using var connection = _fixture.CreateConnection();
        const string sql = """
            INSERT INTO DapperVersionedEntities (Id, Name, Version)
            VALUES (@Id, @Name, @Version)
            """;
        await connection.ExecuteAsync(sql, entity);
    }

    private async Task<int> UpdateWithVersionCheckAsync(DapperVersionedEntity entity, int originalVersion)
    {
        using var connection = _fixture.CreateConnection();
        const string sql = """
            UPDATE DapperVersionedEntities
            SET Name = @Name, Version = @Version
            WHERE Id = @Id AND Version = @OriginalVersion
            """;
        return await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            entity.Version,
            OriginalVersion = originalVersion
        });
    }

    private async Task UpdateVersionDirectlyAsync(Guid id, int newVersion, string newName)
    {
        using var connection = _fixture.CreateConnection();
        const string sql = "UPDATE DapperVersionedEntities SET Name = @Name, Version = @Version WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id, Name = newName, Version = newVersion });
    }

    private async Task<DapperVersionedEntity?> GetEntityByIdAsync(Guid id)
    {
        using var connection = _fixture.CreateConnection();
        const string sql = "SELECT Id, Name, Version FROM DapperVersionedEntities WHERE Id = @Id";
        return await connection.QuerySingleOrDefaultAsync<DapperVersionedEntity>(sql, new { Id = id });
    }

    private async Task<bool> EntityExistsAsync(Guid id)
    {
        using var connection = _fixture.CreateConnection();
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM DapperVersionedEntities WHERE Id = @Id) THEN 1 ELSE 0 END";
        return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    private async Task DeleteEntityAsync(Guid id)
    {
        using var connection = _fixture.CreateConnection();
        const string sql = "DELETE FROM DapperVersionedEntities WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    #endregion
}

/// <summary>
/// Test entity for Dapper concurrency tests.
/// Uses class to allow mutation in tests.
/// </summary>
public class DapperVersionedEntity : IVersionedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }

    long IVersioned.Version => Version;
}

/// <summary>
/// Fixture for Dapper concurrency integration tests.
/// </summary>
public sealed class ConcurrencyDapperFixture : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServerFixture = new();

    public string ConnectionString => _sqlServerFixture.ConnectionString;

    public SqlConnection CreateConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public async Task InitializeAsync()
    {
        await _sqlServerFixture.InitializeAsync();

        using var connection = CreateConnection();
        await CreateDapperConcurrencyTestSchemaAsync(connection);
    }

    public async Task DisposeAsync()
    {
        await _sqlServerFixture.DisposeAsync();
    }

    public async Task ClearDataAsync()
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync("DELETE FROM DapperVersionedEntities");
    }

    private static async Task CreateDapperConcurrencyTestSchemaAsync(SqlConnection connection)
    {
        const string createTableSql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DapperVersionedEntities')
            BEGIN
                CREATE TABLE DapperVersionedEntities (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Version INT NOT NULL DEFAULT 1
                );
            END
            """;

        await connection.ExecuteAsync(createTableSql);
    }
}

/// <summary>
/// Collection definition for Dapper concurrency integration tests.
/// </summary>
[CollectionDefinition("ConcurrencyDapperTests")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit convention requires 'Collection' suffix for collection definitions")]
public class ConcurrencyDapperTestsCollection : ICollectionFixture<ConcurrencyDapperFixture>
{
}
