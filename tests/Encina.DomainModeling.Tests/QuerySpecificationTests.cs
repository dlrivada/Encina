using System.Linq.Expressions;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Unit tests for the QuerySpecification class.
/// </summary>
public class QuerySpecificationTests
{
    private sealed class Product
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public bool IsActive { get; init; }
        public Category? Category { get; init; }
    }

    private sealed class Category
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private sealed class ActiveProductsQuerySpec : QuerySpecification<Product>
    {
        public ActiveProductsQuerySpec()
        {
            Criteria = p => p.IsActive;
        }
    }

    private sealed class ProductsWithCategorySpec : QuerySpecification<Product>
    {
        public ProductsWithCategorySpec()
        {
            Criteria = p => p.IsActive;
            AddInclude(p => p.Category!);
        }
    }

    private sealed class PaginatedProductsSpec : QuerySpecification<Product>
    {
        public PaginatedProductsSpec(int page, int pageSize)
        {
            Criteria = p => p.IsActive;
            ApplyPaging((page - 1) * pageSize, pageSize);
        }
    }

    private sealed class OrderedProductsSpec : QuerySpecification<Product>
    {
        public OrderedProductsSpec(bool descending = false)
        {
            Criteria = p => p.IsActive;

            if (descending)
                ApplyOrderByDescending(p => p.Price);
            else
                ApplyOrderBy(p => p.Price);
        }
    }

    private sealed class StringIncludeSpec : QuerySpecification<Product>
    {
        public StringIncludeSpec()
        {
            Criteria = p => p.IsActive;
            AddInclude("Category.SubCategories");
        }
    }

    private sealed class ProjectedProductsSpec : QuerySpecification<Product, string>
    {
        public ProjectedProductsSpec()
        {
            Criteria = p => p.IsActive;
            Selector = p => p.Name;
        }
    }

    #region Criteria Tests

    [Fact]
    public void ToExpression_WithCriteria_ReturnsCorrectExpression()
    {
        // Arrange
        var spec = new ActiveProductsQuerySpec();
        var products = new List<Product>
        {
            new() { Id = 1, IsActive = true },
            new() { Id = 2, IsActive = false }
        }.AsQueryable();

        // Act
        var result = products.Where(spec.ToExpression()).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public void ToExpression_WithoutCriteria_ReturnsAlwaysTrue()
    {
        // Arrange
        var spec = new EmptySpec();
        var products = new List<Product>
        {
            new() { Id = 1, IsActive = true },
            new() { Id = 2, IsActive = false }
        }.AsQueryable();

        // Act
        var result = products.Where(spec.ToExpression()).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    private sealed class EmptySpec : QuerySpecification<Product>
    {
        // No criteria set
    }

    #endregion

    #region Include Tests

    [Fact]
    public void AddInclude_Expression_AddsToIncludesList()
    {
        // Arrange & Act
        var spec = new ProductsWithCategorySpec();

        // Assert
        spec.Includes.Should().ContainSingle();
    }

    [Fact]
    public void AddInclude_String_AddsToIncludeStringsList()
    {
        // Arrange & Act
        var spec = new StringIncludeSpec();

        // Assert
        spec.IncludeStrings.Should().ContainSingle();
        spec.IncludeStrings[0].Should().Be("Category.SubCategories");
    }

    [Fact]
    public void AddInclude_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act
        var act = () => spec.TestAddInclude(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("includeExpression");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInclude_NullOrEmptyString_ThrowsArgumentException(string? includeString)
    {
        // Arrange
        var spec = new TestSpec();

        // Act
        var act = () => spec.TestAddIncludeString(includeString!);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName(nameof(includeString));
    }

    private sealed class TestSpec : QuerySpecification<Product>
    {
        public void TestAddInclude(Expression<Func<Product, object>> include)
            => AddInclude(include);

        public void TestAddIncludeString(string include)
            => AddInclude(include);

        public void TestApplyPaging(int skip, int take)
            => ApplyPaging(skip, take);

        public void TestApplyOrderBy(Expression<Func<Product, object>> orderBy)
            => ApplyOrderBy(orderBy);

        public void TestApplyOrderByDescending(Expression<Func<Product, object>> orderByDescending)
            => ApplyOrderByDescending(orderByDescending);
    }

    #endregion

    #region Paging Tests

    [Fact]
    public void ApplyPaging_SetsSkipAndTake()
    {
        // Arrange & Act
        var spec = new PaginatedProductsSpec(page: 2, pageSize: 10);

        // Assert
        spec.Skip.Should().Be(10);
        spec.Take.Should().Be(10);
        spec.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void ApplyPaging_NegativeSkip_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act
        var act = () => spec.TestApplyPaging(-1, 10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("skip");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ApplyPaging_InvalidTake_ThrowsArgumentOutOfRangeException(int take)
    {
        // Arrange
        var spec = new TestSpec();

        // Act
        var act = () => spec.TestApplyPaging(0, take);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(take));
    }

    [Fact]
    public void IsPagingEnabled_NoPaging_ReturnsFalse()
    {
        // Arrange & Act
        var spec = new ActiveProductsQuerySpec();

        // Assert
        spec.IsPagingEnabled.Should().BeFalse();
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public void ApplyOrderBy_SetsOrderBy()
    {
        // Arrange & Act
        var spec = new OrderedProductsSpec(descending: false);

        // Assert
        spec.OrderBy.Should().NotBeNull();
        spec.OrderByDescending.Should().BeNull();
    }

    [Fact]
    public void ApplyOrderByDescending_SetsOrderByDescending()
    {
        // Arrange & Act
        var spec = new OrderedProductsSpec(descending: true);

        // Assert
        spec.OrderBy.Should().BeNull();
        spec.OrderByDescending.Should().NotBeNull();
    }

    [Fact]
    public void ApplyOrderBy_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act
        var act = () => spec.TestApplyOrderBy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("orderByExpression");
    }

    [Fact]
    public void ApplyOrderByDescending_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestSpec();

        // Act
        var act = () => spec.TestApplyOrderByDescending(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("orderByDescendingExpression");
    }

    #endregion

    #region Tracking Options Tests

    [Fact]
    public void AsNoTracking_DefaultsToTrue()
    {
        // Arrange & Act
        var spec = new ActiveProductsQuerySpec();

        // Assert
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void AsSplitQuery_DefaultsToFalse()
    {
        // Arrange & Act
        var spec = new ActiveProductsQuerySpec();

        // Assert
        spec.AsSplitQuery.Should().BeFalse();
    }

    #endregion

    #region Projection Tests

    [Fact]
    public void Selector_WhenSet_ReturnsExpression()
    {
        // Arrange & Act
        var spec = new ProjectedProductsSpec();

        // Assert
        spec.Selector.Should().NotBeNull();
    }

    [Fact]
    public void Selector_CanBeUsedForProjection()
    {
        // Arrange
        var spec = new ProjectedProductsSpec();
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Widget", IsActive = true },
            new() { Id = 2, Name = "Gadget", IsActive = true }
        }.AsQueryable();

        // Act
        var names = products
            .Where(spec.ToExpression())
            .Select(spec.Selector!)
            .ToList();

        // Assert
        names.Should().Contain("Widget");
        names.Should().Contain("Gadget");
    }

    #endregion
}
