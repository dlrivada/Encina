using Encina.Caching;
namespace Encina.UnitTests.Caching.Base;

/// <summary>
/// Unit tests for <see cref="InvalidatesCacheAttribute"/>.
/// </summary>
public class InvalidatesCacheAttributeTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var attribute = new InvalidatesCacheAttribute();

        // Assert
        attribute.KeyPattern.ShouldBe("*");
        attribute.BroadcastInvalidation.ShouldBeTrue();
        attribute.DelayMilliseconds.ShouldBe(0);
        attribute.Delay.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void ConstructorWithKeyPattern_SetsKeyPattern()
    {
        // Arrange & Act
        var attribute = new InvalidatesCacheAttribute("product:{ProductId}:*");

        // Assert
        attribute.KeyPattern.ShouldBe("product:{ProductId}:*");
        attribute.BroadcastInvalidation.ShouldBeTrue();
        attribute.DelayMilliseconds.ShouldBe(0);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void KeyPattern_CanBeSet()
    {
        // Arrange & Act
        var attribute = new InvalidatesCacheAttribute
        {
            KeyPattern = "custom:pattern:*"
        };

        // Assert
        attribute.KeyPattern.ShouldBe("custom:pattern:*");
    }

    [Fact]
    public void BroadcastInvalidation_CanBeDisabled()
    {
        // Arrange & Act
        var attribute = new InvalidatesCacheAttribute
        {
            BroadcastInvalidation = false
        };

        // Assert
        attribute.BroadcastInvalidation.ShouldBeFalse();
    }

    [Fact]
    public void DelayMilliseconds_CanBeSet()
    {
        // Arrange & Act
        var attribute = new InvalidatesCacheAttribute
        {
            DelayMilliseconds = 500
        };

        // Assert
        attribute.DelayMilliseconds.ShouldBe(500);
        attribute.Delay.ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Delay_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var attribute = new InvalidatesCacheAttribute
        {
            DelayMilliseconds = 1000
        };

        // Act & Assert
        attribute.Delay.ShouldBe(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var attribute = new InvalidatesCacheAttribute
        {
            KeyPattern = "products:category:{CategoryId}:*",
            BroadcastInvalidation = false,
            DelayMilliseconds = 250
        };

        // Assert
        attribute.KeyPattern.ShouldBe("products:category:{CategoryId}:*");
        attribute.BroadcastInvalidation.ShouldBeFalse();
        attribute.DelayMilliseconds.ShouldBe(250);
        attribute.Delay.ShouldBe(TimeSpan.FromMilliseconds(250));
    }

    #endregion

    #region Attribute Usage Tests

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var type = typeof(TestCommandWithInvalidation);
        var attributes = type.GetCustomAttributes(typeof(InvalidatesCacheAttribute), false);

        // Assert
        attributes.Length.ShouldBe(1);
        var attribute = attributes[0] as InvalidatesCacheAttribute;
        attribute.ShouldNotBeNull();
        attribute!.KeyPattern.ShouldBe("test:*");
    }

    [Fact]
    public void Attribute_CanBeAppliedMultipleTimes()
    {
        // Arrange & Act
        var type = typeof(TestCommandWithMultipleInvalidations);
        var attributes = type.GetCustomAttributes(typeof(InvalidatesCacheAttribute), false);

        // Assert
        attributes.Length.ShouldBe(3);
        var patterns = attributes.Cast<InvalidatesCacheAttribute>().Select(a => a.KeyPattern).ToList();
        patterns.ShouldContain("product:{ProductId}:*");
        patterns.ShouldContain("products:list:*");
        patterns.ShouldContain("search:*");
    }

    [Fact]
    public void Attribute_IsInheritedFromBaseClass()
    {
        // Arrange & Act
        var type = typeof(DerivedTestCommand);
        var attributes = type.GetCustomAttributes(typeof(InvalidatesCacheAttribute), true);

        // Assert
        attributes.Length.ShouldBe(1);
    }

    #endregion

    #region Test Types

    [InvalidatesCache(KeyPattern = "test:*")]
    private sealed record TestCommandWithInvalidation(Guid Id) : IRequest<string>;

    [InvalidatesCache(KeyPattern = "product:{ProductId}:*")]
    [InvalidatesCache(KeyPattern = "products:list:*")]
    [InvalidatesCache(KeyPattern = "search:*")]
    private sealed record TestCommandWithMultipleInvalidations(Guid ProductId) : IRequest<string>;

    [InvalidatesCache(KeyPattern = "base:*")]
    private record BaseTestCommand(Guid Id) : IRequest<string>;

    private sealed record DerivedTestCommand(Guid Id) : BaseTestCommand(Id);

    #endregion
}
