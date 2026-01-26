using System.Data;
using System.Globalization;
using Encina.Dapper.Oracle.BulkOperations;
using Encina.Dapper.Oracle.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Oracle.ManagedDataAccess.Client;
using Shouldly;
using Xunit;

using HashSet = System.Collections.Generic.HashSet<string>;

namespace Encina.IntegrationTests.Infrastructure.Dapper.Oracle.BulkOperations;

/// <summary>
/// Integration tests for <see cref="BulkOperationsDapper{TEntity, TId}"/> with real Oracle.
/// Uses Testcontainers to spin up a throwaway Oracle instance.
/// </summary>
/// <remarks>
/// These tests verify the actual Array Binding (ODP.NET), MERGE statements,
/// and bulk DELETE operations work correctly against a real Oracle database using Dapper.
/// Oracle tests may take longer to initialize due to database startup time.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.Oracle")]
[Trait("Feature", "BulkOperations")]
public sealed class BulkOperationsDapperOracleIntegrationTests : IClassFixture<OracleFixture>, IAsyncLifetime
{
    private readonly OracleFixture _fixture;
    private IDbConnection _connection = null!;
    private BulkOperationsDapper<BulkTestOrder, Guid> _bulkOps = null!;

    public BulkOperationsDapperOracleIntegrationTests(OracleFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _connection = _fixture.CreateConnection();

        // Create test table
        await CreateBulkTestTableAsync();

        // Create entity mapping
        var mapping = new BulkTestOrderMapping();
        _bulkOps = new BulkOperationsDapper<BulkTestOrder, Guid>(_connection, mapping);
    }

    public async Task DisposeAsync()
    {
        await DropBulkTestTableAsync();
        _connection?.Dispose();
        // Note: Do NOT call _fixture.DisposeAsync() here.
        // When using IClassFixture, xUnit manages the fixture lifecycle.
        await Task.CompletedTask;
    }

    private async Task CreateBulkTestTableAsync()
    {
        const string dropSql = """
            BEGIN
                EXECUTE IMMEDIATE 'DROP TABLE BulkTestOrders';
            EXCEPTION
                WHEN OTHERS THEN NULL;
            END;
            """;

        const string createSql = """
            CREATE TABLE BulkTestOrders (
                Id RAW(16) PRIMARY KEY,
                CustomerName VARCHAR2(200) NOT NULL,
                Amount NUMBER(18,2) NOT NULL,
                IsActive NUMBER(1) NOT NULL,
                CreatedAtUtc TIMESTAMP NOT NULL
            )
            """;

        if (_connection is OracleConnection oracleConnection)
        {
            await using var dropCmd = oracleConnection.CreateCommand();
            dropCmd.CommandText = dropSql;
            await dropCmd.ExecuteNonQueryAsync();

            await using var createCmd = oracleConnection.CreateCommand();
            createCmd.CommandText = createSql;
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    private async Task DropBulkTestTableAsync()
    {
        const string sql = """
            BEGIN
                EXECUTE IMMEDIATE 'DROP TABLE BulkTestOrders';
            EXCEPTION
                WHEN OTHERS THEN NULL;
            END;
            """;

        if (_connection.State == ConnectionState.Open && _connection is OracleConnection oracleConnection)
        {
            await using var command = oracleConnection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<int> GetRowCountAsync()
    {
        if (_connection is OracleConnection oracleConnection)
        {
            await using var command = oracleConnection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM BulkTestOrders";
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        return 0;
    }

    private static List<BulkTestOrder> CreateOrders(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new BulkTestOrder
            {
                Id = Guid.NewGuid(),
                CustomerName = $"Customer_{i}",
                Amount = (i + 1) * 10.50m,
                IsActive = i % 2 == 0,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }

    #region BulkInsertAsync Tests

    [Fact]
    public async Task BulkInsertAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestOrder>();

        // Act
        var result = await _bulkOps.BulkInsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task BulkInsertAsync_SingleEntity_InsertsSuccessfully()
    {
        // Arrange
        var order = new BulkTestOrder
        {
            Id = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Amount = 99.99m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = await _bulkOps.BulkInsertAsync([order]);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1));

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(1);
    }

    [Fact]
    public async Task BulkInsertAsync_100Entities_InsertsAllSuccessfully()
    {
        // Arrange
        var orders = CreateOrders(100);

        // Act
        var result = await _bulkOps.BulkInsertAsync(orders);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(100));

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(100);
    }

    [Fact]
    public async Task BulkInsertAsync_500Entities_InsertsAllSuccessfully()
    {
        // Arrange
        var orders = CreateOrders(500);

        // Act
        var result = await _bulkOps.BulkInsertAsync(orders);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(500));

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(500);
    }

    [Fact]
    public async Task BulkInsertAsync_WithCustomBatchSize_InsertsSuccessfully()
    {
        // Arrange
        var orders = CreateOrders(250);
        var config = BulkConfig.Default with { BatchSize = 50 };

        // Act
        var result = await _bulkOps.BulkInsertAsync(orders, config);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(250));
    }

    #endregion

    #region BulkReadAsync Tests

    [Fact]
    public async Task BulkReadAsync_EmptyIds_ReturnsEmptyCollection()
    {
        // Arrange
        var ids = new List<object>();

        // Act
        var result = await _bulkOps.BulkReadAsync(ids);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task BulkReadAsync_ExistingIds_ReturnsEntities()
    {
        // Arrange
        var orders = CreateOrders(10);
        await _bulkOps.BulkInsertAsync(orders);

        var idsToRead = orders.Take(5).Select(o => (object)o.Id).ToList();

        // Act
        var result = await _bulkOps.BulkReadAsync(idsToRead);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities =>
        {
            entities.Count.ShouldBe(5);
            entities.All(e => orders.Any(o => o.Id == e.Id)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task BulkReadAsync_MixedExistingAndNonExistingIds_ReturnsOnlyExisting()
    {
        // Arrange
        var orders = CreateOrders(5);
        await _bulkOps.BulkInsertAsync(orders);

        var idsToRead = orders.Take(3).Select(o => (object)o.Id).ToList();
        idsToRead.Add(Guid.NewGuid()); // Non-existing ID
        idsToRead.Add(Guid.NewGuid()); // Another non-existing ID

        // Act
        var result = await _bulkOps.BulkReadAsync(idsToRead);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.Count.ShouldBe(3));
    }

    #endregion

    #region BulkUpdateAsync Tests

    [Fact]
    public async Task BulkUpdateAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestOrder>();

        // Act
        var result = await _bulkOps.BulkUpdateAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task BulkUpdateAsync_ExistingEntities_UpdatesSuccessfully()
    {
        // Arrange
        var orders = CreateOrders(10);
        await _bulkOps.BulkInsertAsync(orders);

        // Modify entities
        foreach (var order in orders)
        {
            order.CustomerName = $"Updated_{order.CustomerName}";
            order.Amount += 100;
        }

        // Act
        var result = await _bulkOps.BulkUpdateAsync(orders);

        // Assert
        var updateCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkUpdate failed: {error.Message}")
        );
        updateCount.ShouldBe(10);

        // Verify updates persisted
        var readResult = await _bulkOps.BulkReadAsync(orders.Select(o => (object)o.Id).ToList());
        readResult.IfRight(entities =>
        {
            entities.All(e => e.CustomerName.StartsWith("Updated_", StringComparison.Ordinal)).ShouldBeTrue();
        });
    }

    #endregion

    #region BulkDeleteAsync Tests

    [Fact]
    public async Task BulkDeleteAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestOrder>();

        // Act
        var result = await _bulkOps.BulkDeleteAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task BulkDeleteAsync_ExistingEntities_DeletesSuccessfully()
    {
        // Arrange
        var orders = CreateOrders(20);
        await _bulkOps.BulkInsertAsync(orders);

        var ordersToDelete = orders.Take(10).ToList();

        // Act
        var result = await _bulkOps.BulkDeleteAsync(ordersToDelete);

        // Assert
        var deleteCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkDelete failed: {error.Message}")
        );
        deleteCount.ShouldBe(10);

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(10);
    }

    #endregion

    #region BulkMergeAsync Tests

    [Fact]
    public async Task BulkMergeAsync_EmptyCollection_ReturnsRightWithZero()
    {
        // Arrange
        var entities = new List<BulkTestOrder>();

        // Act
        var result = await _bulkOps.BulkMergeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task BulkMergeAsync_NewEntities_InsertsSuccessfully()
    {
        // Arrange
        var orders = CreateOrders(10);

        // Act
        var result = await _bulkOps.BulkMergeAsync(orders);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(10));

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(10);
    }

    [Fact]
    public async Task BulkMergeAsync_MixedNewAndExisting_InsertsAndUpdates()
    {
        // Arrange
        var existingOrders = CreateOrders(5);
        await _bulkOps.BulkInsertAsync(existingOrders);

        // Modify existing orders
        foreach (var order in existingOrders)
        {
            order.CustomerName = $"Merged_{order.CustomerName}";
        }

        // Add new orders
        var newOrders = CreateOrders(5);
        var allOrders = existingOrders.Concat(newOrders).ToList();

        // Act
        var result = await _bulkOps.BulkMergeAsync(allOrders);

        // Assert
        var mergeCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkMerge failed: {error.Message}")
        );
        mergeCount.ShouldBe(10); // 5 updates + 5 inserts

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(10);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task BulkInsertAsync_DuplicatePrimaryKey_ReturnsLeftWithError()
    {
        // Arrange
        var order = new BulkTestOrder
        {
            Id = Guid.NewGuid(),
            CustomerName = "Test",
            Amount = 100m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _bulkOps.BulkInsertAsync([order]);

        // Try to insert the same order again
        var duplicateOrder = new BulkTestOrder
        {
            Id = order.Id, // Same ID
            CustomerName = "Duplicate",
            Amount = 200m,
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = await _bulkOps.BulkInsertAsync([duplicateOrder]);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.AlreadyExistsErrorCode));
        });
    }

    #endregion
}

#region Test Entity and Mapping

/// <summary>
/// Test entity for bulk operations integration tests.
/// </summary>
public class BulkTestOrder
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Entity mapping for BulkTestOrder.
/// </summary>
/// <remarks>
/// Oracle converts unquoted identifiers to uppercase. Since the table is created without
/// quoted identifiers, column names are stored as uppercase (e.g., ID, CUSTOMERNAME).
/// The mapping must use uppercase column names to match what Oracle stores.
/// See: https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Database-Object-Names-and-Qualifiers.html
/// </remarks>
public class BulkTestOrderMapping : IEntityMapping<BulkTestOrder, Guid>
{
    public string TableName => "BULKTESTORDERS";
    public string IdColumnName => "ID";

    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        ["Id"] = "ID",
        ["CustomerName"] = "CUSTOMERNAME",
        ["Amount"] = "AMOUNT",
        ["IsActive"] = "ISACTIVE",
        ["CreatedAtUtc"] = "CREATEDATUTC"
    };

    public Guid GetId(BulkTestOrder entity) => entity.Id;

    public IReadOnlySet<string> InsertExcludedProperties { get; } = new HashSet();
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new HashSet { "Id", "CreatedAtUtc" };
}

#endregion
