using System.Data;
using System.Linq.Expressions;
using Encina.Dapper.Oracle.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Oracle.ManagedDataAccess.Client;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.Oracle.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryDapper{TEntity, TId}"/> using real Oracle.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Oracle")]
public class FunctionalRepositoryDapperIntegrationTests : IAsyncLifetime
{
    private readonly OracleFixture _fixture = new();
    private IDbConnection _connection = null!;
    private FunctionalRepositoryDapper<TestProduct, Guid> _repository = null!;
    private IEntityMapping<TestProduct, Guid> _mapping = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as OracleConnection;
        if (schemaConnection != null)
        {
            await CreateTestProductsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = new EntityMappingBuilder<TestProduct, Guid>()
            .ToTable("TEST_PRODUCTS")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "NAME")
            .MapProperty(p => p.Price, "PRICE")
            .MapProperty(p => p.IsActive, "IS_ACTIVE")
            .MapProperty(p => p.CreatedAtUtc, "CREATED_AT_UTC")
            .Build();

        _repository = new FunctionalRepositoryDapper<TestProduct, Guid>(_connection, _mapping);
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.DisposeAsync();
    }

    private static async Task CreateTestProductsSchemaAsync(OracleConnection connection)
    {
        // Drop table if exists
        try
        {
            await using var dropCommand = new OracleCommand("DROP TABLE TEST_PRODUCTS", connection);
            await dropCommand.ExecuteNonQueryAsync();
        }
        catch (OracleException)
        {
            // Table doesn't exist, ignore
        }

        const string createSql = """
            CREATE TABLE TEST_PRODUCTS (
                ID RAW(16) PRIMARY KEY,
                NAME VARCHAR2(200) NOT NULL,
                PRICE NUMBER(18,2) NOT NULL,
                IS_ACTIVE NUMBER(1) NOT NULL,
                CREATED_AT_UTC TIMESTAMP NOT NULL
            )
            """;

        await using var createCommand = new OracleCommand(createSql, connection);
        await createCommand.ExecuteNonQueryAsync();

        const string indexSql = "CREATE INDEX IX_TEST_PRODUCTS_IS_ACTIVE ON TEST_PRODUCTS(IS_ACTIVE)";
        await using var indexCommand = new OracleCommand(indexSql, connection);
        await indexCommand.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is OracleConnection oracleConnection)
        {
            await using var command = new OracleCommand("DELETE FROM TEST_PRODUCTS", oracleConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    #region GetByIdAsync Tests

    [SkippableFact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        var entity = CreateTestProduct();
        await _repository.AddAsync(entity);

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(entity.Id);
            e.Name.ShouldBe(entity.Name);
        });
    }

    [SkippableFact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeft()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region ListAsync Tests

    [SkippableFact]
    public async Task ListAsync_EmptyTable_ReturnsEmptyList()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [SkippableFact]
    public async Task ListAsync_WithEntities_ReturnsAllEntities()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateTestProduct("Product 1"),
            CreateTestProduct("Product 2"),
            CreateTestProduct("Product 3")
        });

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [SkippableFact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateTestProduct("Active", isActive: true),
            CreateTestProduct("Inactive", isActive: false)
        });

        var spec = new ActiveProductSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Active");
        });
    }

    #endregion

    #region AddAsync Tests

    [SkippableFact]
    public async Task AddAsync_ValidEntity_ReturnsRightAndPersists()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        var entity = CreateTestProduct("New Product");

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("New Product"));

        // Verify persisted
        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
    }

    #endregion

    #region UpdateAsync Tests

    [SkippableFact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRightAndUpdates()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        var entity = CreateTestProduct("Original");
        await _repository.AddAsync(entity);

        // Modify entity
        entity.Name = "Updated";

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e => e.Name.ShouldBe("Updated"));
    }

    #endregion

    #region DeleteAsync Tests

    [SkippableFact]
    public async Task DeleteAsync_ExistingEntity_ReturnsRightAndRemoves()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        var entity = CreateTestProduct();
        await _repository.AddAsync(entity);

        // Act
        var result = await _repository.DeleteAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsLeft.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsLeft()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region CountAsync Tests

    [SkippableFact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateTestProduct("Product 1"),
            CreateTestProduct("Product 2"),
            CreateTestProduct("Product 3")
        });

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    #endregion

    #region AnyAsync Tests

    [SkippableFact]
    public async Task AnyAsync_EmptyTable_ReturnsFalse()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [SkippableFact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        await _repository.AddAsync(CreateTestProduct());

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [SkippableFact]
    public async Task AnyAsync_WithSpecification_ReturnsTrue()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateTestProduct("Active", isActive: true),
            CreateTestProduct("Inactive", isActive: false)
        });

        var spec = new ActiveProductSpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    #endregion

    #region Helper Methods

    private static TestProduct CreateTestProduct(
        string name = "Test Product",
        bool isActive = true,
        decimal price = 99.99m)
    {
        return new TestProduct
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}

#region Test Entity and Specifications

/// <summary>
/// Test product entity for Dapper Oracle repository integration tests.
/// </summary>
public class TestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for active products.
/// </summary>
public class ActiveProductSpec : Specification<TestProduct>
{
    public override Expression<Func<TestProduct, bool>> ToExpression()
        => p => p.IsActive;
}

#endregion
