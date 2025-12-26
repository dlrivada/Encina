namespace Encina.Polly.ContractTests;

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

    #region RateLimiting Contract Tests

    [Fact]
    public void RateLimitingPipelineBehavior_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behaviorType = typeof(RateLimitingPipelineBehavior<,>);

        // Assert
        behaviorType.GetInterfaces().Should().Contain(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>),
            "RateLimitingPipelineBehavior must implement IPipelineBehavior<,>");
    }

    [Fact]
    public void RateLimitAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(RateLimitAttribute).Should().BeDerivedFrom<Attribute>("RateLimitAttribute must inherit from Attribute");
    }

    [Fact]
    public void RateLimitAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(RateLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void RateLimitingPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(RateLimitingPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.Should().NotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(ValueTask<>));
    }

    [Fact]
    public void IRateLimiter_MustHaveAcquireAsyncMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var acquireMethod = rateLimiterType.GetMethod("AcquireAsync");

        // Assert
        acquireMethod.Should().NotBeNull("AcquireAsync method must exist on IRateLimiter");
        acquireMethod!.ReturnType.Should().Be(typeof(ValueTask<RateLimitResult>));
    }

    [Fact]
    public void IRateLimiter_MustHaveRecordSuccessMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("RecordSuccess");

        // Assert
        method.Should().NotBeNull("RecordSuccess method must exist on IRateLimiter");
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void IRateLimiter_MustHaveRecordFailureMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("RecordFailure");

        // Assert
        method.Should().NotBeNull("RecordFailure method must exist on IRateLimiter");
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void IRateLimiter_MustHaveGetStateMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("GetState");

        // Assert
        method.Should().NotBeNull("GetState method must exist on IRateLimiter");
        method!.ReturnType.Should().Be(typeof(RateLimitState?), "GetState returns nullable RateLimitState");
    }

    [Fact]
    public void IRateLimiter_MustHaveResetMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("Reset");

        // Assert
        method.Should().NotBeNull("Reset method must exist on IRateLimiter");
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void AdaptiveRateLimiter_MustImplementIRateLimiter()
    {
        // Assert
        typeof(AdaptiveRateLimiter).Should().Implement<IRateLimiter>("AdaptiveRateLimiter must implement IRateLimiter");
    }

    [Fact]
    public void RateLimitState_ShouldHaveExpectedValues()
    {
        // Arrange
        var values = Enum.GetValues<RateLimitState>();

        // Assert
        values.Should().Contain(RateLimitState.Normal);
        values.Should().Contain(RateLimitState.Throttled);
        values.Should().Contain(RateLimitState.Recovering);
    }

    [Fact]
    public void RateLimitResult_ShouldBeReadonlyRecordStruct()
    {
        // Assert
        typeof(RateLimitResult).IsValueType.Should().BeTrue("RateLimitResult should be a value type");
        // Record structs don't have <Clone>$ method; check for PrintMembers or EqualityContract instead
        typeof(RateLimitResult)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Any(m => m.Name == "PrintMembers")
            .Should().BeTrue("RateLimitResult should be a record struct (has PrintMembers)");
    }

    [Fact]
    public void RateLimitResult_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(RateLimitResult);

        // Assert
        type.GetProperty("IsAllowed").Should().NotBeNull();
        type.GetProperty("CurrentState").Should().NotBeNull();
        type.GetProperty("RetryAfter").Should().NotBeNull();
        type.GetProperty("CurrentCount").Should().NotBeNull();
        type.GetProperty("CurrentLimit").Should().NotBeNull();
        type.GetProperty("ErrorRate").Should().NotBeNull();
    }

    [Fact]
    public void RateLimitAttribute_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(RateLimitAttribute);

        // Assert
        type.GetProperty("MaxRequestsPerWindow").Should().NotBeNull();
        type.GetProperty("WindowSizeSeconds").Should().NotBeNull();
        type.GetProperty("ErrorThresholdPercent").Should().NotBeNull();
        type.GetProperty("CooldownSeconds").Should().NotBeNull();
        type.GetProperty("RampUpFactor").Should().NotBeNull();
        type.GetProperty("EnableAdaptiveThrottling").Should().NotBeNull();
        type.GetProperty("MinimumThroughputForThrottling").Should().NotBeNull();
    }

    #endregion
}
