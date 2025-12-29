using System.Linq.Expressions;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Unit tests for the Specification Pattern implementation.
/// </summary>
public class SpecificationTests
{
    private sealed class Product
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class ActiveProductsSpec : Specification<Product>
    {
        public override Expression<Func<Product, bool>> ToExpression()
            => p => p.IsActive;
    }

    private sealed class HighPriceSpec : Specification<Product>
    {
        private readonly decimal _minPrice;

        public HighPriceSpec(decimal minPrice) => _minPrice = minPrice;

        public override Expression<Func<Product, bool>> ToExpression()
            => p => p.Price >= _minPrice;
    }

    private sealed class NameContainsSpec : Specification<Product>
    {
        private readonly string _substring;

        public NameContainsSpec(string substring) => _substring = substring;

        public override Expression<Func<Product, bool>> ToExpression()
            => p => p.Name.Contains(_substring);
    }

    #region ToExpression Tests

    [Fact]
    public void ToExpression_ReturnsValidExpression()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        var expression = spec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        expression.Should().BeAssignableTo<Expression<Func<Product, bool>>>();
    }

    [Fact]
    public void ToExpression_CanBeUsedWithLinq()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, IsActive = true },
            new() { Id = 2, IsActive = false },
            new() { Id = 3, IsActive = true }
        }.AsQueryable();

        var spec = new ActiveProductsSpec();

        // Act
        var result = products.Where(spec.ToExpression()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.IsActive).Should().BeTrue();
    }

    #endregion

    #region ToFunc Tests

    [Fact]
    public void ToFunc_ReturnsCompiledFunction()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        var func = spec.ToFunc();

        // Assert
        func.Should().NotBeNull();
    }

    [Fact]
    public void ToFunc_EvaluatesCorrectly()
    {
        // Arrange
        var spec = new ActiveProductsSpec();
        var func = spec.ToFunc();
        var activeProduct = new Product { IsActive = true };
        var inactiveProduct = new Product { IsActive = false };

        // Act & Assert
        func(activeProduct).Should().BeTrue();
        func(inactiveProduct).Should().BeFalse();
    }

    #endregion

    #region IsSatisfiedBy Tests

    [Fact]
    public void IsSatisfiedBy_ReturnsTrueForMatchingEntity()
    {
        // Arrange
        var spec = new ActiveProductsSpec();
        var product = new Product { IsActive = true };

        // Act
        var result = spec.IsSatisfiedBy(product);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalseForNonMatchingEntity()
    {
        // Arrange
        var spec = new ActiveProductsSpec();
        var product = new Product { IsActive = false };

        // Act
        var result = spec.IsSatisfiedBy(product);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        var act = () => spec.IsSatisfiedBy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("entity");
    }

    #endregion

    #region And Composition Tests

    [Fact]
    public void And_CombinesTwoSpecifications()
    {
        // Arrange
        var activeSpec = new ActiveProductsSpec();
        var highPriceSpec = new HighPriceSpec(100);
        var combinedSpec = activeSpec.And(highPriceSpec);

        var matchingProduct = new Product { IsActive = true, Price = 150 };
        var inactiveHighPrice = new Product { IsActive = false, Price = 150 };
        var activeLowPrice = new Product { IsActive = true, Price = 50 };

        // Act & Assert
        combinedSpec.IsSatisfiedBy(matchingProduct).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(inactiveHighPrice).Should().BeFalse();
        combinedSpec.IsSatisfiedBy(activeLowPrice).Should().BeFalse();
    }

    [Fact]
    public void And_NullOther_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        var act = () => spec.And(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("other");
    }

    [Fact]
    public void And_MultipleSpecifications_ChainsCorrectly()
    {
        // Arrange
        var spec = new ActiveProductsSpec()
            .And(new HighPriceSpec(100))
            .And(new NameContainsSpec("Pro"));

        var matching = new Product { IsActive = true, Price = 150, Name = "Pro Widget" };
        var nonMatching = new Product { IsActive = true, Price = 150, Name = "Widget" };

        // Act & Assert
        spec.IsSatisfiedBy(matching).Should().BeTrue();
        spec.IsSatisfiedBy(nonMatching).Should().BeFalse();
    }

    #endregion

    #region Or Composition Tests

    [Fact]
    public void Or_CombinesTwoSpecifications()
    {
        // Arrange
        var activeSpec = new ActiveProductsSpec();
        var highPriceSpec = new HighPriceSpec(100);
        var combinedSpec = activeSpec.Or(highPriceSpec);

        var activeOnly = new Product { IsActive = true, Price = 50 };
        var highPriceOnly = new Product { IsActive = false, Price = 150 };
        var neither = new Product { IsActive = false, Price = 50 };

        // Act & Assert
        combinedSpec.IsSatisfiedBy(activeOnly).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(highPriceOnly).Should().BeTrue();
        combinedSpec.IsSatisfiedBy(neither).Should().BeFalse();
    }

    [Fact]
    public void Or_NullOther_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        var act = () => spec.Or(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("other");
    }

    #endregion

    #region Not Composition Tests

    [Fact]
    public void Not_NegatesSpecification()
    {
        // Arrange
        var activeSpec = new ActiveProductsSpec();
        var notActiveSpec = activeSpec.Not();

        var activeProduct = new Product { IsActive = true };
        var inactiveProduct = new Product { IsActive = false };

        // Act & Assert
        notActiveSpec.IsSatisfiedBy(activeProduct).Should().BeFalse();
        notActiveSpec.IsSatisfiedBy(inactiveProduct).Should().BeTrue();
    }

    [Fact]
    public void Not_DoubleNegation_ReturnsOriginal()
    {
        // Arrange
        var spec = new ActiveProductsSpec();
        var doubleNegation = spec.Not().Not();

        var activeProduct = new Product { IsActive = true };
        var inactiveProduct = new Product { IsActive = false };

        // Act & Assert
        doubleNegation.IsSatisfiedBy(activeProduct).Should().Be(spec.IsSatisfiedBy(activeProduct));
        doubleNegation.IsSatisfiedBy(inactiveProduct).Should().Be(spec.IsSatisfiedBy(inactiveProduct));
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToExpression_Works()
    {
        // Arrange
        var spec = new ActiveProductsSpec();

        // Act
        Expression<Func<Product, bool>> expression = spec;

        // Assert
        expression.Should().NotBeNull();
    }

    #endregion
}
