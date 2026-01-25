using System.Data;
using System.Linq.Expressions;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;

namespace Encina.IntegrationTests.ADO.PostgreSQL.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryADO{TEntity, TId}"/> using real PostgreSQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public class FunctionalRepositoryADOIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture = new();
    private IDbConnection _connection = null!;
    private FunctionalRepositoryADO<TestItem, Guid> _repository = null!;
    private IEntityMapping<TestItem, Guid> _mapping = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as NpgsqlConnection;
        if (schemaConnection != null)
        {
            await CreateTestItemsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = new EntityMappingBuilder<TestItem, Guid>()
            .ToTable("test_items")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "name")
            .MapProperty(p => p.Value, "value")
            .MapProperty(p => p.IsEnabled, "is_enabled")
            .MapProperty(p => p.CreatedAtUtc, "created_at_utc")
            .Build();

        _repository = new FunctionalRepositoryADO<TestItem, Guid>(_connection, _mapping);
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.DisposeAsync();
    }

    private static async Task CreateTestItemsSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS test_items;
            CREATE TABLE test_items (
                id UUID PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                value DECIMAL(18,2) NOT NULL,
                is_enabled BOOLEAN NOT NULL,
                created_at_utc TIMESTAMP NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_test_items_is_enabled ON test_items(is_enabled);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is NpgsqlConnection npgsqlConnection)
        {
            await using var command = new NpgsqlCommand("DELETE FROM test_items", npgsqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    #region GetByIdAsync Tests

    [SkippableFact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
/// Test item entity for ADO.NET PostgreSQL repository integration tests.
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
