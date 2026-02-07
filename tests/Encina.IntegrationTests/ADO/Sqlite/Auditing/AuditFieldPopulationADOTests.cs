using System.Data;
using Encina;
using Encina.ADO.Sqlite.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Encina.Testing.Time;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.Sqlite.Auditing;

/// <summary>
/// Integration tests for audit field auto-population in ADO.NET SQLite repository.
/// Tests verify that IAuditableEntity fields are correctly populated during CRUD operations.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("ADO-Sqlite")]
public class AuditFieldPopulationADOTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;
    private IDbConnection _connection = null!;
    private FunctionalRepositoryADO<AuditableProduct, Guid> _repository = null!;
    private FunctionalRepositoryADO<AuditableProduct, Guid> _repositoryWithAudit = null!;
    private FunctionalRepositoryADO<NonAuditableProduct, Guid> _nonAuditableRepository = null!;
    private IEntityMapping<AuditableProduct, Guid> _auditableMapping = null!;
    private IEntityMapping<NonAuditableProduct, Guid> _nonAuditableMapping = null!;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IRequestContext _mockRequestContext;
    private static readonly DateTimeOffset FixedTime = new(2024, 7, 20, 14, 45, 0, TimeSpan.Zero);
    private const string TestUserId = "ado-test-user-789";

    public AuditFieldPopulationADOTests(SqliteFixture fixture)
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
            .ToTable("AuditableProductsADO")
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
            .ToTable("NonAuditableProductsADO")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Price, "Price")
            .Build();

        // Repository without audit context (default behavior)
        _repository = new FunctionalRepositoryADO<AuditableProduct, Guid>(_connection, _auditableMapping);

        // Repository with audit context
        _repositoryWithAudit = new FunctionalRepositoryADO<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            _mockRequestContext,
            _fakeTimeProvider);

        // Non-auditable repository
        _nonAuditableRepository = new FunctionalRepositoryADO<NonAuditableProduct, Guid>(
            _connection,
            _nonAuditableMapping,
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
            DROP TABLE IF EXISTS AuditableProductsADO;
            CREATE TABLE AuditableProductsADO (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CreatedBy TEXT,
                ModifiedAtUtc TEXT,
                ModifiedBy TEXT
            );

            DROP TABLE IF EXISTS NonAuditableProductsADO;
            CREATE TABLE NonAuditableProductsADO (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Price REAL NOT NULL
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
                "DELETE FROM AuditableProductsADO; DELETE FROM NonAuditableProductsADO;",
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
            Name = "ADO Test Product",
            Price = 149.99m
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
    public async Task AddRangeAsync_WithAuditContext_PopulatesCreatedFieldsForAllEntities()
    {
        // Arrange
        await ClearDataAsync();
        var entities = new[]
        {
            new AuditableProduct { Id = Guid.NewGuid(), Name = "ADO Product 1", Price = 10m },
            new AuditableProduct { Id = Guid.NewGuid(), Name = "ADO Product 2", Price = 20m },
            new AuditableProduct { Id = Guid.NewGuid(), Name = "ADO Product 3", Price = 30m }
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
            Name = "Original ADO Name",
            Price = 99.99m
        };
        await _repositoryWithAudit.AddAsync(entity);

        // Advance time for update
        _fakeTimeProvider.Advance(TimeSpan.FromHours(2));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        // Update with different user
        var updateContext = Substitute.For<IRequestContext>();
        updateContext.UserId.Returns("ado-update-user");
        var updateRepository = new FunctionalRepositoryADO<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            updateContext,
            _fakeTimeProvider);

        // Modify entity
        entity.Name = "Updated ADO Name";

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
            e.ModifiedBy.ShouldBe("ado-update-user");
        });
    }

    [Fact]
    public async Task UpdateRangeAsync_WithAuditContext_PopulatesModifiedFieldsForAllEntities()
    {
        // Arrange
        await ClearDataAsync();
        var entities = new[]
        {
            new AuditableProduct { Id = Guid.NewGuid(), Name = "ADO Product 1", Price = 10m },
            new AuditableProduct { Id = Guid.NewGuid(), Name = "ADO Product 2", Price = 20m }
        };
        await _repositoryWithAudit.AddRangeAsync(entities);

        // Advance time for update
        _fakeTimeProvider.Advance(TimeSpan.FromHours(3));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        // Modify entities
        entities[0].Name = "Updated ADO 1";
        entities[1].Name = "Updated ADO 2";

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
            Name = "Non-Auditable ADO Product",
            Price = 59.99m
        };

        // Act
        var result = await _nonAuditableRepository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _nonAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.Name.ShouldBe("Non-Auditable ADO Product");
            e.Price.ShouldBe(59.99m);
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
            Name = "Original ADO",
            Price = 59.99m
        };
        await _nonAuditableRepository.AddAsync(entity);

        // Modify entity
        entity.Name = "Updated ADO";

        // Act
        var result = await _nonAuditableRepository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _nonAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e => e.Name.ShouldBe("Updated ADO"));
    }

    #endregion

    #region Null RequestContext Tests

    [Fact]
    public async Task AddAsync_NullRequestContext_PopulatesTimestampButNotUserId()
    {
        // Arrange
        await ClearDataAsync();

        // Repository with TimeProvider but null RequestContext
        var repositoryNullContext = new FunctionalRepositoryADO<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            requestContext: null,
            _fakeTimeProvider);

        var entity = new AuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "ADO Test Product",
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
        var repositoryNullContext = new FunctionalRepositoryADO<AuditableProduct, Guid>(
            _connection,
            _auditableMapping,
            requestContext: null,
            _fakeTimeProvider);

        var entity = new AuditableProduct
        {
            Id = Guid.NewGuid(),
            Name = "Original ADO",
            Price = 99.99m
        };
        await repositoryNullContext.AddAsync(entity);

        // Advance time
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(45));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        entity.Name = "Updated ADO";

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
}

#region Test Entities

/// <summary>
/// Entity implementing full IAuditableEntity for ADO.NET audit field population tests.
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
/// Entity that does NOT implement any audit interfaces for ADO.NET tests.
/// </summary>
public class NonAuditableProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

#endregion
