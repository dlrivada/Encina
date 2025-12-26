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

    #region Bulkhead Contract Tests

    [Fact]
    public void BulkheadPipelineBehavior_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behaviorType = typeof(BulkheadPipelineBehavior<,>);

        // Assert
        behaviorType.GetInterfaces().Should().Contain(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>),
            "BulkheadPipelineBehavior must implement IPipelineBehavior<,>");
    }

    [Fact]
    public void BulkheadAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(BulkheadAttribute).Should().BeDerivedFrom<Attribute>("BulkheadAttribute must inherit from Attribute");
    }

    [Fact]
    public void BulkheadAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(BulkheadAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void BulkheadPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(BulkheadPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.Should().NotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(ValueTask<>));
    }

    [Fact]
    public void IBulkheadManager_MustHaveTryAcquireAsyncMethod()
    {
        // Arrange
        var managerType = typeof(IBulkheadManager);
        var method = managerType.GetMethod("TryAcquireAsync");

        // Assert
        method.Should().NotBeNull("TryAcquireAsync method must exist on IBulkheadManager");
        method!.ReturnType.Should().Be(typeof(ValueTask<BulkheadAcquireResult>));
    }

    [Fact]
    public void IBulkheadManager_MustHaveGetMetricsMethod()
    {
        // Arrange
        var managerType = typeof(IBulkheadManager);
        var method = managerType.GetMethod("GetMetrics");

        // Assert
        method.Should().NotBeNull("GetMetrics method must exist on IBulkheadManager");
        method!.ReturnType.Should().Be(typeof(BulkheadMetrics?));
    }

    [Fact]
    public void IBulkheadManager_MustHaveResetMethod()
    {
        // Arrange
        var managerType = typeof(IBulkheadManager);
        var method = managerType.GetMethod("Reset");

        // Assert
        method.Should().NotBeNull("Reset method must exist on IBulkheadManager");
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void BulkheadManager_MustImplementIBulkheadManager()
    {
        // Assert
        typeof(BulkheadManager).Should().Implement<IBulkheadManager>("BulkheadManager must implement IBulkheadManager");
    }

    [Fact]
    public void BulkheadManager_MustImplementIDisposable()
    {
        // Assert
        typeof(BulkheadManager).Should().Implement<IDisposable>("BulkheadManager must implement IDisposable");
    }

    [Fact]
    public void BulkheadRejectionReason_ShouldHaveExpectedValues()
    {
        // Arrange
        var values = Enum.GetValues<BulkheadRejectionReason>();

        // Assert
        values.Should().Contain(BulkheadRejectionReason.None);
        values.Should().Contain(BulkheadRejectionReason.BulkheadFull);
        values.Should().Contain(BulkheadRejectionReason.QueueTimeout);
        values.Should().Contain(BulkheadRejectionReason.Cancelled);
    }

    [Fact]
    public void BulkheadAcquireResult_ShouldBeReadonlyRecordStruct()
    {
        // Assert
        typeof(BulkheadAcquireResult).IsValueType.Should().BeTrue("BulkheadAcquireResult should be a value type");
        typeof(BulkheadAcquireResult)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Any(m => m.Name == "PrintMembers")
            .Should().BeTrue("BulkheadAcquireResult should be a record struct (has PrintMembers)");
    }

    [Fact]
    public void BulkheadAcquireResult_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(BulkheadAcquireResult);

        // Assert
        type.GetProperty("IsAcquired").Should().NotBeNull();
        type.GetProperty("RejectionReason").Should().NotBeNull();
        type.GetProperty("Releaser").Should().NotBeNull();
        type.GetProperty("Metrics").Should().NotBeNull();
    }

    [Fact]
    public void BulkheadMetrics_ShouldBeReadonlyRecordStruct()
    {
        // Assert
        typeof(BulkheadMetrics).IsValueType.Should().BeTrue("BulkheadMetrics should be a value type");
        typeof(BulkheadMetrics)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Any(m => m.Name == "PrintMembers")
            .Should().BeTrue("BulkheadMetrics should be a record struct (has PrintMembers)");
    }

    [Fact]
    public void BulkheadMetrics_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(BulkheadMetrics);

        // Assert
        type.GetProperty("CurrentConcurrency").Should().NotBeNull();
        type.GetProperty("CurrentQueuedCount").Should().NotBeNull();
        type.GetProperty("MaxConcurrency").Should().NotBeNull();
        type.GetProperty("MaxQueuedActions").Should().NotBeNull();
        type.GetProperty("TotalAcquired").Should().NotBeNull();
        type.GetProperty("TotalRejected").Should().NotBeNull();
        type.GetProperty("ConcurrencyUtilization").Should().NotBeNull();
        type.GetProperty("QueueUtilization").Should().NotBeNull();
        type.GetProperty("RejectionRate").Should().NotBeNull();
    }

    [Fact]
    public void BulkheadAttribute_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(BulkheadAttribute);

        // Assert
        type.GetProperty("MaxConcurrency").Should().NotBeNull();
        type.GetProperty("MaxQueuedActions").Should().NotBeNull();
        type.GetProperty("QueueTimeoutMs").Should().NotBeNull();
    }

    #endregion
}
