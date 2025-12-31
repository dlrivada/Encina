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
        behaviorType.GetInterfaces().ShouldContain(i =>
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
        behaviorType.GetInterfaces().ShouldContain(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>),
            "CircuitBreakerPipelineBehavior must implement IPipelineBehavior<,>");
    }

    [Fact]
    public void RetryAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(RetryAttribute).IsSubclassOf(typeof(Attribute)).ShouldBeTrue("RetryAttribute must inherit from Attribute");
    }

    [Fact]
    public void CircuitBreakerAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(CircuitBreakerAttribute).IsSubclassOf(typeof(Attribute)).ShouldBeTrue("CircuitBreakerAttribute must inherit from Attribute");
    }

    [Fact]
    public void RetryAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(RetryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void CircuitBreakerAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void RetryPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(RetryPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.ShouldNotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.ShouldBeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));
    }

    [Fact]
    public void CircuitBreakerPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(CircuitBreakerPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.ShouldNotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.ShouldBeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));
    }

    #region RateLimiting Contract Tests

    [Fact]
    public void RateLimitingPipelineBehavior_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behaviorType = typeof(RateLimitingPipelineBehavior<,>);

        // Assert
        behaviorType.GetInterfaces().ShouldContain(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>),
            "RateLimitingPipelineBehavior must implement IPipelineBehavior<,>");
    }

    [Fact]
    public void RateLimitAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(RateLimitAttribute).IsSubclassOf(typeof(Attribute)).ShouldBeTrue("RateLimitAttribute must inherit from Attribute");
    }

    [Fact]
    public void RateLimitAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(RateLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void RateLimitingPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(RateLimitingPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.ShouldNotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.ShouldBeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));
    }

    [Fact]
    public void IRateLimiter_MustHaveAcquireAsyncMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var acquireMethod = rateLimiterType.GetMethod("AcquireAsync");

        // Assert
        acquireMethod.ShouldNotBeNull("AcquireAsync method must exist on IRateLimiter");
        acquireMethod!.ReturnType.ShouldBe(typeof(ValueTask<RateLimitResult>));
    }

    [Fact]
    public void IRateLimiter_MustHaveRecordSuccessMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("RecordSuccess");

        // Assert
        method.ShouldNotBeNull("RecordSuccess method must exist on IRateLimiter");
        method!.ReturnType.ShouldBe(typeof(void));
    }

    [Fact]
    public void IRateLimiter_MustHaveRecordFailureMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("RecordFailure");

        // Assert
        method.ShouldNotBeNull("RecordFailure method must exist on IRateLimiter");
        method!.ReturnType.ShouldBe(typeof(void));
    }

    [Fact]
    public void IRateLimiter_MustHaveGetStateMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("GetState");

        // Assert
        method.ShouldNotBeNull("GetState method must exist on IRateLimiter");
        method!.ReturnType.ShouldBe(typeof(RateLimitState?), "GetState returns nullable RateLimitState");
    }

    [Fact]
    public void IRateLimiter_MustHaveResetMethod()
    {
        // Arrange
        var rateLimiterType = typeof(IRateLimiter);
        var method = rateLimiterType.GetMethod("Reset");

        // Assert
        method.ShouldNotBeNull("Reset method must exist on IRateLimiter");
        method!.ReturnType.ShouldBe(typeof(void));
    }

    [Fact]
    public void AdaptiveRateLimiter_MustImplementIRateLimiter()
    {
        // Assert
        typeof(AdaptiveRateLimiter).GetInterfaces().ShouldContain(typeof(IRateLimiter), "AdaptiveRateLimiter must implement IRateLimiter");
    }

    [Fact]
    public void RateLimitState_ShouldHaveExpectedValues()
    {
        // Arrange
        var values = Enum.GetValues<RateLimitState>();

        // Assert
        values.ShouldContain(RateLimitState.Normal);
        values.ShouldContain(RateLimitState.Throttled);
        values.ShouldContain(RateLimitState.Recovering);
    }

    [Fact]
    public void RateLimitResult_ShouldBeReadonlyRecordStruct()
    {
        // Assert
        typeof(RateLimitResult).IsValueType.ShouldBeTrue("RateLimitResult should be a value type");
        // Record structs don't have <Clone>$ method; check for PrintMembers or EqualityContract instead
        typeof(RateLimitResult)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Any(m => m.Name == "PrintMembers")
            .ShouldBeTrue("RateLimitResult should be a record struct (has PrintMembers)");
    }

    [Fact]
    public void RateLimitResult_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(RateLimitResult);

        // Assert
        type.GetProperty("IsAllowed").ShouldNotBeNull();
        type.GetProperty("CurrentState").ShouldNotBeNull();
        type.GetProperty("RetryAfter").ShouldNotBeNull();
        type.GetProperty("CurrentCount").ShouldNotBeNull();
        type.GetProperty("CurrentLimit").ShouldNotBeNull();
        type.GetProperty("ErrorRate").ShouldNotBeNull();
    }

    [Fact]
    public void RateLimitAttribute_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(RateLimitAttribute);

        // Assert
        type.GetProperty("MaxRequestsPerWindow").ShouldNotBeNull();
        type.GetProperty("WindowSizeSeconds").ShouldNotBeNull();
        type.GetProperty("ErrorThresholdPercent").ShouldNotBeNull();
        type.GetProperty("CooldownSeconds").ShouldNotBeNull();
        type.GetProperty("RampUpFactor").ShouldNotBeNull();
        type.GetProperty("EnableAdaptiveThrottling").ShouldNotBeNull();
        type.GetProperty("MinimumThroughputForThrottling").ShouldNotBeNull();
    }

    #endregion

    #region Bulkhead Contract Tests

    [Fact]
    public void BulkheadPipelineBehavior_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behaviorType = typeof(BulkheadPipelineBehavior<,>);

        // Assert
        behaviorType.GetInterfaces().ShouldContain(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>),
            "BulkheadPipelineBehavior must implement IPipelineBehavior<,>");
    }

    [Fact]
    public void BulkheadAttribute_MustInheritFromAttribute()
    {
        // Assert
        typeof(BulkheadAttribute).IsSubclassOf(typeof(Attribute)).ShouldBeTrue("BulkheadAttribute must inherit from Attribute");
    }

    [Fact]
    public void BulkheadAttribute_AttributeUsage_ShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(BulkheadAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void BulkheadPipelineBehavior_HandleMethod_MustReturnValueTask()
    {
        // Arrange
        var behaviorType = typeof(BulkheadPipelineBehavior<,>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.ShouldNotBeNull("Handle method must exist");
        handleMethod!.ReturnType.IsGenericType.ShouldBeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));
    }

    [Fact]
    public void IBulkheadManager_MustHaveTryAcquireAsyncMethod()
    {
        // Arrange
        var managerType = typeof(IBulkheadManager);
        var method = managerType.GetMethod("TryAcquireAsync");

        // Assert
        method.ShouldNotBeNull("TryAcquireAsync method must exist on IBulkheadManager");
        method!.ReturnType.ShouldBe(typeof(ValueTask<BulkheadAcquireResult>));
    }

    [Fact]
    public void IBulkheadManager_MustHaveGetMetricsMethod()
    {
        // Arrange
        var managerType = typeof(IBulkheadManager);
        var method = managerType.GetMethod("GetMetrics");

        // Assert
        method.ShouldNotBeNull("GetMetrics method must exist on IBulkheadManager");
        method!.ReturnType.ShouldBe(typeof(BulkheadMetrics?));
    }

    [Fact]
    public void IBulkheadManager_MustHaveResetMethod()
    {
        // Arrange
        var managerType = typeof(IBulkheadManager);
        var method = managerType.GetMethod("Reset");

        // Assert
        method.ShouldNotBeNull("Reset method must exist on IBulkheadManager");
        method!.ReturnType.ShouldBe(typeof(void));
    }

    [Fact]
    public void BulkheadManager_MustImplementIBulkheadManager()
    {
        // Assert
        typeof(BulkheadManager).GetInterfaces().ShouldContain(typeof(IBulkheadManager), "BulkheadManager must implement IBulkheadManager");
    }

    [Fact]
    public void BulkheadManager_MustImplementIDisposable()
    {
        // Assert
        typeof(BulkheadManager).GetInterfaces().ShouldContain(typeof(IDisposable), "BulkheadManager must implement IDisposable");
    }

    [Fact]
    public void BulkheadRejectionReason_ShouldHaveExpectedValues()
    {
        // Arrange
        var values = Enum.GetValues<BulkheadRejectionReason>();

        // Assert
        values.ShouldContain(BulkheadRejectionReason.None);
        values.ShouldContain(BulkheadRejectionReason.BulkheadFull);
        values.ShouldContain(BulkheadRejectionReason.QueueTimeout);
        values.ShouldContain(BulkheadRejectionReason.Cancelled);
    }

    [Fact]
    public void BulkheadAcquireResult_ShouldBeReadonlyRecordStruct()
    {
        // Assert
        typeof(BulkheadAcquireResult).IsValueType.ShouldBeTrue("BulkheadAcquireResult should be a value type");
        typeof(BulkheadAcquireResult)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Any(m => m.Name == "PrintMembers")
            .ShouldBeTrue("BulkheadAcquireResult should be a record struct (has PrintMembers)");
    }

    [Fact]
    public void BulkheadAcquireResult_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(BulkheadAcquireResult);

        // Assert
        type.GetProperty("IsAcquired").ShouldNotBeNull();
        type.GetProperty("RejectionReason").ShouldNotBeNull();
        type.GetProperty("Releaser").ShouldNotBeNull();
        type.GetProperty("Metrics").ShouldNotBeNull();
    }

    [Fact]
    public void BulkheadMetrics_ShouldBeReadonlyRecordStruct()
    {
        // Assert
        typeof(BulkheadMetrics).IsValueType.ShouldBeTrue("BulkheadMetrics should be a value type");
        typeof(BulkheadMetrics)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Any(m => m.Name == "PrintMembers")
            .ShouldBeTrue("BulkheadMetrics should be a record struct (has PrintMembers)");
    }

    [Fact]
    public void BulkheadMetrics_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(BulkheadMetrics);

        // Assert
        type.GetProperty("CurrentConcurrency").ShouldNotBeNull();
        type.GetProperty("CurrentQueuedCount").ShouldNotBeNull();
        type.GetProperty("MaxConcurrency").ShouldNotBeNull();
        type.GetProperty("MaxQueuedActions").ShouldNotBeNull();
        type.GetProperty("TotalAcquired").ShouldNotBeNull();
        type.GetProperty("TotalRejected").ShouldNotBeNull();
        type.GetProperty("ConcurrencyUtilization").ShouldNotBeNull();
        type.GetProperty("QueueUtilization").ShouldNotBeNull();
        type.GetProperty("RejectionRate").ShouldNotBeNull();
    }

    [Fact]
    public void BulkheadAttribute_ShouldHaveRequiredProperties()
    {
        // Arrange
        var type = typeof(BulkheadAttribute);

        // Assert
        type.GetProperty("MaxConcurrency").ShouldNotBeNull();
        type.GetProperty("MaxQueuedActions").ShouldNotBeNull();
        type.GetProperty("QueueTimeoutMs").ShouldNotBeNull();
    }

    #endregion
}
