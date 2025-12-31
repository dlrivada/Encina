namespace Encina.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="RetryAttribute"/>.
/// Verifies default values and property initialization.
/// </summary>
public class RetryAttributeTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        // Act
        var attribute = new RetryAttribute();

        // Assert
        attribute.MaxAttempts.ShouldBe(3);
        attribute.BackoffType.ShouldBe(BackoffType.Exponential);
        attribute.BaseDelayMs.ShouldBe(1000);
        attribute.MaxDelayMs.ShouldBe(30000);
        attribute.RetryOnAllExceptions.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithCustomValues_ShouldSetProperties()
    {
        // Act
        var attribute = new RetryAttribute
        {
            MaxAttempts = 5,
            BackoffType = BackoffType.Linear,
            BaseDelayMs = 2000,
            MaxDelayMs = 60000,
            RetryOnAllExceptions = true
        };

        // Assert
        attribute.MaxAttempts.ShouldBe(5);
        attribute.BackoffType.ShouldBe(BackoffType.Linear);
        attribute.BaseDelayMs.ShouldBe(2000);
        attribute.MaxDelayMs.ShouldBe(60000);
        attribute.RetryOnAllExceptions.ShouldBeTrue();
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var requestType = typeof(TestRetryRequest);

        // Act
        var attribute = requestType.GetCustomAttributes(typeof(RetryAttribute), false).FirstOrDefault() as RetryAttribute;

        // Assert
        attribute.ShouldNotBeNull("RetryAttribute should be applicable to classes");
        attribute!.MaxAttempts.ShouldBe(5);
        attribute.BackoffType.ShouldBe(BackoffType.Constant);
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_ShouldPreventDuplicates()
    {
        // Arrange
        var attributeUsage = typeof(RetryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.AllowMultiple.ShouldBeFalse("only one RetryAttribute should be allowed per class");
    }

    [Fact]
    public void Attribute_InheritedTrue_ShouldInheritFromBaseClass()
    {
        // Arrange
        var attributeUsage = typeof(RetryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.Inherited.ShouldBeTrue("RetryAttribute should be inherited by derived classes");
    }

    [RetryAttribute(MaxAttempts = 5, BackoffType = BackoffType.Constant)]
    private sealed record TestRetryRequest : IRequest<string>;
}
