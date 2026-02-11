using System.Data;
using System.Linq.Expressions;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Dapper.SqlServer.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryDapper{TEntity, TId}"/> using real SQL Server.
/// </summary>
[Collection("Dapper-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class FunctionalRepositoryDapperIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbConnection _connection = null!;
    private FunctionalRepositoryDapper<TestProduct, Guid> _repository = null!;
    private IEntityMapping<TestProduct, Guid> _mapping = null!;

    public FunctionalRepositoryDapperIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as SqlConnection;
        if (schemaConnection != null)
        {
            await CreateTestProductsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = new EntityMappingBuilder<TestProduct, Guid>()
            .ToTable("TestProducts")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Price, "Price")
            .MapProperty(p => p.IsActive, "IsActive")
            .MapProperty(p => p.CreatedAtUtc, "CreatedAtUtc")
            .Build();

        _repository = new FunctionalRepositoryDapper<TestProduct, Guid>(_connection, _mapping);
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private static async Task CreateTestProductsSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS TestProducts;
            CREATE TABLE TestProducts (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(200) NOT NULL,
                Price DECIMAL(18,2) NOT NULL,
                IsActive BIT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL
            );
            CREATE INDEX IX_TestProducts_IsActive ON TestProducts(IsActive);
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand("DELETE FROM TestProducts", sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
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

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeft()
    {
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

    [Fact]
    public async Task ListAsync_EmptyTable_ReturnsEmptyList()
    {
        // Arrange
        await ClearDataAsync();

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task ListAsync_WithEntities_ReturnsAllEntities()
    {
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

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
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

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsRightAndPersists()
    {
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

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRightAndUpdates()
    {
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

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsRightAndRemoves()
    {
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

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsLeft()
    {
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

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
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

    [Fact]
    public async Task AnyAsync_EmptyTable_ReturnsFalse()
    {
        // Arrange
        await ClearDataAsync();

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    [Fact]
    public async Task AnyAsync_WithEntities_ReturnsTrue()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddAsync(CreateTestProduct());

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_ReturnsTrue()
    {
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

    [Fact]
    public async Task AnyAsync_WithSpecification_NoMatch_ReturnsFalse()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddAsync(CreateTestProduct("Inactive", isActive: false));

        var spec = new ActiveProductSpec();

        // Act
        var result = await _repository.AnyAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_WithMatch_ReturnsEntity()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateTestProduct("Active", isActive: true),
            CreateTestProduct("Inactive", isActive: false)
        });

        var spec = new ActiveProductSpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.IsActive.ShouldBeTrue());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsLeft()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddAsync(CreateTestProduct("Inactive", isActive: false));

        var spec = new ActiveProductSpec();

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region CountAsync with Specification Tests

    [Fact]
    public async Task CountAsync_WithSpecification_ReturnsFilteredCount()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateTestProduct("Active 1", isActive: true),
            CreateTestProduct("Active 2", isActive: true),
            CreateTestProduct("Inactive", isActive: false)
        });

        var spec = new ActiveProductSpec();

        // Act
        var result = await _repository.CountAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

    #endregion

    #region UpdateAsync Edge Cases

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsLeft()
    {
        // Arrange
        await ClearDataAsync();
        var entity = CreateTestProduct("Non-existing");

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    #endregion

    #region UpdateRangeAsync Tests

    [Fact]
    public async Task UpdateRangeAsync_ExistingEntities_ReturnsRightAndUpdates()
    {
        // Arrange
        await ClearDataAsync();
        var entities = new[]
        {
            CreateTestProduct("Original 1"),
            CreateTestProduct("Original 2")
        };
        await _repository.AddRangeAsync(entities);

        // Modify entities
        entities[0].Name = "Updated 1";
        entities[1].Name = "Updated 2";

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored1 = await _repository.GetByIdAsync(entities[0].Id);
        stored1.IfRight(e => e.Name.ShouldBe("Updated 1"));

        var stored2 = await _repository.GetByIdAsync(entities[1].Id);
        stored2.IfRight(e => e.Name.ShouldBe("Updated 2"));
    }

    #endregion

    #region DeleteAsync with Entity Tests

    [Fact]
    public async Task DeleteAsync_WithEntity_ReturnsRightAndRemoves()
    {
        // Arrange
        await ClearDataAsync();
        var entity = CreateTestProduct();
        await _repository.AddAsync(entity);

        // Act
        var result = await _repository.DeleteAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DeleteRangeAsync Tests

    [Fact]
    public async Task DeleteRangeAsync_WithSpecification_DeletesMatchingEntities()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateTestProduct("Active 1", isActive: true),
            CreateTestProduct("Active 2", isActive: true),
            CreateTestProduct("Inactive", isActive: false)
        });

        var spec = new ActiveProductSpec();

        // Act
        var result = await _repository.DeleteRangeAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));

        var remaining = await _repository.CountAsync();
        remaining.IfRight(count => count.ShouldBe(1));
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
/// Test product entity for Dapper repository integration tests.
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
