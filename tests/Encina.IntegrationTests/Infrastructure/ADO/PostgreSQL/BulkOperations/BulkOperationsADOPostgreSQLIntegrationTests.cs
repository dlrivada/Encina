using System.Data;
using System.Globalization;
using Encina.ADO.PostgreSQL.BulkOperations;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;
using Xunit;

using HashSet = System.Collections.Generic.HashSet<string>;

namespace Encina.IntegrationTests.Infrastructure.ADO.PostgreSQL.BulkOperations;

/// <summary>
/// Integration tests for <see cref="BulkOperationsPostgreSQL{TEntity, TId}"/> with real PostgreSQL.
/// Uses Testcontainers to spin up a throwaway PostgreSQL instance.
/// </summary>
/// <remarks>
/// These tests verify the actual COPY command, UPDATE FROM, DELETE USING, and ON CONFLICT
/// operations work correctly against a real PostgreSQL database.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.PostgreSQL")]
[Trait("Feature", "BulkOperations")]
public sealed class BulkOperationsADOPostgreSQLIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private IDbConnection _connection = null!;
    private BulkOperationsPostgreSQL<BulkTestOrder, Guid> _bulkOps = null!;

    public BulkOperationsADOPostgreSQLIntegrationTests(PostgreSqlFixture fixture)
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
        _bulkOps = new BulkOperationsPostgreSQL<BulkTestOrder, Guid>(_connection, mapping);
    }

    public async Task DisposeAsync()
    {
        await DropBulkTestTableAsync();
        _connection?.Dispose();
        await _fixture.DisposeAsync();
    }

    private async Task CreateBulkTestTableAsync()
    {
        const string sql = """
            DROP TABLE IF EXISTS "BulkTestOrders";

            CREATE TABLE "BulkTestOrders" (
                "Id" UUID PRIMARY KEY,
                "CustomerName" TEXT NOT NULL,
                "Amount" NUMERIC(18,2) NOT NULL,
                "IsActive" BOOLEAN NOT NULL,
                "CreatedAtUtc" TIMESTAMP NOT NULL
            );
            """;

        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = npgsqlConnection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task DropBulkTestTableAsync()
    {
        const string sql = """DROP TABLE IF EXISTS "BulkTestOrders";""";

        if (_connection.State == ConnectionState.Open && _connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = npgsqlConnection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<int> GetRowCountAsync()
    {
        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = npgsqlConnection.CreateCommand();
            command.CommandText = """SELECT COUNT(*) FROM "BulkTestOrders" """;
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
    public async Task BulkInsertAsync_1000Entities_InsertsAllSuccessfully()
    {
        // Arrange
        var orders = CreateOrders(1000);

        // Act
        var result = await _bulkOps.BulkInsertAsync(orders);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(1000));

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(1000);
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
public class BulkTestOrderMapping : IEntityMapping<BulkTestOrder, Guid>
{
    public string TableName => "\"BulkTestOrders\"";
    public string IdColumnName => "Id";

    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["CustomerName"] = "CustomerName",
        ["Amount"] = "Amount",
        ["IsActive"] = "IsActive",
        ["CreatedAtUtc"] = "CreatedAtUtc"
    };

    public Guid GetId(BulkTestOrder entity) => entity.Id;

    public IReadOnlySet<string> InsertExcludedProperties { get; } = new HashSet();
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new HashSet { "Id", "CreatedAtUtc" };
}

#endregion
