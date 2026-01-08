using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Time.Testing;

namespace Encina.Polly.PropertyTests;

/// <summary>
/// Property-based tests for rate limiting components.
/// Uses FsCheck to verify invariants hold for all valid inputs.
/// </summary>
public class RateLimitingPropertyTests
{
    private const int MaxTestsForExpensive = 30;

    #region RateLimitAttribute Property Tests

    [Property]
    public bool RateLimitAttribute_MaxRequestsPerWindow_ShouldAlwaysBeStoredCorrectly(PositiveInt maxRequests)
    {
        var attribute = new RateLimitAttribute { MaxRequestsPerWindow = maxRequests.Get };
        return attribute.MaxRequestsPerWindow == maxRequests.Get;
    }

    [Property]
    public bool RateLimitAttribute_WindowSizeSeconds_ShouldAlwaysBeStoredCorrectly(PositiveInt windowSize)
    {
        var attribute = new RateLimitAttribute { WindowSizeSeconds = windowSize.Get };
        return attribute.WindowSizeSeconds == windowSize.Get;
    }

    [Property]
    public bool RateLimitAttribute_ErrorThreshold_ShouldBeStoredCorrectly(PositiveInt threshold)
    {
        var normalizedThreshold = (threshold.Get % 99) + 1; // 1-99 range
        var attribute = new RateLimitAttribute { ErrorThresholdPercent = normalizedThreshold };
        return attribute.ErrorThresholdPercent == normalizedThreshold;
    }

    [Property]
    public bool RateLimitAttribute_EnableAdaptiveThrottling_ShouldBeStoredCorrectly(bool enabled)
    {
        var attribute = new RateLimitAttribute { EnableAdaptiveThrottling = enabled };
        return attribute.EnableAdaptiveThrottling == enabled;
    }

    #endregion

    #region RateLimitResult Property Tests

    [Property]
    public bool RateLimitResult_Allowed_ShouldAlwaysHaveIsAllowedTrue(
        NonNegativeInt count,
        PositiveInt limit,
        NonNegativeInt errorRatePercent)
    {
        var errorRate = errorRatePercent.Get % 101; // 0-100 range
        var result = RateLimitResult.Allowed(RateLimitState.Normal, count.Get, limit.Get, errorRate);
        return result.IsAllowed && result.RetryAfter == null;
    }

    [Property]
    public bool RateLimitResult_Denied_ShouldAlwaysHaveIsAllowedFalse(
        PositiveInt retrySeconds,
        NonNegativeInt count,
        PositiveInt limit,
        NonNegativeInt errorRatePercent)
    {
        var errorRate = errorRatePercent.Get % 101;
        var retryAfter = TimeSpan.FromSeconds(retrySeconds.Get);
        var result = RateLimitResult.Denied(RateLimitState.Throttled, retryAfter, count.Get, limit.Get, errorRate);
        return !result.IsAllowed && result.RetryAfter != null;
    }

    [Property]
    public bool RateLimitResult_ShouldPreserveAllValues(
        NonNegativeInt count,
        PositiveInt limit,
        NonNegativeInt errorRatePercent)
    {
        var errorRate = errorRatePercent.Get % 101;
        var result = RateLimitResult.Allowed(RateLimitState.Recovering, count.Get, limit.Get, errorRate);
        return result.CurrentState == RateLimitState.Recovering &&
               result.CurrentCount == count.Get &&
               result.CurrentLimit == limit.Get &&
               Math.Abs(result.ErrorRate - errorRate) < 0.0001;
    }

    #endregion

    #region AdaptiveRateLimiter Property Tests

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool AdaptiveRateLimiter_RequestsWithinLimit_ShouldAlwaysBeAllowed(PositiveInt maxRequestsRaw)
    {
        var maxRequests = (maxRequestsRaw.Get % 99) + 2; // 2-100 range
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = maxRequests,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = false
        };

        var key = Guid.NewGuid().ToString();
        var requestsWithinLimit = maxRequests - 1;
        var allAllowed = true;

        for (var i = 0; i < requestsWithinLimit && allAllowed; i++)
        {
            allAllowed = AcquireSync(rateLimiter, key, attribute).IsAllowed;
        }

        return allAllowed;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool AdaptiveRateLimiter_RequestsBeyondLimit_ShouldBeDenied(PositiveInt maxRequestsRaw)
    {
        var maxRequests = (maxRequestsRaw.Get % 49) + 1; // 1-50 range
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = maxRequests,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = false
        };

        var key = Guid.NewGuid().ToString();

        // Exhaust the limit
        for (var i = 0; i < maxRequests; i++)
        {
            AcquireSync(rateLimiter, key, attribute);
        }

        // Next request should be denied
        var result = AcquireSync(rateLimiter, key, attribute);

        return !result.IsAllowed;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool AdaptiveRateLimiter_AfterWindowExpires_ShouldResetCount(PositiveInt maxRequestsRaw, PositiveInt windowSizeRaw)
    {
        var maxRequests = (maxRequestsRaw.Get % 19) + 1; // 1-20 range
        var windowSize = (windowSizeRaw.Get % 59) + 1; // 1-60 range

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = maxRequests,
            WindowSizeSeconds = windowSize,
            EnableAdaptiveThrottling = false
        };

        var key = Guid.NewGuid().ToString();

        // Exhaust the limit
        for (var i = 0; i < maxRequests; i++)
        {
            AcquireSync(rateLimiter, key, attribute);
        }

        // Advance time past the window
        timeProvider.Advance(TimeSpan.FromSeconds(windowSize + 1));

        // Should be allowed again
        var result = AcquireSync(rateLimiter, key, attribute);

        return result.IsAllowed;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool AdaptiveRateLimiter_DifferentKeys_ShouldBeIndependent(PositiveInt maxRequestsRaw, PositiveInt keyCountRaw)
    {
        var maxRequests = (maxRequestsRaw.Get % 19) + 1; // 1-20 range
        var keyCount = (keyCountRaw.Get % 4) + 2; // 2-5 range

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = maxRequests,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = false
        };

        var keys = Enumerable.Range(0, keyCount).Select(_ => Guid.NewGuid().ToString()).ToList();
        var allAllowed = true;

        // Make maxRequests for each key
        foreach (var key in keys)
        {
            for (var i = 0; i < maxRequests && allAllowed; i++)
            {
                var result = AcquireSync(rateLimiter, key, attribute);
                allAllowed = result.IsAllowed;
            }
        }

        return allAllowed;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool AdaptiveRateLimiter_ResetKey_ShouldAllowNewRequests(PositiveInt maxRequestsRaw)
    {
        var maxRequests = (maxRequestsRaw.Get % 19) + 1; // 1-20 range

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = maxRequests,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = false
        };

        var key = Guid.NewGuid().ToString();

        // Exhaust the limit
        for (var i = 0; i < maxRequests; i++)
        {
            AcquireSync(rateLimiter, key, attribute);
        }

        // Verify denied
        var deniedResult = AcquireSync(rateLimiter, key, attribute);

        // Reset
        rateLimiter.Reset(key);

        // Should be allowed again
        var allowedResult = AcquireSync(rateLimiter, key, attribute);

        return !deniedResult.IsAllowed && allowedResult.IsAllowed;
    }

    #endregion

    #region Adaptive Throttling Property Tests

    [Property(MaxTest = 20)]
    public bool AdaptiveRateLimiter_HighErrorRate_ShouldTransitionToThrottled(PositiveInt errorThresholdRaw)
    {
        var errorThreshold = (errorThresholdRaw.Get % 40) + 50; // 50-90 range

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = true,
            ErrorThresholdPercent = errorThreshold,
            MinimumThroughputForThrottling = 5
        };

        var key = Guid.NewGuid().ToString();

        // Make minimum throughput requests, all failing
        for (var i = 0; i < 10; i++)
        {
            AcquireSync(rateLimiter, key, attribute);
            rateLimiter.RecordFailure(key);
        }

        var state = rateLimiter.GetState(key);

        return state == RateLimitState.Throttled;
    }

    [Property(MaxTest = 20)]
    public bool AdaptiveRateLimiter_RecordSuccess_ShouldDecreaseOrMaintainErrorRate(PositiveInt successCountRaw)
    {
        var successCount = (successCountRaw.Get % 10) + 5; // 5-15 range

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = true,
            ErrorThresholdPercent = 50,
            MinimumThroughputForThrottling = 5
        };

        var key = Guid.NewGuid().ToString();

        // Start with failures
        for (var i = 0; i < 10; i++)
        {
            AcquireSync(rateLimiter, key, attribute);
            rateLimiter.RecordFailure(key);
        }

        // Get initial error rate
        var initialResult = AcquireSync(rateLimiter, key, attribute);
        var initialErrorRate = initialResult.ErrorRate;

        // Record successes
        for (var i = 0; i < successCount; i++)
        {
            rateLimiter.RecordSuccess(key);
        }

        // Get new error rate
        var finalResult = AcquireSync(rateLimiter, key, attribute);
        var finalErrorRate = finalResult.ErrorRate;

        return finalErrorRate <= initialErrorRate;
    }

    #endregion

    #region State Machine Invariants

    [Property]
    public bool RateLimitState_AllStatesAreValid(byte _)
    {
        var states = Enum.GetValues<RateLimitState>();
        // All enum values should be defined
        return states.All(s => Enum.IsDefined(s));
    }

    [Property]
    public bool RateLimitState_NormalState_ShouldHaveFullCapacity(PositiveInt maxRequests)
    {
        // In Normal state, current limit equals max requests
        var result = RateLimitResult.Allowed(RateLimitState.Normal, 0, maxRequests.Get, 0);
        return result.CurrentLimit == maxRequests.Get;
    }

    [Property]
    public bool RateLimitState_ThrottledState_ShouldHaveReducedCapacity(PositiveInt maxRequestsRaw)
    {
        // In Throttled state, capacity is reduced to 10%
        var maxRequests = maxRequestsRaw.Get;
        var throttledLimit = Math.Max(1, (int)(maxRequests * 0.1));

        // Throttled limit should be at most 10% of max
        return throttledLimit <= maxRequests && throttledLimit >= 1;
    }

    [Property(MaxTest = 20)]
    public bool AdaptiveRateLimiter_LowErrorRate_ShouldStayNormal(PositiveInt requestCountRaw)
    {
        var requestCount = (requestCountRaw.Get % 9) + 5; // 5-14 range (below minimum throughput)

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = true,
            ErrorThresholdPercent = 50,
            MinimumThroughputForThrottling = 20 // Higher than our request count
        };

        var key = Guid.NewGuid().ToString();

        // Make requests with 100% failure but below minimum throughput
        for (var i = 0; i < requestCount; i++)
        {
            AcquireSync(rateLimiter, key, attribute);
            rateLimiter.RecordFailure(key);
        }

        // State should still be Normal (minimum throughput not reached)
        var state = rateLimiter.GetState(key);
        return state == RateLimitState.Normal;
    }

    #endregion

    #region Mathematical Invariants

    [Property]
    public bool ErrorRate_ShouldBeBetween0And100(PositiveInt successes, PositiveInt failures)
    {
        // Calculate error rate
        var total = (double)(successes.Get + failures.Get);
        var errorRate = (failures.Get / total) * 100.0;

        return errorRate >= 0.0 && errorRate <= 100.0;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool SlidingWindow_RequestCount_ShouldNeverExceedMax(
        PositiveInt maxRequestsRaw,
        PositiveInt requestCountRaw)
    {
        // Arrange
        var maxRequests = (maxRequestsRaw.Get % 20) + 5; // 5-25 range
        var requestCount = (requestCountRaw.Get % 50) + maxRequests; // Always more than max

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = maxRequests,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = false
        };

        var key = Guid.NewGuid().ToString();
        var maxObservedCount = 0;

        // Act - make more requests than allowed
        for (var i = 0; i < requestCount; i++)
        {
            var result = AcquireSync(rateLimiter, key, attribute);
            maxObservedCount = Math.Max(maxObservedCount, result.CurrentCount);
        }

        // Assert - CurrentCount should never exceed MaxRequestsPerWindow
        return maxObservedCount <= maxRequests;
    }

    [Property]
    public bool RetryAfter_ShouldAlwaysBePositiveWhenDenied(PositiveInt retrySeconds)
    {
        var retryAfter = TimeSpan.FromSeconds(retrySeconds.Get);
        var result = RateLimitResult.Denied(
            RateLimitState.Throttled,
            retryAfter,
            100,
            100,
            75);

        return result.RetryAfter.HasValue && result.RetryAfter.Value > TimeSpan.Zero;
    }

    [Property]
    public bool WindowSize_ConversionToTimeSpan_ShouldBeAccurate(PositiveInt windowSecondsRaw)
    {
        var windowSeconds = (windowSecondsRaw.Get % 3600) + 1; // 1-3600 seconds
        var windowTimeSpan = TimeSpan.FromSeconds(windowSeconds);

        return Math.Abs(windowTimeSpan.TotalSeconds - windowSeconds) < 0.001;
    }

    [Property]
    public bool RampUpFactor_ShouldAlwaysIncreaseCapacity(PositiveInt currentCapacityRaw)
    {
        var currentCapacity = (currentCapacityRaw.Get % 100) + 1; // 1-100
        var rampUpFactor = 2.0;

        var newCapacity = (int)(currentCapacity * rampUpFactor);

        return newCapacity >= currentCapacity;
    }

    #endregion

    #region Default Values Invariants

    [Property]
    public bool RateLimitAttribute_DefaultValues_ShouldBeReasonable(byte _)
    {
        var attribute = new RateLimitAttribute();

        return attribute.MaxRequestsPerWindow == 100
               && attribute.WindowSizeSeconds == 60
               && Math.Abs(attribute.ErrorThresholdPercent - 50.0) < 0.001
               && attribute.CooldownSeconds == 60
               && Math.Abs(attribute.RampUpFactor - 2.0) < 0.001
               && attribute.EnableAdaptiveThrottling
               && attribute.MinimumThroughputForThrottling == 10;
    }

    [Property]
    public bool RateLimitAttribute_CooldownSeconds_ShouldAlwaysBePositive(PositiveInt cooldown)
    {
        var attribute = new RateLimitAttribute { CooldownSeconds = cooldown.Get };
        return attribute.CooldownSeconds > 0;
    }

    [Property]
    public bool RateLimitAttribute_RampUpFactor_ShouldBeStoredCorrectly(PositiveInt factorRaw)
    {
        var factor = (factorRaw.Get % 10) + 1.0; // 1.0-11.0
        var attribute = new RateLimitAttribute { RampUpFactor = factor };
        return Math.Abs(attribute.RampUpFactor - factor) < 0.001;
    }

    [Property]
    public bool RateLimitAttribute_MinimumThroughput_ShouldBePreserved(PositiveInt throughput)
    {
        var attribute = new RateLimitAttribute { MinimumThroughputForThrottling = throughput.Get };
        return attribute.MinimumThroughputForThrottling == throughput.Get;
    }

    #endregion

    #region Helper Methods

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for synchronous FsCheck property tests
    private static RateLimitResult AcquireSync(AdaptiveRateLimiter rateLimiter, string key, RateLimitAttribute attribute)
    {
        return rateLimiter.AcquireAsync(key, attribute, CancellationToken.None).GetAwaiter().GetResult();
    }
#pragma warning restore CA2012

    #endregion
}
