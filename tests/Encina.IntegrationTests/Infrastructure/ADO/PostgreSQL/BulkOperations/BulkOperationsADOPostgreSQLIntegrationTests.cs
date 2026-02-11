using System.Data;
using System.Globalization;
using Encina.ADO.PostgreSQL.BulkOperations;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;
using Xunit;

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
[Collection("ADO-PostgreSQL")]
public sealed class BulkOperationsADOPostgreSQLIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private readonly string _tableName;
    private NpgsqlConnection _connection = null!;
    private BulkOperationsPostgreSQL<BulkTestOrder, Guid> _bulkOps = null!;

    public BulkOperationsADOPostgreSQLIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _tableName = $"BulkTestOrders_{Guid.NewGuid():N}";
    }

    public async ValueTask InitializeAsync()
    {
        _connection = _fixture.CreateConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected NpgsqlConnection");

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        // Create test table
        await CreateBulkTestTableAsync();

        // Ensure clean state for each test
        await TruncateTableAsync();

        // Create entity mapping and bulk operations
        var mapping = new BulkTestOrderMapping(_tableName);
        _bulkOps = new BulkOperationsPostgreSQL<BulkTestOrder, Guid>(_connection, mapping);
    }

    public async ValueTask DisposeAsync()
    {
        await DropBulkTestTableAsync();
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private string GetQuotedTableName()
    {
        return $"\"{_tableName}\"";
    }

    private async Task CreateBulkTestTableAsync()
    {
        var sql = $"""
            CREATE TABLE IF NOT EXISTS {GetQuotedTableName()} (
                "Id" uuid PRIMARY KEY,
                "CustomerName" text NOT NULL,
                "Amount" numeric(18,2) NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamptz NOT NULL
            );
            """;

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task TruncateTableAsync()
    {
        var sql = $"""TRUNCATE TABLE {GetQuotedTableName()};""";

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropBulkTestTableAsync()
    {
        var sql = $"""DROP TABLE IF EXISTS {GetQuotedTableName()};""";

        if (_connection.State == ConnectionState.Open)
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<int> GetRowCountAsync()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = $"""SELECT COUNT(*) FROM {GetQuotedTableName()} """;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
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
        var config = new BulkConfig { BatchSize = 50 };

        // Act
        var result = await _bulkOps.BulkInsertAsync(orders, config);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(250));
    }

    [Fact]
    public async Task BulkInsertAsync_ConcurrentTasks_InsertsAllSuccessfully()
    {
        // Arrange
        var batchA = CreateOrders(50);
        var batchB = CreateOrders(50);

        await using var connectionA = _fixture.CreateConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected NpgsqlConnection");
        await using var connectionB = _fixture.CreateConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected NpgsqlConnection");

        if (connectionA.State != ConnectionState.Open)
        {
            await connectionA.OpenAsync();
        }

        if (connectionB.State != ConnectionState.Open)
        {
            await connectionB.OpenAsync();
        }

        var mapping = new BulkTestOrderMapping(_tableName);
        var bulkOpsA = new BulkOperationsPostgreSQL<BulkTestOrder, Guid>(connectionA, mapping);
        var bulkOpsB = new BulkOperationsPostgreSQL<BulkTestOrder, Guid>(connectionB, mapping);

        // Act
        var results = await Task.WhenAll(
            bulkOpsA.BulkInsertAsync(batchA),
            bulkOpsB.BulkInsertAsync(batchB)
        );

        // Assert
        results.Length.ShouldBe(2);
        results.All(r => r.IsRight).ShouldBeTrue();

        var total = 0;
        foreach (var result in results)
        {
            result.IfRight(count => total += count);
        }

        total.ShouldBe(100);

        var rowCount = await GetRowCountAsync();
        rowCount.ShouldBe(100);
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
        idsToRead.Add(Guid.NewGuid());
        idsToRead.Add(Guid.NewGuid());

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

        foreach (var order in existingOrders)
        {
            order.CustomerName = $"Merged_{order.CustomerName}";
        }

        var newOrders = CreateOrders(5);
        var allOrders = existingOrders.Concat(newOrders).ToList();

        // Act
        var result = await _bulkOps.BulkMergeAsync(allOrders);

        // Assert
        var mergeCount = result.Match(
            Right: count => count,
            Left: error => throw new InvalidOperationException($"BulkMerge failed: {error.Message}")
        );
        mergeCount.ShouldBe(10);

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

        var duplicateOrder = new BulkTestOrder
        {
            Id = order.Id,
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
public sealed class BulkTestOrder
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
public sealed class BulkTestOrderMapping : IEntityMapping<BulkTestOrder, Guid>
{
    private readonly string _tableName;

    public BulkTestOrderMapping(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _tableName = tableName;
    }

    public string TableName => _tableName;
    public string IdColumnName => "Id";
#pragma warning disable CA1822 // Mark members as static - Interface implementation cannot be static
    public string SchemaName => "public";
#pragma warning restore CA1822

    public IReadOnlyDictionary<string, string> ColumnMappings { get; } = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["CustomerName"] = "CustomerName",
        ["Amount"] = "Amount",
        ["IsActive"] = "IsActive",
        ["CreatedAtUtc"] = "CreatedAtUtc"
    };

    public Guid GetId(BulkTestOrder entity) => entity.Id;

#pragma warning disable CA1822 // Mark members as static - Interface implementation cannot be static
    public void SetId(BulkTestOrder entity, Guid id) => entity.Id = id;
#pragma warning restore CA1822

    public IReadOnlySet<string> InsertExcludedProperties { get; } = new System.Collections.Generic.HashSet<string>();
    public IReadOnlySet<string> UpdateExcludedProperties { get; } = new System.Collections.Generic.HashSet<string> { "Id", "CreatedAtUtc" };

#pragma warning disable CA1822 // Mark members as static - Interface implementation cannot be static
    public object? GetPropertyValue(BulkTestOrder entity, string propertyName)
    {
        return propertyName switch
        {
            "Id" => entity.Id,
            "CustomerName" => entity.CustomerName,
            "Amount" => entity.Amount,
            "IsActive" => entity.IsActive,
            "CreatedAtUtc" => entity.CreatedAtUtc,
            _ => throw new ArgumentException($"Unknown property: {propertyName}", nameof(propertyName))
        };
    }
#pragma warning restore CA1822

#pragma warning disable CA1822 // Mark members as static - Interface implementation cannot be static
    public BulkTestOrder CreateEntity(IReadOnlyDictionary<string, object?> values)
    {
        return new BulkTestOrder
        {
            Id = (Guid)values["Id"]!,
            CustomerName = (string)values["CustomerName"]!,
            Amount = (decimal)values["Amount"]!,
            IsActive = (bool)values["IsActive"]!,
            CreatedAtUtc = (DateTime)values["CreatedAtUtc"]!
        };
    }
#pragma warning restore CA1822
}

#endregion
