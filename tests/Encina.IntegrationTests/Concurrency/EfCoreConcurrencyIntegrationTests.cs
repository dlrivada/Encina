using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.EntityFrameworkCore.Repository;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Concurrency;

/// <summary>
/// Integration tests for EF Core concurrency handling using real SQL Server.
/// Tests the optimistic concurrency abstractions with actual database operations.
/// </summary>
[Collection("ConcurrencyEFTests")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class EfCoreConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly ConcurrencyEFFixture _fixture;
    private ConcurrencyTestDbContext _dbContext = null!;
    private FunctionalRepositoryEF<VersionedTestEntity, Guid> _repository = null!;

    public EfCoreConcurrencyIntegrationTests(ConcurrencyEFFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ClearDataAsync();
        _dbContext = _fixture.CreateDbContext();
        _repository = new FunctionalRepositoryEF<VersionedTestEntity, Guid>(_dbContext);
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    #region Versioned Update Tests

    [Fact]
    public async Task UpdateAsync_WithVersionedEntity_IncrementsVersion()
    {
        // Arrange
        var entity = new VersionedTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        _dbContext.VersionedTestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Get the entity fresh from DB
        var loadedEntity = await _dbContext.VersionedTestEntities.FindAsync(entity.Id);
        loadedEntity.ShouldNotBeNull();
        loadedEntity.Name = "Modified";

        // Act
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Assert - Get fresh from DB to verify version incremented
        var updatedEntity = await _dbContext.VersionedTestEntities.FindAsync(entity.Id);
        updatedEntity.ShouldNotBeNull();
        updatedEntity.Name.ShouldBe("Modified");
        updatedEntity.Version.ShouldBe(2);
    }

    [Fact]
    public async Task UpdateAsync_WithConcurrentModification_DetectsConflict()
    {
        // Arrange - Create entity
        var entity = new VersionedTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        _dbContext.VersionedTestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Load entity in two separate contexts (simulating two concurrent users)
        using var context1 = _fixture.CreateDbContext();
        using var context2 = _fixture.CreateDbContext();

        var entity1 = await context1.VersionedTestEntities.FindAsync(entity.Id);
        var entity2 = await context2.VersionedTestEntities.FindAsync(entity.Id);

        entity1.ShouldNotBeNull();
        entity2.ShouldNotBeNull();

        // Modify entity in first context
        entity1.Name = "Modified by User 1";
        await context1.SaveChangesAsync();

        // Modify entity in second context (using stale version)
        entity2.Name = "Modified by User 2";

        // Act & Assert - Second save should detect concurrency conflict
        var exception = await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
        {
            await context2.SaveChangesAsync();
        });

        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_AfterConcurrencyConflict_CanReloadAndRetry()
    {
        // Arrange
        var entity = new VersionedTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        _dbContext.VersionedTestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        using var context1 = _fixture.CreateDbContext();
        using var context2 = _fixture.CreateDbContext();

        var entity1 = await context1.VersionedTestEntities.FindAsync(entity.Id);
        var entity2 = await context2.VersionedTestEntities.FindAsync(entity.Id);

        entity1.ShouldNotBeNull();
        entity2.ShouldNotBeNull();

        // First user saves
        entity1.Name = "Modified by User 1";
        await context1.SaveChangesAsync();

        // Second user tries (and fails)
        entity2.Name = "Modified by User 2";
        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
        {
            await context2.SaveChangesAsync();
        });

        // Act - Reload entity and retry
        await context2.Entry(entity2).ReloadAsync();
        entity2.Name = "Modified by User 2 (retry)";
        await context2.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var finalEntity = await verifyContext.VersionedTestEntities.FindAsync(entity.Id);
        finalEntity.ShouldNotBeNull();
        finalEntity.Name.ShouldBe("Modified by User 2 (retry)");
        finalEntity.Version.ShouldBe(3); // Original(1) + User1(2) + User2 retry(3)
    }

    #endregion

    #region Concurrency Conflict Info Tests

    [Fact]
    public async Task ConcurrencyConflictInfo_CanCaptureEntityStates()
    {
        // Arrange
        var entity = new VersionedTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Version = 1
        };
        _dbContext.VersionedTestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        using var context1 = _fixture.CreateDbContext();
        using var context2 = _fixture.CreateDbContext();

        var current = await context1.VersionedTestEntities.FindAsync(entity.Id);
        var entity2 = await context2.VersionedTestEntities.FindAsync(entity.Id);

        current.ShouldNotBeNull();
        entity2.ShouldNotBeNull();

        // First user modifies and saves
        current.Name = "Modified";
        await context1.SaveChangesAsync();

        // Second user attempts with stale data - create a copy for proposed
        var proposed = new VersionedTestEntity
        {
            Id = entity2.Id,
            Name = "Proposed Change",
            Version = entity2.Version
        };

        // Act - Create conflict info after detecting conflict
        var database = await context1.VersionedTestEntities.FindAsync(entity.Id);
        database.ShouldNotBeNull();

        var conflictInfo = new ConcurrencyConflictInfo<VersionedTestEntity>(
            CurrentEntity: current,
            ProposedEntity: proposed,
            DatabaseEntity: database
        );

        // Assert
        conflictInfo.CurrentEntity.Name.ShouldBe("Modified");
        conflictInfo.ProposedEntity.Name.ShouldBe("Proposed Change");
        conflictInfo.DatabaseEntity.ShouldNotBeNull();
        conflictInfo.WasDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ConcurrencyConflictInfo_WhenEntityDeleted_WasDeletedIsTrue()
    {
        // Arrange
        var entity = new VersionedTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "To Be Deleted",
            Version = 1
        };
        _dbContext.VersionedTestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Load entity then delete it from another context
        using var context1 = _fixture.CreateDbContext();
        var current = await context1.VersionedTestEntities.FindAsync(entity.Id);
        current.ShouldNotBeNull();

        using var deleteContext = _fixture.CreateDbContext();
        var toDelete = await deleteContext.VersionedTestEntities.FindAsync(entity.Id);
        toDelete.ShouldNotBeNull();
        deleteContext.VersionedTestEntities.Remove(toDelete);
        await deleteContext.SaveChangesAsync();

        // Act - Try to get database entity (should be null because deleted)
        using var verifyContext = _fixture.CreateDbContext();
        var database = await verifyContext.VersionedTestEntities.FindAsync(entity.Id);

        var proposed = new VersionedTestEntity
        {
            Id = current.Id,
            Name = "Proposed after deletion",
            Version = current.Version
        };

        var conflictInfo = new ConcurrencyConflictInfo<VersionedTestEntity>(
            CurrentEntity: current,
            ProposedEntity: proposed,
            DatabaseEntity: database  // null
        );

        // Assert
        conflictInfo.WasDeleted.ShouldBeTrue();
        conflictInfo.DatabaseEntity.ShouldBeNull();
    }

    #endregion

    #region Repository Error Tests

    [Fact]
    public void RepositoryErrors_ConcurrencyConflict_WithConflictInfo_CreatesProperError()
    {
        // Arrange
        var current = new VersionedTestEntity { Id = Guid.NewGuid(), Name = "Current", Version = 1 };
        var proposed = new VersionedTestEntity { Id = current.Id, Name = "Proposed", Version = 2 };
        var database = new VersionedTestEntity { Id = current.Id, Name = "Database", Version = 3 };

        var conflictInfo = new ConcurrencyConflictInfo<VersionedTestEntity>(current, proposed, database);

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode));

        var details = error.GetDetails();
        details.ShouldContainKey("EntityType");
        details["EntityType"].ShouldBe("VersionedTestEntity");
    }

    #endregion
}

/// <summary>
/// Test entity with integer versioning for concurrency tests.
/// Uses class instead of record to allow EF Core change tracking mutations.
/// </summary>
public class VersionedTestEntity : IVersionedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }

    // IVersioned explicit implementation
    long IVersioned.Version => Version;
}

/// <summary>
/// DbContext for concurrency integration tests.
/// </summary>
public sealed class ConcurrencyTestDbContext : DbContext
{
    public ConcurrencyTestDbContext(DbContextOptions<ConcurrencyTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<VersionedTestEntity> VersionedTestEntities => Set<VersionedTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VersionedTestEntity>(entity =>
        {
            entity.ToTable("VersionedTestEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

            // Configure Version as concurrency token
            entity.Property(e => e.Version)
                .IsRequired()
                .IsConcurrencyToken();
        });
    }
}

/// <summary>
/// Fixture for EF Core concurrency integration tests.
/// </summary>
public sealed class ConcurrencyEFFixture : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServerFixture = new();

    public string ConnectionString => _sqlServerFixture.ConnectionString;

    public ConcurrencyTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ConcurrencyTestDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new ConcurrencyTestDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _sqlServerFixture.InitializeAsync();

        // Create the concurrency test schema
        using var connection = _sqlServerFixture.CreateConnection();
        if (connection is SqlConnection sqlConnection)
        {
            await CreateConcurrencyTestSchemaAsync(sqlConnection);
        }
    }

    public async Task DisposeAsync()
    {
        await _sqlServerFixture.DisposeAsync();
    }

    public async Task ClearDataAsync()
    {
        using var connection = _sqlServerFixture.CreateConnection();
        if (connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand("DELETE FROM VersionedTestEntities", sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task CreateConcurrencyTestSchemaAsync(SqlConnection connection)
    {
        const string createTableSql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VersionedTestEntities')
            BEGIN
                CREATE TABLE VersionedTestEntities (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Version INT NOT NULL DEFAULT 1
                );
            END
            """;

        await using var command = new SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Collection definition for EF Core concurrency integration tests.
/// </summary>
[CollectionDefinition("ConcurrencyEFTests")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit convention requires 'Collection' suffix for collection definitions")]
public class ConcurrencyEFTestsCollection : ICollectionFixture<ConcurrencyEFFixture>
{
}
