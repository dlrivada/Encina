using System.Data;
using System.Linq.Expressions;
using Encina.ADO.SqlServer.Repository;
using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.SqlServer.Repository;

/// <summary>
/// Integration tests for <see cref="FunctionalRepositoryADO{TEntity, TId}"/> using real SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public class FunctionalRepositoryADOIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbConnection _connection = null!;
    private FunctionalRepositoryADO<TestItem, Guid> _repository = null!;
    private IEntityMapping<TestItem, Guid> _mapping = null!;

    public FunctionalRepositoryADOIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Create the test schema
        using var schemaConnection = _fixture.CreateConnection() as SqlConnection;
        if (schemaConnection != null)
        {
            await CreateTestItemsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = new EntityMappingBuilder<TestItem, Guid>()
            .ToTable("TestItems")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Value, "Value")
            .MapProperty(p => p.IsEnabled, "IsEnabled")
            .MapProperty(p => p.CreatedAtUtc, "CreatedAtUtc")
            .Build();

        _repository = new FunctionalRepositoryADO<TestItem, Guid>(_connection, _mapping);
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    private static async Task CreateTestItemsSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS TestItems;
            CREATE TABLE TestItems (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(200) NOT NULL,
                Value DECIMAL(18,2) NOT NULL,
                IsEnabled BIT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL
            );
            CREATE INDEX IX_TestItems_IsEnabled ON TestItems(IsEnabled);
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand("DELETE FROM TestItems", sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
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

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredEntities()
    {
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

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsRightAndPersists()
    {
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

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsRightAndUpdates()
    {
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

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsRightAndRemoves()
    {
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
/// Test item entity for ADO.NET repository integration tests.
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
