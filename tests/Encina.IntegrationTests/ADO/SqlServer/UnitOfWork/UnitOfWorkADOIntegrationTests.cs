using System.Data;
using Dapper;
using Encina.ADO.SqlServer.Repository;
using Encina.ADO.SqlServer.UnitOfWork;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.SqlServer.UnitOfWork;

/// <summary>
/// Integration tests for <see cref="UnitOfWorkADO"/> using real SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkADOIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbConnection _connection = null!;
    private UnitOfWorkADO _unitOfWork = null!;
    private IServiceProvider _serviceProvider = null!;
    private IEntityMapping<TestADOProduct, Guid> _mapping = null!;

    public UnitOfWorkADOIntegrationTests(SqlServerFixture fixture)
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
        _mapping = CreateMapping();

        var services = new ServiceCollection();
        services.AddSingleton(_mapping);
        _serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWorkADO(_connection, _serviceProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private static async Task CreateTestProductsSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS TestADOProducts;
            CREATE TABLE TestADOProducts (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(200) NOT NULL,
                Price DECIMAL(18,2) NOT NULL,
                IsActive BIT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL
            );
            CREATE INDEX IX_TestADOProducts_IsActive ON TestADOProducts(IsActive);
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand("DELETE FROM TestADOProducts", sqlConnection);
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

    [Fact]
    public async Task Transaction_ModifyEntities_ChangesPersisted()
    {
        // Arrange - Create initial entity
        await ClearDataAsync();
        var entity = CreateTestProduct("Original Name");

        // Insert directly
        await _connection.ExecuteAsync(
            "INSERT INTO TestADOProducts (Id, Name, Price, IsActive, CreatedAtUtc) VALUES (@Id, @Name, @Price, @IsActive, @CreatedAtUtc)",
            entity);

        // Act - Modify in transaction
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();
        var getResult = await repository.GetByIdAsync(entity.Id);
        getResult.IsRight.ShouldBeTrue();

        // Update the entity
        entity.Name = "Updated Name";
        await repository.UpdateAsync(entity);

        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert
        var updated = await _connection.QuerySingleAsync<TestADOProduct>(
            "SELECT * FROM TestADOProducts WHERE Id = @Id", new { entity.Id });
        updated.Name.ShouldBe("Updated Name");
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

    [Fact]
    public async Task Transaction_RollbackAfterModify_OriginalValuePreserved()
    {
        // Arrange - Create initial entity
        await ClearDataAsync();
        var entity = CreateTestProduct("Original Name");

        await _connection.ExecuteAsync(
            "INSERT INTO TestADOProducts (Id, Name, Price, IsActive, CreatedAtUtc) VALUES (@Id, @Name, @Price, @IsActive, @CreatedAtUtc)",
            entity);

        // Act - Modify in transaction then rollback
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();
        entity.Name = "Modified Name";
        await repository.UpdateAsync(entity);

        await _unitOfWork.RollbackAsync();

        // Assert - Original value preserved
        var persisted = await _connection.QuerySingleAsync<TestADOProduct>(
            "SELECT * FROM TestADOProducts WHERE Id = @Id", new { entity.Id });
        persisted.Name.ShouldBe("Original Name");
    }

    #endregion

    #region Auto-Rollback on Dispose Tests

    [Fact]
    public async Task Dispose_WithUncommittedTransaction_AutoRollback()
    {
        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();
        var entity = CreateTestProduct("Uncommitted Entity");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);

        // Don't commit - just dispose
        await _unitOfWork.DisposeAsync();

        // Assert - Create a new connection to check
        using var verifyConnection = _fixture.CreateConnection();
        var count = await verifyConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestADOProducts");
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
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionAlreadyActiveErrorCode));
        });
    }

    [Fact]
    public async Task Commit_WhenNoTransaction_ReturnsError()
    {
        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode));
        });
    }

    #endregion

    #region Transaction Parameter Passed to Repository Tests

    [Fact]
    public async Task Repository_WithTransaction_OperationsUseTransaction()
    {
        // This test verifies that repository operations participate in the transaction
        // by checking that uncommitted data is visible within the same transaction
        // but not from outside

        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();
        var entity = CreateTestProduct("Transaction Test");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);

        // Within the transaction, data should be visible
        var getResult = await repository.GetByIdAsync(entity.Id);
        getResult.IsRight.ShouldBeTrue();

        // From a separate connection, data should NOT be visible (uncommitted)
        // Use READPAST hint to skip locked rows instead of blocking
        using var separateConnection = _fixture.CreateConnection();
        var outsideCount = await separateConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TestADOProducts WITH (READPAST) WHERE Id = @Id",
            new { entity.Id });
        outsideCount.ShouldBe(0);

        // Commit and verify
        await _unitOfWork.CommitAsync();

        // Now it should be visible from outside
        var afterCommitCount = await separateConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TestADOProducts WHERE Id = @Id",
            new { entity.Id });
        afterCommitCount.ShouldBe(1);
    }

    #endregion

    #region Complex Workflow Tests

    [Fact]
    public async Task ComplexWorkflow_MultipleOperations_CommitPreservesAll()
    {
        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();

        // Act - Begin transaction
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        // Add entities
        var entity1 = CreateTestProduct("First");
        var entity2 = CreateTestProduct("Second");
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Modify one entity
        entity1.Name = "First Modified";
        await repository.UpdateAsync(entity1);

        // Add another entity
        var entity3 = CreateTestProduct("Third");
        await repository.AddAsync(entity3);

        // Commit
        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert - All changes persisted
        var entities = (await _connection.QueryAsync<TestADOProduct>("SELECT * FROM TestADOProducts")).ToList();
        entities.Count.ShouldBe(3);
        entities.ShouldContain(e => e.Name == "First Modified");
        entities.ShouldContain(e => e.Name == "Second");
        entities.ShouldContain(e => e.Name == "Third");
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
/// Test product entity for ADO.NET UnitOfWork integration tests.
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
