using System.Data;
using Dapper;
using Encina.Dapper.MySQL.Repository;
using Encina.Dapper.MySQL.UnitOfWork;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.MySQL.UnitOfWork;

/// <summary>
/// Integration tests for <see cref="UnitOfWorkDapper"/> using real MySQL.
/// </summary>
[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkDapperIntegrationTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;
    private IDbConnection _connection = null!;
    private UnitOfWorkDapper _unitOfWork = null!;
    private IServiceProvider _serviceProvider = null!;
    private IEntityMapping<TestUoWProduct, Guid> _mapping = null!;

    public UnitOfWorkDapperIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as MySqlConnection;
        if (schemaConnection != null)
        {
            await CreateTestProductsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = CreateMapping();

        var services = new ServiceCollection();
        services.AddSingleton(_mapping);
        _serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWorkDapper(_connection, _serviceProvider);
    }

    public async Task DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private static async Task CreateTestProductsSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS test_uow_products;
            CREATE TABLE test_uow_products (
                id CHAR(36) PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                price DECIMAL(18,2) NOT NULL,
                is_active BOOLEAN NOT NULL,
                created_at_utc DATETIME(6) NOT NULL
            );
            CREATE INDEX ix_test_uow_products_is_active ON test_uow_products(is_active);
            """;

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is MySqlConnection mysqlConnection)
        {
            await using var command = new MySqlCommand("DELETE FROM test_uow_products", mysqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static IEntityMapping<TestUoWProduct, Guid> CreateMapping()
    {
        return new EntityMappingBuilder<TestUoWProduct, Guid>()
            .ToTable("test_uow_products")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "name")
            .MapProperty(p => p.Price, "price")
            .MapProperty(p => p.IsActive, "is_active")
            .MapProperty(p => p.CreatedAtUtc, "created_at_utc")
            .Build();
    }

    #region Transaction Commit Tests

    [Fact]
    public async Task Transaction_CommitMultipleEntities_AllPersisted()
    {

        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestUoWProduct, Guid>();
        var entity1 = CreateTestProduct("Entity 1");
        var entity2 = CreateTestProduct("Entity 2");
        var entity3 = CreateTestProduct("Entity 3");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);
        await repository.AddAsync(entity3);

        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert
        var count = await _connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM test_uow_products");
        count.ShouldBe(3);
    }

    #endregion

    #region Transaction Rollback Tests

    [Fact]
    public async Task Transaction_Rollback_NoChangesPersisted()
    {

        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestUoWProduct, Guid>();
        var entity = CreateTestProduct("Should Not Persist");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);
        await _unitOfWork.RollbackAsync();

        // Assert
        var count = await _connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM test_uow_products");
        count.ShouldBe(0);
    }

    #endregion

    #region Repository Caching Tests

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {

        // Act
        var repository1 = _unitOfWork.Repository<TestUoWProduct, Guid>();
        var repository2 = _unitOfWork.Repository<TestUoWProduct, Guid>();

        // Assert
        repository1.ShouldBeSameAs(repository2);
    }

    #endregion

    #region Transaction State Tests

    [Fact]
    public async Task BeginTransaction_SetsHasActiveTransactionTrue()
    {

        // Arrange
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Cleanup
        await _unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Commit_ClearsHasActiveTransaction()
    {

        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.CommitAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task Rollback_ClearsHasActiveTransaction()
    {

        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginTransaction_WhenAlreadyActive_ReturnsError()
    {

        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();

        // Cleanup
        await _unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Commit_WhenNoTransaction_ReturnsError()
    {

        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private static TestUoWProduct CreateTestProduct(
        string name = "Test Product",
        bool isActive = true,
        decimal price = 99.99m)
    {
        return new TestUoWProduct
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

#region Test Entity

/// <summary>
/// Test product entity for Dapper MySQL UnitOfWork integration tests.
/// </summary>
public class TestUoWProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

#endregion
