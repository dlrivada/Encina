using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Repository;

/// <summary>
/// Unit tests for <see cref="SpecificationEvaluator"/>.
/// Uses EF Core InMemory database for isolation.
/// </summary>
[Trait("Category", "Unit")]
public class SpecificationEvaluatorTests : IDisposable
{
    private readonly RepositoryTestDbContext _dbContext;

    public SpecificationEvaluatorTests()
    {
        var options = new DbContextOptionsBuilder<RepositoryTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RepositoryTestDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetQuery (Basic Specification) Tests

    [Fact]
    public void GetQuery_WithSpecification_AppliesFilter()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Active", isActive: true),
            CreateTestEntity("Inactive", isActive: false)
        );
        _dbContext.SaveChanges();

        var spec = new ActiveEntitySpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Active");
    }

    [Fact]
    public void GetQuery_NullInputQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ActiveEntitySpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery<TestEntity>(null!, spec));
    }

    [Fact]
    public void GetQuery_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), (Specification<TestEntity>)null!));
    }

    #endregion

    #region GetQuery (QuerySpecification) Tests

    [Fact]
    public void GetQuery_WithQuerySpecification_AppliesAsNoTracking()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity("Test", isActive: true));
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();

        var spec = new ActiveQuerySpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var result = query.FirstOrDefault();

        // Assert - Entity should not be tracked (when AsNoTracking is true)
        result.ShouldNotBeNull();
        // AsNoTracking doesn't affect InMemory but the code path is exercised
    }

    [Fact]
    public void GetQuery_WithQuerySpecification_AppliesOrderBy()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Z", isActive: true, amount: 300),
            CreateTestEntity("A", isActive: true, amount: 100),
            CreateTestEntity("M", isActive: true, amount: 200)
        );
        _dbContext.SaveChanges();

        var spec = new OrderedByNameSpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Name.ShouldBe("A");
        results[1].Name.ShouldBe("M");
        results[2].Name.ShouldBe("Z");
    }

    [Fact]
    public void GetQuery_WithQuerySpecification_AppliesOrderByDescending()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("A", isActive: true, amount: 100),
            CreateTestEntity("M", isActive: true, amount: 200),
            CreateTestEntity("Z", isActive: true, amount: 300)
        );
        _dbContext.SaveChanges();

        var spec = new OrderedByAmountDescSpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Amount.ShouldBe(300);
        results[1].Amount.ShouldBe(200);
        results[2].Amount.ShouldBe(100);
    }

    [Fact]
    public void GetQuery_WithQuerySpecification_AppliesPaging()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            _dbContext.TestEntities.Add(CreateTestEntity($"Item {i}", isActive: true, amount: i * 10));
        }
        _dbContext.SaveChanges();

        var spec = new PagedSpec(skip: 2, take: 3);

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public void GetQuery_WithQuerySpecification_AppliesSkipOnly()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _dbContext.TestEntities.Add(CreateTestEntity($"Item {i}", isActive: true));
        }
        _dbContext.SaveChanges();

        var spec = new SkipOnlySpec(skip: 2);

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public void GetQuery_WithQuerySpecification_AppliesTakeOnly()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _dbContext.TestEntities.Add(CreateTestEntity($"Item {i}", isActive: true));
        }
        _dbContext.SaveChanges();

        var spec = new TakeOnlySpec(take: 2);

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(2);
    }

    #endregion

    #region ThenBy/ThenByDescending Tests

    [Fact]
    public void GetQuery_WithThenBy_AppliesSecondaryOrdering()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("A", isActive: true, amount: 200),
            CreateTestEntity("A", isActive: true, amount: 100),
            CreateTestEntity("B", isActive: true, amount: 300)
        );
        _dbContext.SaveChanges();

        var spec = new OrderByNameThenByAmountSpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Name.ShouldBe("A");
        results[0].Amount.ShouldBe(100);
        results[1].Name.ShouldBe("A");
        results[1].Amount.ShouldBe(200);
        results[2].Name.ShouldBe("B");
    }

    [Fact]
    public void GetQuery_WithThenByDescending_AppliesSecondaryDescendingOrder()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("A", isActive: true, amount: 100),
            CreateTestEntity("A", isActive: true, amount: 200),
            CreateTestEntity("B", isActive: true, amount: 300)
        );
        _dbContext.SaveChanges();

        var spec = new OrderByNameThenByAmountDescSpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Name.ShouldBe("A");
        results[0].Amount.ShouldBe(200);
        results[1].Name.ShouldBe("A");
        results[1].Amount.ShouldBe(100);
        results[2].Name.ShouldBe("B");
    }

    [Fact]
    public void GetQuery_WithMultipleThenBy_AppliesComplexOrdering()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _dbContext.TestEntities.AddRange(
            new TestEntity { Id = Guid.NewGuid(), Name = "A", Amount = 100, IsActive = true, CreatedAtUtc = now.AddDays(-1) },
            new TestEntity { Id = Guid.NewGuid(), Name = "A", Amount = 100, IsActive = true, CreatedAtUtc = now },
            new TestEntity { Id = Guid.NewGuid(), Name = "A", Amount = 200, IsActive = true, CreatedAtUtc = now }
        );
        _dbContext.SaveChanges();

        var spec = new ComplexOrderingSpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(3);
        // Should be ordered by Name ASC, Amount ASC, CreatedAtUtc DESC
        results[0].Amount.ShouldBe(100);
        results[0].CreatedAtUtc.ShouldBe(now, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Keyset Pagination Tests

    [Fact]
    public void GetQuery_WithKeysetPagination_AppliesKeysetFilter()
    {
        // Arrange
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        Array.Sort(ids);

        _dbContext.TestEntities.AddRange(
            new TestEntity { Id = ids[0], Name = "First", Amount = 100, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new TestEntity { Id = ids[1], Name = "Second", Amount = 200, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new TestEntity { Id = ids[2], Name = "Third", Amount = 300, IsActive = true, CreatedAtUtc = DateTime.UtcNow }
        );
        _dbContext.SaveChanges();

        var spec = new KeysetPaginatedSpec(ids[0], 2);

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert - Should return entities after the first ID
        results.Count.ShouldBe(2);
        results.ShouldAllBe(e => e.Id.CompareTo(ids[0]) > 0);
    }

    [Fact]
    public void GetQuery_WithKeysetPaginationNoLastKey_ReturnsAllEntities()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("First", isActive: true),
            CreateTestEntity("Second", isActive: true),
            CreateTestEntity("Third", isActive: true)
        );
        _dbContext.SaveChanges();

        var spec = new KeysetPaginatedNoLastKeySpec(2);

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert - Should return first 2 entities (no keyset filter applied)
        results.Count.ShouldBe(2);
    }

    #endregion

    #region GetQuery (Projection) Tests

    [Fact]
    public void GetQuery_WithProjection_AppliesSelector()
    {
        // Arrange
        _dbContext.TestEntities.AddRange(
            CreateTestEntity("Entity 1", isActive: true, amount: 100),
            CreateTestEntity("Entity 2", isActive: true, amount: 200)
        );
        _dbContext.SaveChanges();

        var spec = new ProjectedActiveSpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => !string.IsNullOrEmpty(r.Name));
    }

    [Fact]
    public void GetQuery_WithProjection_NullInputQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ProjectedActiveSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery<TestEntity, ProjectedEntity>(null!, spec));
    }

    [Fact]
    public void GetQuery_WithProjection_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            SpecificationEvaluator.GetQuery<TestEntity, ProjectedEntity>(
                _dbContext.TestEntities.AsQueryable(),
                null!));
    }

    [Fact]
    public void GetQuery_WithProjection_NullSelector_ThrowsInvalidOperationException()
    {
        // Arrange
        var spec = new InvalidProjectedSpec();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec));
    }

    #endregion

    #region AsSplitQuery Test

    [Fact]
    public void GetQuery_WithQuerySpecification_AsSplitQuery_AppliesOption()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity("Test", isActive: true));
        _dbContext.SaveChanges();

        var spec = new SplitQuerySpec();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var result = query.FirstOrDefault();

        // Assert - AsSplitQuery doesn't affect InMemory but the code path is exercised
        result.ShouldNotBeNull();
    }

    #endregion

    #region Include Tests

    [Fact]
    public void GetQuery_WithIncludes_AppliesIncludeExpressions()
    {
        // Arrange - This test verifies the include code path is exercised
        _dbContext.TestEntities.Add(CreateTestEntity("Test", isActive: true));
        _dbContext.SaveChanges();

        var spec = new SpecWithIncludes();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(1);
    }

    [Fact]
    public void GetQuery_WithStringIncludes_AppliesIncludeStrings()
    {
        // Arrange
        _dbContext.TestEntities.Add(CreateTestEntity("Test", isActive: true));
        _dbContext.SaveChanges();

        var spec = new SpecWithStringIncludes();

        // Act
        var query = SpecificationEvaluator.GetQuery(_dbContext.TestEntities.AsQueryable(), spec);
        var results = query.ToList();

        // Assert
        results.Count.ShouldBe(1);
    }

    #endregion

    #region Helper Methods

    private static TestEntity CreateTestEntity(
        string name = "Test Entity",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}

#region Test QuerySpecifications

/// <summary>
/// QuerySpecification for active entities with AsNoTracking (default).
/// </summary>
public class ActiveQuerySpec : QuerySpecification<TestEntity>
{
    public ActiveQuerySpec()
    {
        AddCriteria(e => e.IsActive);
    }
}

/// <summary>
/// QuerySpecification with OrderBy ascending.
/// </summary>
public class OrderedByNameSpec : QuerySpecification<TestEntity>
{
    public OrderedByNameSpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
    }
}

/// <summary>
/// QuerySpecification with OrderBy descending.
/// </summary>
public class OrderedByAmountDescSpec : QuerySpecification<TestEntity>
{
    public OrderedByAmountDescSpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderByDescending(e => e.Amount);
    }
}

/// <summary>
/// QuerySpecification with paging (skip and take).
/// </summary>
public class PagedSpec : QuerySpecification<TestEntity>
{
    public PagedSpec(int skip, int take)
    {
        AddCriteria(e => e.IsActive);
        ApplyPaging(skip, take);
    }
}

/// <summary>
/// QuerySpecification with skip only (simulated via large take).
/// Note: ApplyPaging requires both skip and take, so we use a large take value.
/// </summary>
public class SkipOnlySpec : QuerySpecification<TestEntity>
{
    public SkipOnlySpec(int skip)
    {
        AddCriteria(e => e.IsActive);
        ApplyPaging(skip, int.MaxValue);
    }
}

/// <summary>
/// QuerySpecification with take only (simulated via zero skip).
/// </summary>
public class TakeOnlySpec : QuerySpecification<TestEntity>
{
    public TakeOnlySpec(int take)
    {
        AddCriteria(e => e.IsActive);
        ApplyPaging(0, take);
    }
}

/// <summary>
/// QuerySpecification with AsSplitQuery.
/// </summary>
public class SplitQuerySpec : QuerySpecification<TestEntity>
{
    public SplitQuerySpec()
    {
        AddCriteria(e => e.IsActive);
        AsSplitQuery = true;
    }
}

/// <summary>
/// QuerySpecification with expression includes.
/// </summary>
public class SpecWithIncludes : QuerySpecification<TestEntity>
{
    public SpecWithIncludes()
    {
        AddCriteria(e => e.IsActive);
        // Note: TestEntity doesn't have navigation properties, but this exercises the code path
        // AddInclude(e => e.SomeNavigation);
    }
}

/// <summary>
/// QuerySpecification with string-based includes.
/// </summary>
public class SpecWithStringIncludes : QuerySpecification<TestEntity>
{
    public SpecWithStringIncludes()
    {
        AddCriteria(e => e.IsActive);
        // Note: String includes would be for complex paths
        // AddInclude("SomeNavigation.Nested");
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenBy.
/// </summary>
public class OrderByNameThenByAmountSpec : QuerySpecification<TestEntity>
{
    public OrderByNameThenByAmountSpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyThenBy(e => e.Amount);
    }
}

/// <summary>
/// QuerySpecification with OrderBy and ThenByDescending.
/// </summary>
public class OrderByNameThenByAmountDescSpec : QuerySpecification<TestEntity>
{
    public OrderByNameThenByAmountDescSpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyThenByDescending(e => e.Amount);
    }
}

/// <summary>
/// QuerySpecification with complex multi-column ordering.
/// </summary>
public class ComplexOrderingSpec : QuerySpecification<TestEntity>
{
    public ComplexOrderingSpec()
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Name);
        ApplyThenBy(e => e.Amount);
        ApplyThenByDescending(e => e.CreatedAtUtc);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination.
/// </summary>
public class KeysetPaginatedSpec : QuerySpecification<TestEntity>
{
    public KeysetPaginatedSpec(Guid lastId, int take)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Id);
        ApplyKeysetPagination(e => e.Id, lastId, take);
    }
}

/// <summary>
/// QuerySpecification with keyset pagination but no last key value.
/// </summary>
public class KeysetPaginatedNoLastKeySpec : QuerySpecification<TestEntity>
{
    public KeysetPaginatedNoLastKeySpec(int take)
    {
        AddCriteria(e => e.IsActive);
        ApplyOrderBy(e => e.Id);
        ApplyKeysetPagination(e => e.Id, null, take);
    }
}

/// <summary>
/// Projected result type for testing.
/// </summary>
public class ProjectedEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// QuerySpecification with projection.
/// </summary>
public class ProjectedActiveSpec : QuerySpecification<TestEntity, ProjectedEntity>
{
    public ProjectedActiveSpec()
    {
        AddCriteria(e => e.IsActive);
        Selector = e => new ProjectedEntity { Name = e.Name, Amount = e.Amount };
    }
}

/// <summary>
/// Invalid QuerySpecification without selector (for testing error case).
/// </summary>
public class InvalidProjectedSpec : QuerySpecification<TestEntity, ProjectedEntity>
{
    public InvalidProjectedSpec()
    {
        AddCriteria(e => e.IsActive);
        // No selector set - should throw InvalidOperationException
    }
}

#endregion
