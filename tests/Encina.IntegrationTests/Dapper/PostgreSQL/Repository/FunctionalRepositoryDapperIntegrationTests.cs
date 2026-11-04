using System.Data;
using System.Linq.Expressions;
using Encina.Dapper.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.PostgreSQL.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryDapper{TEntity, TId}"/> using real PostgreSQL.
/// </summary>
[Collection("Dapper-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public class FunctionalRepositoryDapperIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private IDbConnection _connection = null!;
    private FunctionalRepositoryDapper<TestProduct, Guid> _repository = null!;
    private IEntityMapping<TestProduct, Guid> _mapping = null!;

    public FunctionalRepositoryDapperIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as NpgsqlConnection;
        if (schemaConnection != null)
        {
            await CreateTestProductsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = new EntityMappingBuilder<TestProduct, Guid>()
            .ToTable("test_products")
            .HasId(p => p.Id, "id")  // PostgreSQL is case-sensitive with quoted identifiers
            .MapProperty(p => p.Name, "name")
            .MapProperty(p => p.Price, "price")
            .MapProperty(p => p.IsActive, "is_active")
            .MapProperty(p => p.CreatedAtUtc, "created_at_utc")
            .Build()
            .ShouldBeSuccess();

        _repository = new FunctionalRepositoryDapper<TestProduct, Guid>(_connection, _mapping);
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private static async Task CreateTestProductsSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS test_products;
            CREATE TABLE test_products (
                id UUID PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                price DECIMAL(18,2) NOT NULL,
                is_active BOOLEAN NOT NULL,
                created_at_utc TIMESTAMPTZ NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_test_products_is_active ON test_products(is_active);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = new NpgsqlCommand("DELETE FROM test_products", npgsqlConnection);
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

    #endregion

    #region String Method Wildcard Tests

    // Each value would also match other names if its LIKE wildcards ('%', '_', SQL Server's '[')
    // or the '!' escape character were not matched literally (#3156).
    private static readonly string[] Names = ["500", "50% off", "100%", "a_b", "axb", "[a]", "hey!"];

    [Theory]
    [InlineData(nameof(string.Contains), "50%", "50% off")]
    [InlineData(nameof(string.StartsWith), "a_", "a_b")]
    [InlineData(nameof(string.EndsWith), "0%", "100%")]
    [InlineData(nameof(string.Contains), "[a]", "[a]")]
    [InlineData(nameof(string.Contains), "!", "hey!")]
    public async Task ListAsync_StringMethodSpecification_MatchesLikeWildcardsLiterally(string method, string value, string expectedName)
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(Names.Select(name => CreateTestProduct(name)).ToArray());
        var spec = new ProductNameSpec(method, value);

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Select(e => e.Name).ToList().ShouldBe([expectedName]));
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
/// Test product entity for Dapper PostgreSQL repository integration tests.
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

/// <summary>
/// Specification matching a TestProduct whose name contains, starts with or ends with a value.
/// </summary>
public class ProductNameSpec(string method, string value) : Specification<TestProduct>
{
    public override Expression<Func<TestProduct, bool>> ToExpression() => method switch
    {
        nameof(string.Contains) => e => e.Name.Contains(value),
        nameof(string.StartsWith) => e => e.Name.StartsWith(value),
        nameof(string.EndsWith) => e => e.Name.EndsWith(value),
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Expected Contains, StartsWith or EndsWith."),
    };
}

#endregion
