using System.Linq.Expressions;

namespace Encina.DomainModeling.GuardTests;

/// <summary>
/// Guard tests for the Specification Pattern types.
/// </summary>
public class SpecificationGuardTests
{
    private sealed class Product
    {
        public int Id { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class ActiveProductsSpec : Specification<Product>
    {
        public override Expression<Func<Product, bool>> ToExpression()
            => p => p.IsActive;
    }

    private sealed class TestQuerySpec : QuerySpecification<Product>
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

    #region Specification Guards

    [Fact]
    public void IsSatisfiedBy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        Action act = () => { _ = spec.IsSatisfiedBy(null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void And_NullOther_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        var act = () => spec.And(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("other");
    }

    [Fact]
    public void Or_NullOther_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        var act = () => spec.Or(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("other");
    }

    #endregion

    #region QuerySpecification Guards

    [Fact]
    public void AddInclude_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestQuerySpec();

        // Act
        var act = () => spec.TestAddInclude(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("includeExpression");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInclude_NullOrEmptyString_ThrowsArgumentException(string? includeString)
    {
        // Arrange
        var spec = new TestQuerySpec();

        // Act
        var act = () => spec.TestAddIncludeString(includeString!);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("includeString");
    }

    [Fact]
    public void ApplyPaging_NegativeSkip_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var spec = new TestQuerySpec();

        // Act
        var act = () => spec.TestApplyPaging(-1, 10);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("skip");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ApplyPaging_InvalidTake_ThrowsArgumentOutOfRangeException(int take)
    {
        // Arrange
        var spec = new TestQuerySpec();

        // Act
        var act = () => spec.TestApplyPaging(0, take);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("take");
    }

    [Fact]
    public void ApplyOrderBy_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestQuerySpec();

        // Act
        var act = () => spec.TestApplyOrderBy(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("orderByExpression");
    }

    [Fact]
    public void ApplyOrderByDescending_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestQuerySpec();

        // Act
        var act = () => spec.TestApplyOrderByDescending(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("orderByDescendingExpression");
    }

    #endregion
}
