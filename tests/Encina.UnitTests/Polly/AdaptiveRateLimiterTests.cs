using Encina.Testing;
using Encina.Polly;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="AdaptiveRateLimiter"/>.
/// Tests rate limiting logic, adaptive throttling, state transitions, and recovery.
/// </summary>
public class AdaptiveRateLimiterTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly AdaptiveRateLimiter _rateLimiter;

    public AdaptiveRateLimiterTests()
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _rateLimiter = new AdaptiveRateLimiter(_timeProvider);
    }

    #region Basic Rate Limiting

    [Fact]
    public async Task AcquireAsync_WithinLimit_ShouldAllow()
    {
        // Arrange
        var config = new RateLimitAttribute { MaxRequestsPerWindow = 10, WindowSizeSeconds = 60 };

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.IsAllowed.ShouldBeTrue();
        result.CurrentState.ShouldBe(RateLimitState.Normal);
        result.CurrentCount.ShouldBe(1);
        result.CurrentLimit.ShouldBe(10);
    }

    [Fact]
    public async Task AcquireAsync_AtLimit_ShouldDeny()
    {
        // Arrange
        var config = new RateLimitAttribute { MaxRequestsPerWindow = 3, WindowSizeSeconds = 60 };

        // Act - Use up all permits
        await _rateLimiter.AcquireAsync("test", config);
        await _rateLimiter.AcquireAsync("test", config);
        await _rateLimiter.AcquireAsync("test", config);

        // Fourth request should be denied
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.IsAllowed.ShouldBeFalse();
        result.CurrentCount.ShouldBe(3);
        result.RetryAfter.ShouldNotBeNull();
    }

    [Fact]
    public async Task AcquireAsync_AfterWindowExpires_ShouldAllow()
    {
        // Arrange
        var config = new RateLimitAttribute { MaxRequestsPerWindow = 2, WindowSizeSeconds = 60 };

        // Use up all permits
        await _rateLimiter.AcquireAsync("test", config);
        await _rateLimiter.AcquireAsync("test", config);

        // Advance time past the window
        _timeProvider.Advance(TimeSpan.FromSeconds(61));

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.IsAllowed.ShouldBeTrue();
        result.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task AcquireAsync_SlidingWindow_ShouldExpireOldRequests()
    {
        // Arrange
        var config = new RateLimitAttribute { MaxRequestsPerWindow = 2, WindowSizeSeconds = 60 };

        // Make first request at T=0
        await _rateLimiter.AcquireAsync("test", config);

        // Advance 30 seconds and make second request
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        await _rateLimiter.AcquireAsync("test", config);

        // Third request should be denied
        var deniedResult = await _rateLimiter.AcquireAsync("test", config);
        deniedResult.IsAllowed.ShouldBeFalse();

        // Advance 31 seconds (first request should expire at T=60)
        _timeProvider.Advance(TimeSpan.FromSeconds(31));

        // Act - Now we should have room for one more
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireAsync_DifferentKeys_ShouldHaveSeparateLimits()
    {
        // Arrange
        var config = new RateLimitAttribute { MaxRequestsPerWindow = 1, WindowSizeSeconds = 60 };

        // Use up limit for key1
        await _rateLimiter.AcquireAsync("key1", config);
        var key1Result = await _rateLimiter.AcquireAsync("key1", config);

        // Act - key2 should still have its own limit
        var key2Result = await _rateLimiter.AcquireAsync("key2", config);

        // Assert
        key1Result.IsAllowed.ShouldBeFalse();
        key2Result.IsAllowed.ShouldBeTrue();
    }

    #endregion

    #region Adaptive Throttling

    [Fact]
    public async Task AcquireAsync_HighErrorRate_ShouldTransitionToThrottled()
    {
        // Arrange
        var config = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            ErrorThresholdPercent = 50.0,
            MinimumThroughputForThrottling = 10,
            EnableAdaptiveThrottling = true
        };

        // Simulate 10 requests with 60% failure rate (6 failures, 4 successes)
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            if (i < 6)
            {
                _rateLimiter.RecordFailure("test");
            }
            else
            {
                _rateLimiter.RecordSuccess("test");
            }
        }

        // Act - Next acquire should detect high error rate
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.CurrentState.ShouldBe(RateLimitState.Throttled);
        result.CurrentLimit.ShouldBeLessThan(100, "capacity should be reduced in throttled state");
    }

    [Fact]
    public async Task AcquireAsync_BelowErrorThreshold_ShouldStayNormal()
    {
        // Arrange
        var config = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            ErrorThresholdPercent = 50.0,
            MinimumThroughputForThrottling = 10,
            EnableAdaptiveThrottling = true
        };

        // Simulate 10 requests with 30% failure rate (3 failures, 7 successes)
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            if (i < 3)
            {
                _rateLimiter.RecordFailure("test");
            }
            else
            {
                _rateLimiter.RecordSuccess("test");
            }
        }

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.CurrentState.ShouldBe(RateLimitState.Normal);
        result.CurrentLimit.ShouldBe(100);
    }

    [Fact]
    public async Task AcquireAsync_AdaptiveDisabled_ShouldNotThrottle()
    {
        // Arrange
        var config = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            ErrorThresholdPercent = 50.0,
            MinimumThroughputForThrottling = 10,
            EnableAdaptiveThrottling = false // Disabled!
        };

        // Simulate 100% failure rate
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            _rateLimiter.RecordFailure("test");
        }

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.CurrentState.ShouldBe(RateLimitState.Normal, "adaptive throttling is disabled");
        result.CurrentLimit.ShouldBe(100);
    }

    [Fact]
    public async Task AcquireAsync_BelowMinimumThroughput_ShouldNotThrottle()
    {
        // Arrange
        var config = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            ErrorThresholdPercent = 50.0,
            MinimumThroughputForThrottling = 10,
            EnableAdaptiveThrottling = true
        };

        // Simulate only 5 requests with 100% failure rate (below minimum)
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            _rateLimiter.RecordFailure("test");
        }

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.CurrentState.ShouldBe(RateLimitState.Normal, "below minimum throughput for throttling");
    }

    #endregion

    #region Recovery

    [Fact]
    public async Task AcquireAsync_AfterCooldown_ShouldTransitionToRecovering()
    {
        // Arrange
        var config = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            ErrorThresholdPercent = 50.0,
            MinimumThroughputForThrottling = 10,
            CooldownSeconds = 60,
            EnableAdaptiveThrottling = true
        };

        // Force into throttled state
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            _rateLimiter.RecordFailure("test");
        }

        // Verify throttled
        var throttledResult = await _rateLimiter.AcquireAsync("test", config);
        throttledResult.CurrentState.ShouldBe(RateLimitState.Throttled);

        // Advance past cooldown
        _timeProvider.Advance(TimeSpan.FromSeconds(61));

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.CurrentState.ShouldBe(RateLimitState.Recovering);
    }

    [Fact]
    public async Task AcquireAsync_RecoveringWithSuccesses_ShouldRampUp()
    {
        // Arrange
        var config = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            ErrorThresholdPercent = 50.0,
            MinimumThroughputForThrottling = 10,
            CooldownSeconds = 30,
            RampUpFactor = 2.0,
            EnableAdaptiveThrottling = true
        };

        // Force into throttled state
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            _rateLimiter.RecordFailure("test");
        }
        await _rateLimiter.AcquireAsync("test", config); // Trigger state check

        // Move to recovering
        _timeProvider.Advance(TimeSpan.FromSeconds(31));
        var recoveringResult = await _rateLimiter.AcquireAsync("test", config);
        var initialLimit = recoveringResult.CurrentLimit;

        // Record successes and advance time for ramp-up
        _rateLimiter.RecordSuccess("test");
        _timeProvider.Advance(TimeSpan.FromSeconds(61)); // Past window size

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.CurrentLimit.ShouldBeGreaterThan(initialLimit, "capacity should increase during recovery");
    }

    [Fact]
    public async Task AcquireAsync_RecoveringWithErrors_ShouldRevertToThrottled()
    {
        // Arrange
        var config = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100,
            WindowSizeSeconds = 60,
            ErrorThresholdPercent = 50.0,
            MinimumThroughputForThrottling = 5,
            CooldownSeconds = 30,
            EnableAdaptiveThrottling = true
        };

        // Force into throttled state
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            _rateLimiter.RecordFailure("test");
        }
        await _rateLimiter.AcquireAsync("test", config);

        // Move to recovering
        _timeProvider.Advance(TimeSpan.FromSeconds(31));
        await _rateLimiter.AcquireAsync("test", config);

        // Simulate more failures during recovery
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.AcquireAsync("test", config);
            _rateLimiter.RecordFailure("test");
        }

        // Act
        var result = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        result.CurrentState.ShouldBe(RateLimitState.Throttled, "should revert to throttled on errors during recovery");
    }

    #endregion

    #region State Management

    [Fact]
    public void GetState_UnknownKey_ShouldReturnNull()
    {
        // Act
        var state = _rateLimiter.GetState("unknown-key");

        // Assert
        state.ShouldBeNull();
    }

    [Fact]
    public async Task GetState_KnownKey_ShouldReturnCurrentState()
    {
        // Arrange
        var config = new RateLimitAttribute { MaxRequestsPerWindow = 10, WindowSizeSeconds = 60 };
        await _rateLimiter.AcquireAsync("test", config);

        // Act
        var state = _rateLimiter.GetState("test");

        // Assert
        state.ShouldBe(RateLimitState.Normal);
    }

    [Fact]
    public async Task Reset_ShouldClearState()
    {
        // Arrange
        var config = new RateLimitAttribute { MaxRequestsPerWindow = 2, WindowSizeSeconds = 60 };
        await _rateLimiter.AcquireAsync("test", config);
        await _rateLimiter.AcquireAsync("test", config);

        // Verify at limit
        var atLimitResult = await _rateLimiter.AcquireAsync("test", config);
        atLimitResult.IsAllowed.ShouldBeFalse();

        // Act
        _rateLimiter.Reset("test");
        var afterResetResult = await _rateLimiter.AcquireAsync("test", config);

        // Assert
        afterResetResult.IsAllowed.ShouldBeTrue();
        afterResetResult.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void RecordSuccess_UnknownKey_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _rateLimiter.RecordSuccess("unknown-key");
        Should.NotThrow(action);
    }

    [Fact]
    public void RecordFailure_UnknownKey_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _rateLimiter.RecordFailure("unknown-key");
        Should.NotThrow(action);
    }

    #endregion

    #region RateLimitResult Factory Methods

    [Fact]
    public void RateLimitResult_Allowed_ShouldCreateCorrectResult()
    {
        // Act
        var result = RateLimitResult.Allowed(
            RateLimitState.Normal,
            currentCount: 5,
            currentLimit: 100,
            errorRate: 10.0);

        // Assert
        result.IsAllowed.ShouldBeTrue();
        result.CurrentState.ShouldBe(RateLimitState.Normal);
        result.CurrentCount.ShouldBe(5);
        result.CurrentLimit.ShouldBe(100);
        result.ErrorRate.ShouldBe(10.0);
        result.RetryAfter.ShouldBeNull();
    }

    [Fact]
    public void RateLimitResult_Denied_ShouldCreateCorrectResult()
    {
        // Act
        var result = RateLimitResult.Denied(
            RateLimitState.Throttled,
            retryAfter: TimeSpan.FromSeconds(30),
            currentCount: 10,
            currentLimit: 10,
            errorRate: 60.0);

        // Assert
        result.IsAllowed.ShouldBeFalse();
        result.CurrentState.ShouldBe(RateLimitState.Throttled);
        result.CurrentCount.ShouldBe(10);
        result.CurrentLimit.ShouldBe(10);
        result.ErrorRate.ShouldBe(60.0);
        result.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }

    #endregion
}
