using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="PagedQuerySpecification{T}"/> and <see cref="PagedQuerySpecification{T, TResult}"/>.
/// </summary>
public class PagedQuerySpecificationTests
{
    #region Test Entities and Specifications

    private sealed record TestEntity(int Id, string Name, DateTime CreatedAtUtc, bool IsActive);

    private sealed record TestEntityDto(int Id, string Name);

    /// <summary>
    /// Simple paged specification for testing.
    /// </summary>
    private sealed class SimplePagedSpec : PagedQuerySpecification<TestEntity>
    {
        public SimplePagedSpec(PaginationOptions pagination) : base(pagination)
        {
        }
    }

    /// <summary>
    /// Paged specification with criteria.
    /// </summary>
    private sealed class ActiveEntitiesPagedSpec : PagedQuerySpecification<TestEntity>
    {
        public ActiveEntitiesPagedSpec(PaginationOptions pagination) : base(pagination)
        {
            AddCriteria(e => e.IsActive);
        }
    }

    /// <summary>
    /// Paged specification with criteria and ordering.
    /// </summary>
    private sealed class OrderedActiveEntitiesPagedSpec : PagedQuerySpecification<TestEntity>
    {
        public OrderedActiveEntitiesPagedSpec(PaginationOptions pagination, bool descending = false)
            : base(pagination)
        {
            AddCriteria(e => e.IsActive);
            if (descending)
                ApplyOrderByDescending(e => e.CreatedAtUtc);
            else
                ApplyOrderBy(e => e.CreatedAtUtc);
        }
    }

    /// <summary>
    /// Paged specification with projection.
    /// </summary>
    private sealed class EntitySummaryPagedSpec : PagedQuerySpecification<TestEntity, TestEntityDto>
    {
        public EntitySummaryPagedSpec(PaginationOptions pagination) : base(pagination)
        {
            Selector = e => new TestEntityDto(e.Id, e.Name);
        }
    }

    /// <summary>
    /// Paged specification with criteria and projection.
    /// </summary>
    private sealed class ActiveEntitySummaryPagedSpec : PagedQuerySpecification<TestEntity, TestEntityDto>
    {
        public ActiveEntitySummaryPagedSpec(PaginationOptions pagination) : base(pagination)
        {
            AddCriteria(e => e.IsActive);
            ApplyOrderByDescending(e => e.CreatedAtUtc);
            Selector = e => new TestEntityDto(e.Id, e.Name);
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidPagination_ShouldSetPaginationProperty()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 25);

        // Act
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Pagination.Should().Be(pagination);
        spec.Pagination.PageNumber.Should().Be(3);
        spec.Pagination.PageSize.Should().Be(25);
    }

    [Fact]
    public void Constructor_WithNullPagination_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new SimplePagedSpec(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldApplyPaging()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 3, PageSize: 10);

        // Act
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Skip.Should().Be(20); // (3-1) * 10
        spec.Take.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithPage1_ShouldSkipZero()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 20);

        // Act
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Skip.Should().Be(0);
        spec.Take.Should().Be(20);
    }

    #endregion

    #region IPagedSpecification Implementation Tests

    [Fact]
    public void PagedQuerySpecification_ShouldImplementIPagedSpecification()
    {
        // Arrange
        var pagination = PaginationOptions.Default;
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Should().BeAssignableTo<IPagedSpecification<TestEntity>>();
    }

    [Fact]
    public void PagedQuerySpecification_ShouldImplementISpecification()
    {
        // Arrange
        var pagination = PaginationOptions.Default;
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Should().BeAssignableTo<ISpecification<TestEntity>>();
    }

    [Fact]
    public void PagedQuerySpecificationWithProjection_ShouldImplementIPagedSpecificationWithResult()
    {
        // Arrange
        var pagination = PaginationOptions.Default;
        var spec = new EntitySummaryPagedSpec(pagination);

        // Assert
        spec.Should().BeAssignableTo<IPagedSpecification<TestEntity, TestEntityDto>>();
    }

    #endregion

    #region Criteria Tests

    [Fact]
    public void PagedSpec_WithCriteria_ShouldIncludeCriteria()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var spec = new ActiveEntitiesPagedSpec(pagination);

        // Assert
        spec.Criteria.Should().NotBeEmpty();
        spec.Criteria.Should().HaveCount(1);
    }

    [Fact]
    public void PagedSpec_WithCriteria_ShouldFilterCorrectly()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var spec = new ActiveEntitiesPagedSpec(pagination);
        var testEntities = new[]
        {
            new TestEntity(1, "Active", DateTime.UtcNow, true),
            new TestEntity(2, "Inactive", DateTime.UtcNow, false),
            new TestEntity(3, "Also Active", DateTime.UtcNow, true),
        };

        // Act
        var filtered = testEntities.AsQueryable().Where(spec.Criteria[0]).ToList();

        // Assert
        filtered.Should().HaveCount(2);
        filtered.Should().OnlyContain(e => e.IsActive);
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public void PagedSpec_WithOrdering_ShouldIncludeOrderBy()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var spec = new OrderedActiveEntitiesPagedSpec(pagination, descending: false);

        // Assert
        spec.OrderBy.Should().NotBeNull();
        spec.OrderByDescending.Should().BeNull();
    }

    [Fact]
    public void PagedSpec_WithDescendingOrdering_ShouldIncludeOrderByDescending()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var spec = new OrderedActiveEntitiesPagedSpec(pagination, descending: true);

        // Assert
        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderBy.Should().BeNull();
    }

    #endregion

    #region Projection Tests

    [Fact]
    public void PagedSpecWithProjection_ShouldHaveSelector()
    {
        // Arrange
        var pagination = PaginationOptions.Default;
        var spec = new EntitySummaryPagedSpec(pagination);

        // Assert
        spec.Selector.Should().NotBeNull();
    }

    [Fact]
    public void PagedSpecWithProjection_ShouldProjectCorrectly()
    {
        // Arrange
        var pagination = PaginationOptions.Default;
        var spec = new EntitySummaryPagedSpec(pagination);
        var entity = new TestEntity(1, "Test Entity", DateTime.UtcNow, true);

        // Act
        var projected = spec.Selector!.Compile()(entity);

        // Assert
        projected.Id.Should().Be(1);
        projected.Name.Should().Be("Test Entity");
    }

    [Fact]
    public void PagedSpecWithProjection_ShouldCombineWithCriteria()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 10);
        var spec = new ActiveEntitySummaryPagedSpec(pagination);

        // Assert
        spec.Criteria.Should().HaveCount(1);
        spec.Selector.Should().NotBeNull();
        spec.Pagination.Should().NotBeNull();
    }

    #endregion

    #region Pagination Integration Tests

    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(2, 10, 10, 10)]
    [InlineData(3, 10, 20, 10)]
    [InlineData(1, 25, 0, 25)]
    [InlineData(5, 25, 100, 25)]
    public void PagedSpec_ShouldCalculatePagingCorrectly(
        int pageNumber, int pageSize, int expectedSkip, int expectedTake)
    {
        // Arrange
        var pagination = new PaginationOptions(pageNumber, pageSize);

        // Act
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Skip.Should().Be(expectedSkip);
        spec.Take.Should().Be(expectedTake);
        spec.Pagination.PageNumber.Should().Be(pageNumber);
        spec.Pagination.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public void PagedSpec_ShouldApplyToQueryable()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 2, PageSize: 5);
        var spec = new SimplePagedSpec(pagination);
        var testEntities = Enumerable.Range(1, 20)
            .Select(i => new TestEntity(i, $"Entity-{i}", DateTime.UtcNow.AddDays(-i), true))
            .ToList();

        // Act - Simulate what a repository would do
        var query = testEntities.AsQueryable()
            .Skip(spec.Skip ?? 0)
            .Take(spec.Take ?? int.MaxValue);

        var result = query.ToList();

        // Assert
        result.Should().HaveCount(5);
        result[0].Id.Should().Be(6); // Page 2 starts at item 6
        result[4].Id.Should().Be(10);
    }

    #endregion

    #region Combined Features Tests

    [Fact]
    public void PagedSpec_CombinedFeatures_ShouldWorkTogether()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 3);
        var spec = new OrderedActiveEntitiesPagedSpec(pagination, descending: true);

        var testEntities = new[]
        {
            new TestEntity(1, "A", DateTime.UtcNow.AddDays(-3), true),
            new TestEntity(2, "B", DateTime.UtcNow.AddDays(-1), false),  // Inactive
            new TestEntity(3, "C", DateTime.UtcNow.AddDays(-2), true),
            new TestEntity(4, "D", DateTime.UtcNow, true),
            new TestEntity(5, "E", DateTime.UtcNow.AddDays(-4), true),
        };

        // Act - Simulate repository behavior
        var query = testEntities.AsQueryable();

        // Apply criteria
        foreach (var criteria in spec.Criteria)
        {
            query = query.Where(criteria);
        }

        // Apply ordering
        if (spec.OrderByDescending != null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        // Apply paging
        query = query.Skip(spec.Skip ?? 0).Take(spec.Take ?? int.MaxValue);

        var result = query.ToList();

        // Assert
        result.Should().HaveCount(3); // Page size 3, 4 active items
        result.Should().OnlyContain(e => e.IsActive);
        result[0].Id.Should().Be(4); // Most recent active
    }

    #endregion

    #region Default Pagination Tests

    [Fact]
    public void PagedSpec_WithDefaultPagination_ShouldUseDefaults()
    {
        // Arrange
        var spec = new SimplePagedSpec(PaginationOptions.Default);

        // Assert
        spec.Pagination.PageNumber.Should().Be(1);
        spec.Pagination.PageSize.Should().Be(20);
        spec.Skip.Should().Be(0);
        spec.Take.Should().Be(20);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void PagedSpec_WithLargePageNumber_ShouldCalculateCorrectly()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1000, PageSize: 100);
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Skip.Should().Be(99900); // (1000-1) * 100
        spec.Take.Should().Be(100);
    }

    [Fact]
    public void PagedSpec_WithSmallPageSize_ShouldWork()
    {
        // Arrange
        var pagination = new PaginationOptions(PageNumber: 1, PageSize: 1);
        var spec = new SimplePagedSpec(pagination);

        // Assert
        spec.Skip.Should().Be(0);
        spec.Take.Should().Be(1);
    }

    #endregion
}
