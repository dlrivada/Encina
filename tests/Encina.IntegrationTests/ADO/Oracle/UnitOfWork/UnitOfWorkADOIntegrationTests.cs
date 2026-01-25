using System.Data;
using Dapper;
using Encina.ADO.Oracle.Repository;
using Encina.ADO.Oracle.UnitOfWork;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;
using Shouldly;

namespace Encina.IntegrationTests.ADO.Oracle.UnitOfWork;

/// <summary>
/// Integration tests for <see cref="UnitOfWorkADO"/> using real Oracle.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Oracle")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkADOIntegrationTests : IAsyncLifetime
{
    private readonly OracleFixture _fixture = new();
    private IDbConnection _connection = null!;
    private UnitOfWorkADO _unitOfWork = null!;
    private IServiceProvider _serviceProvider = null!;
    private IEntityMapping<TestADOProduct, Guid> _mapping = null!;

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
        _mapping = CreateMapping();

        var services = new ServiceCollection();
        services.AddSingleton(_mapping);
        _serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWorkADO(_connection, _serviceProvider);
    }

    public async Task DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        _connection?.Dispose();
        await _fixture.DisposeAsync();
    }

    private static async Task CreateTestProductsSchemaAsync(OracleConnection connection)
    {
        // Drop table if exists
        try
        {
            await using var dropCommand = new OracleCommand("DROP TABLE TEST_ADO_PRODUCTS", connection);
            await dropCommand.ExecuteNonQueryAsync();
        }
        catch (OracleException)
        {
            // Table doesn't exist, ignore
        }

        const string createSql = """
            CREATE TABLE TEST_ADO_PRODUCTS (
                ID RAW(16) PRIMARY KEY,
                NAME VARCHAR2(200) NOT NULL,
                PRICE NUMBER(18,2) NOT NULL,
                IS_ACTIVE NUMBER(1) NOT NULL,
                CREATED_AT_UTC TIMESTAMP NOT NULL
            )
            """;

        await using var createCommand = new OracleCommand(createSql, connection);
        await createCommand.ExecuteNonQueryAsync();

        const string indexSql = "CREATE INDEX IX_TEST_ADO_PRODUCTS_IS_ACTIVE ON TEST_ADO_PRODUCTS(IS_ACTIVE)";
        await using var indexCommand = new OracleCommand(indexSql, connection);
        await indexCommand.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is OracleConnection oracleConnection)
        {
            await using var command = new OracleCommand("DELETE FROM TEST_ADO_PRODUCTS", oracleConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static IEntityMapping<TestADOProduct, Guid> CreateMapping()
    {
        return new EntityMappingBuilder<TestADOProduct, Guid>()
            .ToTable("TEST_ADO_PRODUCTS")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "NAME")
            .MapProperty(p => p.Price, "PRICE")
            .MapProperty(p => p.IsActive, "IS_ACTIVE")
            .MapProperty(p => p.CreatedAtUtc, "CREATED_AT_UTC")
            .Build();
    }

    #region Transaction Commit Tests

    [SkippableFact]
    public async Task Transaction_CommitMultipleEntities_AllPersisted()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

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

        // Assert
        var count = await _connection.ExecuteScalarAsync<decimal>("SELECT COUNT(*) FROM TEST_ADO_PRODUCTS");
        count.ShouldBe(3);
    }

    #endregion

    #region Transaction Rollback Tests

    [SkippableFact]
    public async Task Transaction_Rollback_NoChangesPersisted()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await ClearDataAsync();
        var repository = _unitOfWork.Repository<TestADOProduct, Guid>();
        var entity = CreateTestProduct("Should Not Persist");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);
        await _unitOfWork.RollbackAsync();

        // Assert
        var count = await _connection.ExecuteScalarAsync<decimal>("SELECT COUNT(*) FROM TEST_ADO_PRODUCTS");
        count.ShouldBe(0);
    }

    #endregion

    #region Repository Caching Tests

    [SkippableFact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Act
        var repository1 = _unitOfWork.Repository<TestADOProduct, Guid>();
        var repository2 = _unitOfWork.Repository<TestADOProduct, Guid>();

        // Assert
        repository1.ShouldBeSameAs(repository2);
    }

    #endregion

    #region Transaction State Tests

    [SkippableFact]
    public async Task BeginTransaction_SetsHasActiveTransactionTrue()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

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

    [SkippableFact]
    public async Task Commit_ClearsHasActiveTransaction()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.CommitAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task Rollback_ClearsHasActiveTransaction()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task BeginTransaction_WhenAlreadyActive_ReturnsError()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();

        // Cleanup
        await _unitOfWork.RollbackAsync();
    }

    [SkippableFact]
    public async Task Commit_WhenNoTransaction_ReturnsError()
    {
        Skip.IfNot(_fixture.IsAvailable, "Oracle container not available");

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
/// Test product entity for ADO.NET Oracle UnitOfWork integration tests.
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
