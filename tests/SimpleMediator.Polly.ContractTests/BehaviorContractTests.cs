namespace SimpleMediator.Polly.ContractTests;

/// <summary>
/// Contract tests for Polly pipeline behaviors.
/// Verifies that behaviors implement required interfaces correctly.
/// </summary>
public class BehaviorContractTests
{
    [Fact]
    public void RetryPipelineBehavior_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behaviorType = typeof(RetryPipelineBehavior<,>);

        // Assert
        behaviorType.GetInterfaces().Should().Contain(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>),
            "RetryPipelineBehavior must implement IPipelineBehavior<,>");
    }

    [Fact]
    public void CircuitBreakerPipelineBehavior_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behaviorType = typeof(CircuitBreakerPipelineBehavior<,>);

        // Assert
        behaviorType.GetInterfaces().Should().Contain(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>),
            "CircuitBreakerPipelineBehavior must implement IPipelineBehavior<,>");
    }

    [Fact]
    public void RetryAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(RetryAttribute).Should().BeDerivedFrom<Attribute>("RetryAttribute must inherit from Attribute");
    }

    [Fact]
    public void CircuitBreakerAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(CircuitBreakerAttribute).Should().BeDerivedFrom<Attribute>("CircuitBreakerAttribute must inherit from Attribute");
    }

    [Fact]
    public void RetryAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(RetryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void CircuitBreakerAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void RetryPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(RetryPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.Should().NotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(ValueTask<>));
    }

    [Fact]
    public void CircuitBreakerPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(CircuitBreakerPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.Should().NotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(ValueTask<>));
    }
}
