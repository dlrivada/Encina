using Encina.DomainModeling;
using LanguageExt;
using Shouldly;

namespace Encina.DomainModeling.Tests;

public sealed class DomainDslExtensionsTests
{
    private sealed class TestProduct
    {
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
    }

    private sealed class ActiveProductSpec : Specification<TestProduct>
    {
        public override System.Linq.Expressions.Expression<Func<TestProduct, bool>> ToExpression() => p => p.Price > 0;
    }

    private sealed class PremiumProductSpec : Specification<TestProduct>
    {
        public override System.Linq.Expressions.Expression<Func<TestProduct, bool>> ToExpression() => p => p.Price > 100;
    }

    private sealed class TestBusinessRule(bool satisfied, string message = "Rule violated") : IBusinessRule
    {
        public string ErrorCode => "TEST_RULE";
        public string ErrorMessage => message;
        public bool IsSatisfied() => satisfied;
    }

    [Fact]
    public void Is_Extension_ReturnsTrue_WhenSpecificationSatisfied()
    {
        // Arrange
        var product = new TestProduct { Name = "Active", Price = 50m };
        var spec = new ActiveProductSpec();

        // Act
        var result = product.Is(spec);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Is_Extension_ReturnsFalse_WhenSpecificationNotSatisfied()
    {
        // Arrange
        var product = new TestProduct { Name = "Inactive", Price = 0m };
        var spec = new ActiveProductSpec();

        // Act
        var result = product.Is(spec);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Is_Extension_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        TestProduct? product = null;
        var spec = new ActiveProductSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => product!.Is(spec));
    }

    [Fact]
    public void Is_Extension_NullSpec_ThrowsArgumentNullException()
    {
        // Arrange
        var product = new TestProduct { Name = "Test", Price = 10m };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => product.Is(null!));
    }

    [Fact]
    public void Satisfies_Extension_ReturnsTrue_WhenSpecificationSatisfied()
    {
        // Arrange
        var product = new TestProduct { Name = "Active", Price = 50m };
        var spec = new ActiveProductSpec();

        // Act
        var result = product.Satisfies(spec);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Violates_Extension_ReturnsTrue_WhenSpecificationNotSatisfied()
    {
        // Arrange
        var product = new TestProduct { Name = "Inactive", Price = 0m };
        var spec = new ActiveProductSpec();

        // Act
        var result = product.Violates(spec);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Violates_Extension_ReturnsFalse_WhenSpecificationSatisfied()
    {
        // Arrange
        var product = new TestProduct { Name = "Active", Price = 50m };
        var spec = new ActiveProductSpec();

        // Act
        var result = product.Violates(spec);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Passes_Extension_ReturnsTrue_WhenRuleSatisfied()
    {
        // Arrange
        var rule = new TestBusinessRule(satisfied: true);

        // Act
        var result = rule.Passes();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Passes_Extension_ReturnsFalse_WhenRuleNotSatisfied()
    {
        // Arrange
        var rule = new TestBusinessRule(satisfied: false);

        // Act
        var result = rule.Passes();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Passes_Extension_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        IBusinessRule? rule = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => rule!.Passes());
    }

    [Fact]
    public void Fails_Extension_ReturnsTrue_WhenRuleNotSatisfied()
    {
        // Arrange
        var rule = new TestBusinessRule(satisfied: false);

        // Act
        var result = rule.Fails();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Fails_Extension_ReturnsFalse_WhenRuleSatisfied()
    {
        // Arrange
        var rule = new TestBusinessRule(satisfied: true);

        // Act
        var result = rule.Fails();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Fails_Extension_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        IBusinessRule? rule = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => rule!.Fails());
    }

    [Fact]
    public void EnsureValid_ReturnsRight_WhenConditionTrue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.EnsureValid(v => v > 0, "Must be positive");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: v => v.ShouldBe(42),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void EnsureValid_ReturnsLeft_WhenConditionFalse()
    {
        // Arrange
        var value = -5;

        // Act
        var result = value.EnsureValid(v => v > 0, "Must be positive");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void EnsureValid_NullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => value.EnsureValid(null!, "Error"));
    }

    [Fact]
    public void EnsureValid_EmptyErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        Should.Throw<ArgumentException>(() => value.EnsureValid(v => v > 0, ""));
    }

    [Fact]
    public void EnsureNotNull_ReturnsRight_WhenValueNotNull()
    {
        // Arrange
        string? value = "test";

        // Act
        var result = value.EnsureNotNull("PropertyName");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotNull_ReturnsLeft_WhenValueNull()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.EnsureNotNull("PropertyName");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotNull_EmptyPropertyName_ThrowsArgumentException()
    {
        // Arrange
        string? value = "test";

        // Act & Assert
        Should.Throw<ArgumentException>(() => value.EnsureNotNull(""));
    }

    [Fact]
    public void EnsureNotEmpty_String_ReturnsRight_WhenValueNotEmpty()
    {
        // Arrange
        string? value = "test";

        // Act
        var result = value.EnsureNotEmpty("PropertyName");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotEmpty_String_ReturnsLeft_WhenValueEmpty()
    {
        // Arrange
        string? value = "";

        // Act
        var result = value.EnsureNotEmpty("PropertyName");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotEmpty_String_ReturnsLeft_WhenValueNull()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.EnsureNotEmpty("PropertyName");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotEmpty_Collection_ReturnsRight_WhenNotEmpty()
    {
        // Arrange
        IReadOnlyList<int>? collection = [1, 2, 3];

        // Act
        var result = collection.EnsureNotEmpty("Items");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotEmpty_Collection_ReturnsLeft_WhenEmpty()
    {
        // Arrange
        IReadOnlyList<int>? collection = [];

        // Act
        var result = collection.EnsureNotEmpty("Items");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotEmpty_Collection_ReturnsLeft_WhenNull()
    {
        // Arrange
        IReadOnlyList<int>? collection = null;

        // Act
        var result = collection.EnsureNotEmpty("Items");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

}

