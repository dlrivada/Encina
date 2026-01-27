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
/// Extended integration tests for <see cref="FunctionalRepositoryADO{TEntity, TId}"/> using real SQL Server.
/// Tests complex specifications, pagination, bulk operations, and type mapping edge cases.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class ExtendedFunctionalRepositoryADOIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture = new();
    private IDbConnection _connection = null!;
    private FunctionalRepositoryADO<ExtendedTestItem, Guid> _repository = null!;
    private IEntityMapping<ExtendedTestItem, Guid> _mapping = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        using var schemaConnection = _fixture.CreateConnection() as SqlConnection;
        if (schemaConnection != null)
        {
            await CreateExtendedTestItemsSchemaAsync(schemaConnection);
        }

        _connection = _fixture.CreateConnection();
        _mapping = new EntityMappingBuilder<ExtendedTestItem, Guid>()
            .ToTable("ExtendedTestItems")
            .HasId(p => p.Id)
            .MapProperty(p => p.Name, "Name")
            .MapProperty(p => p.Description, "Description")
            .MapProperty(p => p.Amount, "Amount")
            .MapProperty(p => p.IsActive, "IsActive")
            .MapProperty(p => p.Category, "Category")
            .MapProperty(p => p.CreatedAtUtc, "CreatedAtUtc")
            .MapProperty(p => p.UpdatedAtUtc, "UpdatedAtUtc")
            .Build();

        _repository = new FunctionalRepositoryADO<ExtendedTestItem, Guid>(_connection, _mapping);
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.DisposeAsync();
    }

    private static async Task CreateExtendedTestItemsSchemaAsync(SqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS ExtendedTestItems;
            CREATE TABLE ExtendedTestItems (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(256) NOT NULL,
                Description NVARCHAR(1024) NULL,
                Amount DECIMAL(18,4) NOT NULL,
                IsActive BIT NOT NULL DEFAULT 1,
                Category NVARCHAR(100) NULL,
                CreatedAtUtc DATETIME2(7) NOT NULL,
                UpdatedAtUtc DATETIME2(7) NULL
            );
            CREATE INDEX IX_ExtendedTestItems_IsActive ON ExtendedTestItems(IsActive);
            CREATE INDEX IX_ExtendedTestItems_Category ON ExtendedTestItems(Category);
            CREATE INDEX IX_ExtendedTestItems_CreatedAtUtc ON ExtendedTestItems(CreatedAtUtc);
            CREATE INDEX IX_ExtendedTestItems_Amount ON ExtendedTestItems(Amount);
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand("DELETE FROM ExtendedTestItems", sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }

    #region Complex Specification Tests (AND/OR/NOT)

    [Fact]
    public async Task ListAsync_WithAndSpecification_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Item1", isActive: true, amount: 100m, category: "A"),
            CreateItem("Item2", isActive: true, amount: 200m, category: "B"),
            CreateItem("Item3", isActive: false, amount: 100m, category: "A"),
            CreateItem("Item4", isActive: true, amount: 50m, category: "A")
        });

        // Active AND Category = "A"
        var activeSpec = new IsActiveSpec();
        var categorySpec = new CategorySpec("A");
        var combinedSpec = activeSpec.And(categorySpec);

        // Act
        var result = await _repository.ListAsync(combinedSpec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(i => i.IsActive && i.Category == "A");
        });
    }

    [Fact]
    public async Task ListAsync_WithOrSpecification_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Item1", isActive: true, amount: 100m, category: "A"),
            CreateItem("Item2", isActive: false, amount: 200m, category: "B"),
            CreateItem("Item3", isActive: false, amount: 300m, category: "C")
        });

        // IsActive OR Amount > 150
        var activeSpec = new IsActiveSpec();
        var amountSpec = new MinAmountSpec(150m);
        var combinedSpec = activeSpec.Or(amountSpec);

        // Act
        var result = await _repository.ListAsync(combinedSpec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(3); // Item1 (active), Item2 (amount > 150), Item3 (amount > 150)
        });
    }

    [Fact]
    public async Task ListAsync_WithNotSpecification_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Item1", isActive: true),
            CreateItem("Item2", isActive: false),
            CreateItem("Item3", isActive: true)
        });

        // NOT IsActive
        var activeSpec = new IsActiveSpec();
        var notActiveSpec = activeSpec.Not();

        // Act
        var result = await _repository.ListAsync(notActiveSpec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Item2");
        });
    }

    [Fact]
    public async Task ListAsync_WithComplexNestedSpecification_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Item1", isActive: true, amount: 100m, category: "A"),
            CreateItem("Item2", isActive: false, amount: 200m, category: "B"),
            CreateItem("Item3", isActive: true, amount: 300m, category: "A"),
            CreateItem("Item4", isActive: false, amount: 50m, category: "C"),
            CreateItem("Item5", isActive: true, amount: 150m, category: "B")
        });

        // (IsActive AND Category = "A") OR (Amount >= 200)
        var activeSpec = new IsActiveSpec();
        var categoryASpec = new CategorySpec("A");
        var minAmountSpec = new MinAmountSpec(200m);
        var combinedSpec = activeSpec.And(categoryASpec).Or(minAmountSpec);

        // Act
        var result = await _repository.ListAsync(combinedSpec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            // Item1: Active + Cat A = YES
            // Item2: Amount >= 200 = YES
            // Item3: Active + Cat A = YES, Amount >= 200 = YES (but counted once)
            // Item4: NO
            // Item5: NO
            list.Count.ShouldBe(3);
        });
    }

    #endregion

    #region String Operations Tests

    [Fact]
    public async Task ListAsync_WithContainsSpecification_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("TestItem1"),
            CreateItem("AnotherTest"),
            CreateItem("SomethingElse"),
            CreateItem("TestAgain")
        });

        var spec = new NameContainsSpec("Test");

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(3);
            list.ShouldAllBe(i => i.Name.Contains("Test"));
        });
    }

    [Fact]
    public async Task ListAsync_WithStartsWithSpecification_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Alpha_Item"),
            CreateItem("Beta_Item"),
            CreateItem("Alpha_Other"),
            CreateItem("Gamma_Item")
        });

        var spec = new NameStartsWithSpec("Alpha");

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(i => i.Name.StartsWith("Alpha"));
        });
    }

    [Fact]
    public async Task ListAsync_WithEndsWithSpecification_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Item_Suffix"),
            CreateItem("Another_Suffix"),
            CreateItem("NoMatch"),
            CreateItem("Also_Suffix")
        });

        var spec = new NameEndsWithSpec("Suffix");

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(3);
            list.ShouldAllBe(i => i.Name.EndsWith("Suffix"));
        });
    }

    #endregion

    #region Null Handling Tests

    [Fact]
    public async Task ListAsync_WithNullDescriptionSpec_ReturnsEntitiesWithNullDescription()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("WithDescription", description: "Has description"),
            CreateItem("WithoutDescription", description: null),
            CreateItem("AlsoWithout", description: null)
        });

        var spec = new NullDescriptionSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(i => i.Description == null);
        });
    }

    [Fact]
    public async Task ListAsync_WithNotNullDescriptionSpec_ReturnsEntitiesWithDescription()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("WithDescription", description: "Has description"),
            CreateItem("WithoutDescription", description: null),
            CreateItem("AlsoWith", description: "Another description")
        });

        var spec = new NotNullDescriptionSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(i => i.Description != null);
        });
    }

    [Fact]
    public async Task ListAsync_WithNullableDateTimeComparison_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        var now = DateTime.UtcNow;
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Updated", updatedAtUtc: now.AddDays(-1)),
            CreateItem("NotUpdated", updatedAtUtc: null),
            CreateItem("RecentlyUpdated", updatedAtUtc: now)
        });

        var spec = new HasBeenUpdatedSpec();

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(i => i.UpdatedAtUtc != null);
        });
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task AddRangeAsync_MultipleBatches_PersistsAllEntities()
    {
        // Arrange
        await ClearDataAsync();
        var entities = Enumerable.Range(1, 100)
            .Select(i => CreateItem($"BatchItem_{i}"))
            .ToArray();

        // Act
        var result = await _repository.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        var countResult = await _repository.CountAsync();
        countResult.IsRight.ShouldBeTrue();
        countResult.IfRight(count => count.ShouldBe(100));
    }

    [Fact]
    public async Task DeleteRangeAsync_MultipleEntities_RemovesAllSpecified()
    {
        // Arrange
        await ClearDataAsync();
        var entitiesToDelete = new[]
        {
            CreateItem("ToDelete1"),
            CreateItem("ToDelete2"),
            CreateItem("ToDelete3")
        };
        var entityToKeep = CreateItem("ToKeep");

        await _repository.AddRangeAsync(entitiesToDelete);
        await _repository.AddAsync(entityToKeep);

        // Act - Create a specification that matches the entities to delete by their IDs
        var idsToDelete = entitiesToDelete.Select(e => e.Id).ToArray();
        var deleteSpec = new IdsInListSpec(idsToDelete);
        var result = await _repository.DeleteRangeAsync(deleteSpec);

        // Assert
        result.IsRight.ShouldBeTrue();

        var remaining = await _repository.ListAsync();
        remaining.IsRight.ShouldBeTrue();
        remaining.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("ToKeep");
        });
    }

    [Fact]
    public async Task UpdateRangeAsync_MultipleEntities_UpdatesAll()
    {
        // Arrange
        await ClearDataAsync();
        var entities = new[]
        {
            CreateItem("Item1", amount: 100m),
            CreateItem("Item2", amount: 200m),
            CreateItem("Item3", amount: 300m)
        };
        await _repository.AddRangeAsync(entities);

        // Modify all entities
        foreach (var entity in entities)
        {
            entity.Amount *= 2;
        }

        // Act
        var result = await _repository.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        var updated = await _repository.ListAsync();
        updated.IsRight.ShouldBeTrue();
        updated.IfRight(list =>
        {
            list.ShouldContain(i => i.Amount == 200m);
            list.ShouldContain(i => i.Amount == 400m);
            list.ShouldContain(i => i.Amount == 600m);
        });
    }

    #endregion

    #region Type Mapping Edge Cases

    [Fact]
    public async Task TypeMapping_DecimalPrecision_PreservesFourDecimalPlaces()
    {
        // Arrange
        await ClearDataAsync();
        var entity = CreateItem("PrecisionTest", amount: 123.4567m);

        // Act
        await _repository.AddAsync(entity);
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Amount.ShouldBe(123.4567m));
    }

    [Fact]
    public async Task TypeMapping_DateTimePrecision_PreservesMilliseconds()
    {
        // Arrange
        await ClearDataAsync();
        var preciseTime = new DateTime(2026, 1, 15, 10, 30, 45, 123, DateTimeKind.Utc)
            .AddTicks(4567); // Add sub-millisecond precision
        var entity = CreateItem("DateTimeTest");
        entity.CreatedAtUtc = preciseTime;

        // Act
        await _repository.AddAsync(entity);
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            // SQL Server DATETIME2(7) preserves 100-nanosecond precision
            (e.CreatedAtUtc - preciseTime).TotalMilliseconds.ShouldBeLessThan(0.001);
        });
    }

    [Fact]
    public async Task TypeMapping_GuidStorage_PreservesValue()
    {
        // Arrange
        await ClearDataAsync();
        var specificGuid = new Guid("12345678-1234-1234-1234-123456789012");
        var entity = CreateItem("GuidTest");
        entity.Id = specificGuid;

        // Act
        await _repository.AddAsync(entity);
        var result = await _repository.GetByIdAsync(specificGuid);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e => e.Id.ShouldBe(specificGuid));
    }

    [Fact]
    public async Task TypeMapping_BooleanStorage_PreservesValue()
    {
        // Arrange
        await ClearDataAsync();
        var activeEntity = CreateItem("Active", isActive: true);
        var inactiveEntity = CreateItem("Inactive", isActive: false);

        // Act
        await _repository.AddAsync(activeEntity);
        await _repository.AddAsync(inactiveEntity);

        var activeResult = await _repository.GetByIdAsync(activeEntity.Id);
        var inactiveResult = await _repository.GetByIdAsync(inactiveEntity.Id);

        // Assert
        activeResult.IsRight.ShouldBeTrue();
        activeResult.IfRight(e => e.IsActive.ShouldBeTrue());

        inactiveResult.IsRight.ShouldBeTrue();
        inactiveResult.IfRight(e => e.IsActive.ShouldBeFalse());
    }

    #endregion

    #region Date Range Tests

    [Fact]
    public async Task ListAsync_WithDateRangeSpec_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        var baseDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Old", createdAtUtc: baseDate.AddDays(-10)),
            CreateItem("InRange1", createdAtUtc: baseDate.AddDays(-3)),
            CreateItem("InRange2", createdAtUtc: baseDate.AddDays(-1)),
            CreateItem("Future", createdAtUtc: baseDate.AddDays(5))
        });

        var spec = new DateRangeSpec(baseDate.AddDays(-5), baseDate);

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(i => i.CreatedAtUtc >= baseDate.AddDays(-5) && i.CreatedAtUtc < baseDate);
        });
    }

    #endregion

    #region Amount Range Tests

    [Fact]
    public async Task ListAsync_WithAmountRangeSpec_ReturnsCorrectResults()
    {
        // Arrange
        await ClearDataAsync();
        await _repository.AddRangeAsync(new[]
        {
            CreateItem("Low", amount: 50m),
            CreateItem("InRange1", amount: 100m),
            CreateItem("InRange2", amount: 150m),
            CreateItem("High", amount: 300m)
        });

        var spec = new AmountRangeSpec(100m, 200m);

        // Act
        var result = await _repository.ListAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(2);
            list.ShouldAllBe(i => i.Amount >= 100m && i.Amount <= 200m);
        });
    }

    #endregion

    #region Helper Methods

    private static ExtendedTestItem CreateItem(
        string name = "Test Item",
        string? description = null,
        decimal amount = 100m,
        bool isActive = true,
        string? category = null,
        DateTime? createdAtUtc = null,
        DateTime? updatedAtUtc = null)
    {
        return new ExtendedTestItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Amount = amount,
            IsActive = isActive,
            Category = category,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    #endregion
}

#region Test Entity and Specifications

/// <summary>
/// Extended test item entity for comprehensive ADO.NET repository integration tests.
/// </summary>
public class ExtendedTestItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class IsActiveSpec : Specification<ExtendedTestItem>
{
    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.IsActive;
}

public class CategorySpec : Specification<ExtendedTestItem>
{
    private readonly string _category;

    public CategorySpec(string category)
    {
        _category = category;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Category == _category;
}

public class MinAmountSpec : Specification<ExtendedTestItem>
{
    private readonly decimal _minAmount;

    public MinAmountSpec(decimal minAmount)
    {
        _minAmount = minAmount;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Amount >= _minAmount;
}

public class AmountRangeSpec : Specification<ExtendedTestItem>
{
    private readonly decimal _min;
    private readonly decimal _max;

    public AmountRangeSpec(decimal min, decimal max)
    {
        _min = min;
        _max = max;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Amount >= _min && i.Amount <= _max;
}

public class NameContainsSpec : Specification<ExtendedTestItem>
{
    private readonly string _pattern;

    public NameContainsSpec(string pattern)
    {
        _pattern = pattern;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Name.Contains(_pattern);
}

public class NameStartsWithSpec : Specification<ExtendedTestItem>
{
    private readonly string _prefix;

    public NameStartsWithSpec(string prefix)
    {
        _prefix = prefix;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Name.StartsWith(_prefix);
}

public class NameEndsWithSpec : Specification<ExtendedTestItem>
{
    private readonly string _suffix;

    public NameEndsWithSpec(string suffix)
    {
        _suffix = suffix;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Name.EndsWith(_suffix);
}

public class NullDescriptionSpec : Specification<ExtendedTestItem>
{
    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Description == null;
}

public class NotNullDescriptionSpec : Specification<ExtendedTestItem>
{
    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.Description != null;
}

public class HasBeenUpdatedSpec : Specification<ExtendedTestItem>
{
    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.UpdatedAtUtc != null;
}

public class DateRangeSpec : Specification<ExtendedTestItem>
{
    private readonly DateTime _start;
    private readonly DateTime _end;

    public DateRangeSpec(DateTime start, DateTime end)
    {
        _start = start;
        _end = end;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => i.CreatedAtUtc >= _start && i.CreatedAtUtc < _end;
}

public class IdsInListSpec : Specification<ExtendedTestItem>
{
    private readonly Guid[] _ids;

    public IdsInListSpec(Guid[] ids)
    {
        _ids = ids;
    }

    public override Expression<Func<ExtendedTestItem, bool>> ToExpression()
        => i => _ids.Contains(i.Id);
}

#endregion
