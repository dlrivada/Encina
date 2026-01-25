using System.Data;
using Dapper;
using Encina.ADO.Sqlite.Repository;
using Encina.ADO.Sqlite.UnitOfWork;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.IntegrationTests.ADO.Sqlite.UnitOfWork;

/// <summary>
/// Integration tests for <see cref="UnitOfWorkADO"/> using real SQLite.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by test cleanup")]
public class UnitOfWorkADOIntegrationTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;
    private IDbConnection _connection = null!;
    private UnitOfWorkADO _unitOfWork = null!;
    private IServiceProvider _serviceProvider = null!;
    private IEntityMapping<TestADOProduct, Guid> _mapping = null!;

    public UnitOfWorkADOIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as SqliteConnection;
        if (schemaConnection != null)
        {
            await CreateTestProductsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = CreateMapping();

        var services = new ServiceCollection();
        services.AddSingleton(_mapping);
        _serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWorkADO(_connection, _serviceProvider);
    }

    private static async Task CreateTestProductsSchemaAsync(SqliteConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        const string sql = """
            DROP TABLE IF EXISTS TestADOProducts;
            CREATE TABLE TestADOProducts (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_TestADOProducts_IsActive ON TestADOProducts(IsActive);
            """;

        await using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqliteConnection sqliteConnection)
        {
            if (sqliteConnection.State != ConnectionState.Open)
                await sqliteConnection.OpenAsync();

            await using var command = new SqliteCommand("DELETE FROM TestADOProducts", sqliteConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static IEntityMapping<TestADOProduct, Guid> CreateMapping()
    {
        return new EntityMappingBuilder<TestADOProduct, Guid>()
            .ToTable("TestADOProducts")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Price, "Price")
            .MapProperty(p => p.IsActive, "IsActive")
            .MapProperty(p => p.CreatedAtUtc, "CreatedAtUtc")
            .Build();
    }

    #region Transaction Commit Tests

    [Fact]
    public async Task Transaction_CommitMultipleEntities_AllPersisted()
    {
        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();
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

        // Assert - Verify with a separate query
        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestADOProducts");
        count.ShouldBe(3);
    }

    #endregion

    #region Transaction Rollback Tests

    [Fact]
    public async Task Transaction_Rollback_NoChangesPersisted()
    {
        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();
        var entity = CreateTestProduct("Should Not Persist");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);

        // Rollback instead of commit
        await _unitOfWork.RollbackAsync();

        // Assert
        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestADOProducts");
        count.ShouldBe(0);
    }

    #endregion

    #region Repository Caching Tests

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        // Act
        var repository1 = _unitOfWork.Repository<TestADOProduct, Guid>();
        var repository2 = _unitOfWork.Repository<TestADOProduct, Guid>();

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

    private static TestADOProduct CreateTestProduct(
        string name = "Test Product",
        bool isActive = true,
        decimal price = 99.99m)
    {
        return new TestADOProduct
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
/// Test product entity for ADO.NET SQLite UnitOfWork integration tests.
/// </summary>
public class TestADOProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

#endregion
