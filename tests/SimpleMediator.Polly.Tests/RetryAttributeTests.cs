namespace SimpleMediator.Polly.Tests;

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
        attribute.MaxAttempts.Should().Be(3);
        attribute.BackoffType.Should().Be(BackoffType.Exponential);
        attribute.BaseDelayMs.Should().Be(1000);
        attribute.MaxDelayMs.Should().Be(30000);
        attribute.RetryOnAllExceptions.Should().BeFalse();
    }

    [Fact]
    public void MaxAttempts_ShouldDefault3()
    {
        // Act
        var attribute = new RetryAttribute();

        // Assert
        attribute.MaxAttempts.Should().Be(3, "default should be 3 attempts (1 initial + 2 retries)");
    }

    [Fact]
    public void BackoffType_ShouldDefaultExponential()
    {
        // Act
        var attribute = new RetryAttribute();

        // Assert
        attribute.BackoffType.Should().Be(BackoffType.Exponential, "exponential backoff is recommended default");
    }

    [Fact]
    public void BaseDelayMs_ShouldDefault1000()
    {
        // Act
        var attribute = new RetryAttribute();

        // Assert
        attribute.BaseDelayMs.Should().Be(1000, "default should be 1 second (1000ms)");
    }

    [Fact]
    public void MaxDelayMs_ShouldDefault30000()
    {
        // Act
        var attribute = new RetryAttribute();

        // Assert
        attribute.MaxDelayMs.Should().Be(30000, "default should be 30 seconds (30000ms)");
    }

    [Fact]
    public void RetryOnAllExceptions_ShouldDefaultFalse()
    {
        // Act
        var attribute = new RetryAttribute();

        // Assert
        attribute.RetryOnAllExceptions.Should().BeFalse("default should only retry transient exceptions");
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
        attribute.MaxAttempts.Should().Be(5);
        attribute.BackoffType.Should().Be(BackoffType.Linear);
        attribute.BaseDelayMs.Should().Be(2000);
        attribute.MaxDelayMs.Should().Be(60000);
        attribute.RetryOnAllExceptions.Should().BeTrue();
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var requestType = typeof(TestRetryRequest);

        // Act
        var attribute = requestType.GetCustomAttributes(typeof(RetryAttribute), false).FirstOrDefault() as RetryAttribute;

        // Assert
        attribute.Should().NotBeNull("RetryAttribute should be applicable to classes");
        attribute!.MaxAttempts.Should().Be(5);
        attribute.BackoffType.Should().Be(BackoffType.Constant);
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_ShouldPreventDuplicates()
    {
        // Arrange
        var attributeUsage = typeof(RetryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeFalse("only one RetryAttribute should be allowed per class");
    }

    [Fact]
    public void Attribute_InheritedTrue_ShouldInheritFromBaseClass()
    {
        // Arrange
        var attributeUsage = typeof(RetryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.Inherited.Should().BeTrue("RetryAttribute should be inherited by derived classes");
    }

    [RetryAttribute(MaxAttempts = 5, BackoffType = BackoffType.Constant)]
    private sealed record TestRetryRequest : IRequest<string>;
}
