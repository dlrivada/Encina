using System.Data;
using System.Linq.Expressions;
using Encina;
using Encina.Dapper.Sqlite.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Encina.Testing.Time;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Dapper.Sqlite.Auditing;

/// <summary>
/// Integration tests for audit field auto-population in Dapper SQLite repository.
/// Tests verify that IAuditableEntity fields are correctly populated during CRUD operations.
/// </summary>
[Collection("Dapper-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
public class AuditFieldPopulationDapperTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private IDbConnection _connection = null!;
    private FunctionalRepositoryDapper<AuditableProduct, Guid> _repository = null!;
    private FunctionalRepositoryDapper<AuditableProduct, Guid> _repositoryWithAudit = null!;
    private FunctionalRepositoryDapper<NonAuditableProduct, Guid> _nonAuditableRepository = null!;
    private FunctionalRepositoryDapper<PartialAuditableProduct, Guid> _partialAuditableRepository = null!;
    private IEntityMapping<AuditableProduct, Guid> _auditableMapping = null!;
    private IEntityMapping<NonAuditableProduct, Guid> _nonAuditableMapping = null!;
    private IEntityMapping<PartialAuditableProduct, Guid> _partialAuditableMapping = null!;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IRequestContext _mockRequestContext;
    private static readonly DateTimeOffset FixedTime = new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
    private const string TestUserId = "test-user-123";

    public AuditFieldPopulationDapperTests(SqliteFixture fixture)
    {
        _fixture = fixture;
        _fakeTimeProvider = new FakeTimeProvider(FixedTime);
        _mockRequestContext = Substitute.For<IRequestContext>();
        _mockRequestContext.UserId.Returns(TestUserId);
    }

    public async Task InitializeAsync()
    {
        // Create test schema
        if (_fixture.CreateConnection() is SqliteConnection schemaConnection)
        {
            await CreateAuditableProductsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();

        // Mapping for fully auditable entity
        _auditableMapping = new EntityMappingBuilder<AuditableProduct, Guid>()
            .ToTable("AuditableProducts")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Price, "Price")
            .MapProperty(p => p.CreatedAtUtc, "CreatedAtUtc")
            .MapProperty(p => p.CreatedBy, "CreatedBy")
            .MapProperty(p => p.ModifiedAtUtc, "ModifiedAtUtc")
            .MapProperty(p => p.ModifiedBy, "ModifiedBy")
            .Build();

        // Mapping for non-auditable entity
        _nonAuditableMapping = new EntityMappingBuilder<NonAuditableProduct, Guid>()
            .ToTable("NonAuditableProducts")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Price, "Price")
            .Build();

        // Mapping for partial auditable entity (only CreatedAtUtc)
        _partialAuditableMapping = new EntityMappingBuilder<PartialAuditableProduct, Guid>()
            .ToTable("PartialAuditableProducts")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.CreatedAtUtc, "CreatedAtUtc")
            .Build();

        // Repository without audit context (default behavior)
        _repository = new FunctionalRepositoryDapper<AuditableProduct, Guid>(_connection, _auditableMapping);

        // Repository with audit context
        _repositoryWithAudit = new FunctionalRepositoryDapper<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            _mockRequestContext,
            _fakeTimeProvider);

        // Non-auditable repository
        _nonAuditableRepository = new FunctionalRepositoryDapper<NonAuditableProduct, Guid>(
            _connection,
            _nonAuditableMapping,
            _mockRequestContext,
            _fakeTimeProvider);

        // Partial auditable repository
        _partialAuditableRepository = new FunctionalRepositoryDapper<PartialAuditableProduct, Guid>(
            _connection,
            _partialAuditableMapping,
            _mockRequestContext,
            _fakeTimeProvider);
    }

    public Task DisposeAsync()
    {
        // Do NOT dispose _connection - it's the shared SQLite in-memory connection owned by the fixture.
        return Task.CompletedTask;
    }

    private static async Task CreateAuditableProductsSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS AuditableProducts;
            CREATE TABLE AuditableProducts (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CreatedBy TEXT,
                ModifiedAtUtc TEXT,
                ModifiedBy TEXT
            );

            DROP TABLE IF EXISTS NonAuditableProducts;
            CREATE TABLE NonAuditableProducts (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Price REAL NOT NULL
            );

            DROP TABLE IF EXISTS PartialAuditableProducts;
            CREATE TABLE PartialAuditableProducts (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            """;

        await using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqliteConnection sqliteConnection)
        {
            await using var command = new SqliteCommand(
                "DELETE FROM AuditableProducts; DELETE FROM NonAuditableProducts; DELETE FROM PartialAuditableProducts;",
                sqliteConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    #region Create Operations Tests

    [Fact]
    public async Task AddAsync_WithAuditContext_PopulatesCreatedFields()
    {
        // Arrange
        await ClearDataAsync();
        var entity = new AuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m
        };

        // Act
        var result = await _repositoryWithAudit.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            e.CreatedBy.ShouldBe(TestUserId);
            e.ModifiedAtUtc.ShouldBeNull();
            e.ModifiedBy.ShouldBeNull();
        });
    }

    [Fact]
    public async Task AddAsync_WithDefaultTimeProvider_PopulatesTimestampWithSystemTime()
    {
        // Arrange
        await ClearDataAsync();
        var beforeAdd = DateTime.UtcNow;
        var entity = new AuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m
        };

        // Act - Repository without explicit audit context uses TimeProvider.System by default
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            // With default TimeProvider.System, CreatedAtUtc should be populated
            e.CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(beforeAdd);
            e.CreatedAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
            // No RequestContext provided, so CreatedBy should be null
            e.CreatedBy.ShouldBeNull();
        });
    }

    [Fact]
    public async Task AddRangeAsync_WithAuditContext_PopulatesCreatedFieldsForAllEntities()
    {
        // Arrange
        await ClearDataAsync();
        var entities = new[]
        {
            new AuditableProduct { Id = Guid.NewGuid(), Name = "Product 1", Price = 10m },
            new AuditableProduct { Id = Guid.NewGuid(), Name = "Product 2", Price = 20m },
            new AuditableProduct { Id = Guid.NewGuid(), Name = "Product 3", Price = 30m }
        };

        // Act
        var result = await _repositoryWithAudit.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        foreach (var entity in entities)
        {
            var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
            stored.IsRight.ShouldBeTrue();
            stored.IfRight(e =>
            {
                e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
                e.CreatedBy.ShouldBe(TestUserId);
                e.ModifiedAtUtc.ShouldBeNull();
                e.ModifiedBy.ShouldBeNull();
            });
        }
    }

    #endregion

    #region Update Operations Tests

    [Fact]
    public async Task UpdateAsync_WithAuditContext_PopulatesModifiedFields()
    {
        // Arrange
        await ClearDataAsync();
        var entity = new AuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Price = 99.99m
        };
        await _repositoryWithAudit.AddAsync(entity);

        // Advance time for update
        _fakeTimeProvider.Advance(TimeSpan.FromHours(1));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        // Update with different user
        var updateContext = Substitute.For<IRequestContext>();
        updateContext.UserId.Returns("update-user-456");
        var updateRepository = new FunctionalRepositoryDapper<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            updateContext,
            _fakeTimeProvider);

        // Modify entity
        entity.Name = "Updated Name";

        // Act
        var result = await updateRepository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            // Created fields should remain unchanged
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            e.CreatedBy.ShouldBe(TestUserId);

            // Modified fields should be updated
            e.ModifiedAtUtc.ShouldBe(updateTime);
            e.ModifiedBy.ShouldBe("update-user-456");
        });
    }

    [Fact]
    public async Task UpdateRangeAsync_WithAuditContext_PopulatesModifiedFieldsForAllEntities()
    {
        // Arrange
        await ClearDataAsync();
        var entities = new[]
        {
            new AuditableProduct { Id = Guid.NewGuid(), Name = "Product 1", Price = 10m },
            new AuditableProduct { Id = Guid.NewGuid(), Name = "Product 2", Price = 20m }
        };
        await _repositoryWithAudit.AddRangeAsync(entities);

        // Advance time for update
        _fakeTimeProvider.Advance(TimeSpan.FromHours(2));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        // Modify entities
        entities[0].Name = "Updated 1";
        entities[1].Name = "Updated 2";

        // Act
        var result = await _repositoryWithAudit.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        foreach (var entity in entities)
        {
            var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
            stored.IsRight.ShouldBeTrue();
            stored.IfRight(e =>
            {
                e.ModifiedAtUtc.ShouldBe(updateTime);
                e.ModifiedBy.ShouldBe(TestUserId);
            });
        }
    }

    #endregion

    #region Non-Auditable Entity Tests

    [Fact]
    public async Task AddAsync_NonAuditableEntity_WorksWithoutErrors()
    {
        // Arrange
        await ClearDataAsync();
        var entity = new NonAuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Non-Auditable Product",
            Price = 49.99m
        };

        // Act
        var result = await _nonAuditableRepository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _nonAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.Name.ShouldBe("Non-Auditable Product");
            e.Price.ShouldBe(49.99m);
        });
    }

    [Fact]
    public async Task UpdateAsync_NonAuditableEntity_WorksWithoutErrors()
    {
        // Arrange
        await ClearDataAsync();
        var entity = new NonAuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Price = 49.99m
        };
        await _nonAuditableRepository.AddAsync(entity);

        // Modify entity
        entity.Name = "Updated";

        // Act
        var result = await _nonAuditableRepository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _nonAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e => e.Name.ShouldBe("Updated"));
    }

    #endregion

    #region Null RequestContext Tests

    [Fact]
    public async Task AddAsync_NullRequestContext_PopulatesTimestampButNotUserId()
    {
        // Arrange
        await ClearDataAsync();

        // Repository with TimeProvider but null RequestContext
        var repositoryNullContext = new FunctionalRepositoryDapper<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            requestContext: null,
            _fakeTimeProvider);

        var entity = new AuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m
        };

        // Act
        var result = await repositoryNullContext.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await repositoryNullContext.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            e.CreatedBy.ShouldBeNull(); // No user context, should remain null
        });
    }

    [Fact]
    public async Task UpdateAsync_NullRequestContext_PopulatesTimestampButNotUserId()
    {
        // Arrange
        await ClearDataAsync();

        // Repository with TimeProvider but null RequestContext
        var repositoryNullContext = new FunctionalRepositoryDapper<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            requestContext: null,
            _fakeTimeProvider);

        var entity = new AuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Price = 99.99m
        };
        await repositoryNullContext.AddAsync(entity);

        // Advance time
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(30));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        entity.Name = "Updated";

        // Act
        var result = await repositoryNullContext.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await repositoryNullContext.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.ModifiedAtUtc.ShouldBe(updateTime);
            e.ModifiedBy.ShouldBeNull(); // No user context
        });
    }

    #endregion

    #region Partial Interface Implementation Tests

    [Fact]
    public async Task AddAsync_PartialAuditableEntity_PopulatesOnlyImplementedFields()
    {
        // Arrange
        await ClearDataAsync();
        var entity = new PartialAuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Partial Auditable Product"
        };

        // Act
        var result = await _partialAuditableRepository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _partialAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            // No CreatedBy field on this entity
        });
    }

    #endregion
}

#region Test Entities

/// <summary>
/// Entity implementing full IAuditableEntity for audit field population tests.
/// </summary>
public class AuditableProduct : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Entity that does NOT implement any audit interfaces.
/// </summary>
public class NonAuditableProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Entity implementing only ICreatedAtUtc (partial audit tracking).
/// </summary>
public class PartialAuditableProduct : ICreatedAtUtc
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

#endregion
