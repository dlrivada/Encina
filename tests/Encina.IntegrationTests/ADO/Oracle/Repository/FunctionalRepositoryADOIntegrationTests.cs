using System.Data;
using System.Linq.Expressions;
using Encina.ADO.Oracle.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Oracle.ManagedDataAccess.Client;
using Shouldly;

namespace Encina.IntegrationTests.ADO.Oracle.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryADO{TEntity, TId}"/> using real Oracle.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Oracle")]
public class FunctionalRepositoryADOIntegrationTests : IAsyncLifetime
{
    private readonly OracleFixture _fixture = new();
    private IDbConnection _connection = null!;
    private FunctionalRepositoryADO<TestItem, Guid> _repository = null!;
    private IEntityMapping<TestItem, Guid> _mapping = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as OracleConnection;
        if (schemaConnection != null)
        {
            await CreateTestItemsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = new EntityMappingBuilder<TestItem, Guid>()
            .ToTable("TEST_ITEMS")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "NAME")
            .MapProperty(p => p.Value, "VALUE")
            .MapProperty(p => p.IsEnabled, "IS_ENABLED")
            .MapProperty(p => p.CreatedAtUtc, "CREATED_AT_UTC")
            .Build();

        _repository = new FunctionalRepositoryADO<TestItem, Guid>(_connection, _mapping);
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.DisposeAsync();
    }

    private static async Task CreateTestItemsSchemaAsync(OracleConnection connection)
    {
        // Drop table if exists
        try
        {
            await using var dropCommand = new OracleCommand("DROP TABLE TEST_ITEMS", connection);
            await dropCommand.ExecuteNonQueryAsync();
        }
        catch (OracleException)
        {
            // Table doesn't exist, ignore
        }

        const string createSql = """
            CREATE TABLE TEST_ITEMS (
                ID RAW(16) PRIMARY KEY,
                NAME VARCHAR2(200) NOT NULL,
                VALUE NUMBER(18,2) NOT NULL,
                IS_ENABLED NUMBER(1) NOT NULL,
                CREATED_AT_UTC TIMESTAMP NOT NULL
            )
            """;

        await using var createCommand = new OracleCommand(createSql, connection);
        await createCommand.ExecuteNonQueryAsync();

        const string indexSql = "CREATE INDEX IX_TEST_ITEMS_IS_ENABLED ON TEST_ITEMS(IS_ENABLED)";
        await using var indexCommand = new OracleCommand(indexSql, connection);
        await indexCommand.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is OracleConnection oracleConnection)
        {
            await using var command = new OracleCommand("DELETE FROM TEST_ITEMS", oracleConnection);
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
        var entity = CreateTestItem();
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
            CreateTestItem("Item 1"),
            CreateTestItem("Item 2"),
            CreateTestItem("Item 3")
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
            CreateTestItem("Enabled", isEnabled: true),
            CreateTestItem("Disabled", isEnabled: false)
        });

        var spec = new EnabledItemSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Enabled");
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
        var entity = CreateTestItem("New Item");

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Name.ShouldBe("New Item"));

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
        var entity = CreateTestItem("Original");
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
        var entity = CreateTestItem();
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
            CreateTestItem("Item 1"),
            CreateTestItem("Item 2"),
            CreateTestItem("Item 3")
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
        await _repository.AddAsync(CreateTestItem());

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    #endregion

    #region Helper Methods

    private static TestItem CreateTestItem(
        string name = "Test Item",
        bool isEnabled = true,
        decimal value = 50.00m)
    {
        return new TestItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Value = value,
            IsEnabled = isEnabled,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}

#region Test Entity and Specifications

/// <summary>
/// Test item entity for ADO.NET Oracle repository integration tests.
/// </summary>
public class TestItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for enabled items.
/// </summary>
public class EnabledItemSpec : Specification<TestItem>
{
    public override Expression<Func<TestItem, bool>> ToExpression()
        => i => i.IsEnabled;
}

#endregion
