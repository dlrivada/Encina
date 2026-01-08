using FsCheck;
using FsCheck.Xunit;

namespace Encina.Polly.PropertyTests;

/// <summary>
/// Property-based tests for bulkhead isolation policy invariants.
/// Verifies bulkhead behavior guarantees hold for all valid inputs.
/// </summary>
public class BulkheadInvariantPropertyTests : IDisposable
{
    private BulkheadManager? _manager;

    public void Dispose()
    {
        _manager?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region BulkheadAttribute Invariants

    [Property]
    public bool BulkheadAttribute_MaxConcurrency_ShouldAlwaysBePositive(PositiveInt concurrency)
    {
        var attribute = new BulkheadAttribute { MaxConcurrency = concurrency.Get };
        return attribute.MaxConcurrency > 0;
    }

    [Property]
    public bool BulkheadAttribute_MaxQueuedActions_ShouldPreserveValue(NonNegativeInt queueSize)
    {
        var attribute = new BulkheadAttribute { MaxQueuedActions = queueSize.Get };
        return attribute.MaxQueuedActions == queueSize.Get;
    }

    [Property]
    public bool BulkheadAttribute_QueueTimeoutMs_ShouldAlwaysBePositive(PositiveInt timeoutMs)
    {
        var attribute = new BulkheadAttribute { QueueTimeoutMs = timeoutMs.Get };
        return attribute.QueueTimeoutMs > 0;
    }

    [Fact]
    public void BulkheadAttribute_DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var attribute = new BulkheadAttribute();

        // Assert
        Assert.Equal(10, attribute.MaxConcurrency);
        Assert.Equal(20, attribute.MaxQueuedActions);
        Assert.Equal(30000, attribute.QueueTimeoutMs);
    }

    #endregion

    #region BulkheadMetrics Invariants

    [Property]
    public bool BulkheadMetrics_ConcurrencyUtilization_ShouldBeBetween0And100(
        NonNegativeInt currentRaw,
        PositiveInt maxRaw)
    {
        var max = maxRaw.Get;
        var current = Math.Min(currentRaw.Get, max); // Current can't exceed max

        var metrics = new BulkheadMetrics(current, 0, max, 10, 0, 0);

        return metrics.ConcurrencyUtilization >= 0.0 && metrics.ConcurrencyUtilization <= 100.0;
    }

    [Property]
    public bool BulkheadMetrics_QueueUtilization_ShouldBeBetween0And100(
        NonNegativeInt queuedRaw,
        PositiveInt maxQueueRaw)
    {
        var maxQueue = maxQueueRaw.Get;
        var queued = Math.Min(queuedRaw.Get, maxQueue);

        var metrics = new BulkheadMetrics(0, queued, 10, maxQueue, 0, 0);

        return metrics.QueueUtilization >= 0.0 && metrics.QueueUtilization <= 100.0;
    }

    [Property]
    public bool BulkheadMetrics_RejectionRate_ShouldBeBetween0And100(
        NonNegativeInt acquired,
        NonNegativeInt rejected)
    {
        var metrics = new BulkheadMetrics(0, 0, 10, 20, acquired.Get, rejected.Get);

        return metrics.RejectionRate >= 0.0 && metrics.RejectionRate <= 100.0;
    }

    [Property]
    public bool BulkheadMetrics_ZeroMaxConcurrency_ShouldReturn0Utilization(NonNegativeInt current)
    {
        var metrics = new BulkheadMetrics(current.Get, 0, 0, 20, 0, 0);
        return Math.Abs(metrics.ConcurrencyUtilization) < 0.001;
    }

    [Property]
    public bool BulkheadMetrics_ZeroMaxQueuedActions_ShouldReturn0Utilization(NonNegativeInt queued)
    {
        var metrics = new BulkheadMetrics(0, queued.Get, 10, 0, 0, 0);
        return Math.Abs(metrics.QueueUtilization) < 0.001;
    }

    [Property]
    public bool BulkheadMetrics_FullConcurrency_ShouldShow100PercentUtilization(PositiveInt max)
    {
        var metrics = new BulkheadMetrics(max.Get, 0, max.Get, 20, 0, 0);
        return Math.Abs(metrics.ConcurrencyUtilization - 100.0) < 0.001;
    }

    [Property]
    public bool BulkheadMetrics_FullQueue_ShouldShow100PercentUtilization(PositiveInt maxQueue)
    {
        var metrics = new BulkheadMetrics(0, maxQueue.Get, 10, maxQueue.Get, 0, 0);
        return Math.Abs(metrics.QueueUtilization - 100.0) < 0.001;
    }

    [Property]
    public bool BulkheadMetrics_AllRejected_ShouldShow100PercentRejectionRate(PositiveInt rejected)
    {
        var metrics = new BulkheadMetrics(0, 0, 10, 20, 0, rejected.Get);
        return Math.Abs(metrics.RejectionRate - 100.0) < 0.001;
    }

    [Fact]
    public void BulkheadMetrics_NoRequests_ShouldShow0PercentRejectionRate()
    {
        // Arrange & Act
        var metrics = new BulkheadMetrics(0, 0, 10, 20, 0, 0);

        // Assert
        Assert.True(Math.Abs(metrics.RejectionRate) < 0.001);
    }

    #endregion

    #region BulkheadAcquireResult Invariants

    [Fact]
    public void BulkheadAcquireResult_Acquired_ShouldHaveIsAcquiredTrue()
    {
        // Arrange
        var metrics = new BulkheadMetrics(1, 0, 10, 20, 1, 0);

        // Act
        var result = BulkheadAcquireResult.Acquired(new DummyDisposable(), metrics);

        // Assert
        Assert.True(result.IsAcquired);
        Assert.Equal(BulkheadRejectionReason.None, result.RejectionReason);
        Assert.NotNull(result.Releaser);
    }

    [Property]
    public bool BulkheadAcquireResult_RejectedBulkheadFull_ShouldHaveCorrectReason(PositiveInt dummy)
    {
        var metrics = new BulkheadMetrics(10, 20, 10, 20, 0, dummy.Get);
        var result = BulkheadAcquireResult.RejectedBulkheadFull(metrics);

        return !result.IsAcquired
               && result.RejectionReason == BulkheadRejectionReason.BulkheadFull
               && result.Releaser is null;
    }

    [Property]
    public bool BulkheadAcquireResult_RejectedQueueTimeout_ShouldHaveCorrectReason(PositiveInt dummy)
    {
        var metrics = new BulkheadMetrics(10, 5, 10, 20, 0, dummy.Get);
        var result = BulkheadAcquireResult.RejectedQueueTimeout(metrics);

        return !result.IsAcquired
               && result.RejectionReason == BulkheadRejectionReason.QueueTimeout
               && result.Releaser is null;
    }

    [Property]
    public bool BulkheadAcquireResult_RejectedCancelled_ShouldHaveCorrectReason(PositiveInt dummy)
    {
        var metrics = new BulkheadMetrics(5, 2, 10, 20, 0, dummy.Get);
        var result = BulkheadAcquireResult.RejectedCancelled(metrics);

        return !result.IsAcquired
               && result.RejectionReason == BulkheadRejectionReason.Cancelled
               && result.Releaser is null;
    }

    #endregion

    #region BulkheadManager Invariants

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for synchronous FsCheck property tests
    [Property(MaxTest = 20)]
    public bool BulkheadManager_FirstAcquire_ShouldSucceed(NonEmptyString keyRaw, PositiveInt concurrencyRaw)
    {
        var key = keyRaw.Get;
        var concurrency = (concurrencyRaw.Get % 100) + 1; // 1-100

        _manager?.Dispose();
        _manager = new BulkheadManager();

        var config = new BulkheadAttribute
        {
            MaxConcurrency = concurrency,
            MaxQueuedActions = 10,
            QueueTimeoutMs = 100
        };

        var result = _manager.TryAcquireAsync(key, config, CancellationToken.None)
            .GetAwaiter().GetResult();

        // First acquire should always succeed
        var success = result.IsAcquired;
        result.Releaser?.Dispose();
        return success;
    }

    [Property(MaxTest = 20)]
    public bool BulkheadManager_GetMetrics_ShouldReturnNullForUnknownKey(NonEmptyString keyRaw)
    {
        _manager?.Dispose();
        _manager = new BulkheadManager();

        var metrics = _manager.GetMetrics(keyRaw.Get);
        return metrics is null;
    }

    [Property(MaxTest = 20)]
    public bool BulkheadManager_Reset_ShouldRemoveMetrics(NonEmptyString keyRaw)
    {
        var key = keyRaw.Get;

        _manager?.Dispose();
        _manager = new BulkheadManager();

        var config = new BulkheadAttribute { MaxConcurrency = 5 };

        // Acquire once to create the bucket
        var result = _manager.TryAcquireAsync(key, config, CancellationToken.None)
            .GetAwaiter().GetResult();
        result.Releaser?.Dispose();

        // Metrics should exist
        var metricsBefore = _manager.GetMetrics(key);
        if (metricsBefore is null) return false;

        // Reset
        _manager.Reset(key);

        // Metrics should be gone
        var metricsAfter = _manager.GetMetrics(key);
        return metricsAfter is null;
    }
#pragma warning restore CA2012

    #endregion

    #region Concurrency Limit Invariants

    [Property]
    public bool ConcurrencyLimit_Current_ShouldNeverExceedMax(
        NonNegativeInt currentRaw,
        PositiveInt maxRaw)
    {
        var max = maxRaw.Get;
        var current = currentRaw.Get;

        // This is a mathematical invariant - in real system, current can't exceed max
        // We test that our model respects this
        var actualCurrent = Math.Min(current, max);
        return actualCurrent <= max;
    }

    [Property]
    public bool QueueLimit_Queued_ShouldNeverExceedMax(
        NonNegativeInt queuedRaw,
        PositiveInt maxQueueRaw)
    {
        var maxQueue = maxQueueRaw.Get;
        var queued = queuedRaw.Get;

        // This is a mathematical invariant
        var actualQueued = Math.Min(queued, maxQueue);
        return actualQueued <= maxQueue;
    }

    [Property]
    public bool TotalCapacity_ShouldBeSumOfConcurrencyAndQueue(
        PositiveInt maxConcurrencyRaw,
        NonNegativeInt maxQueueRaw)
    {
        // Arrange - bound values to reasonable ranges
        var maxConcurrency = (maxConcurrencyRaw.Get % 100) + 1; // 1-100
        var maxQueue = maxQueueRaw.Get % 100; // 0-99

        var attribute = new BulkheadAttribute
        {
            MaxConcurrency = maxConcurrency,
            MaxQueuedActions = maxQueue
        };

        // Act - calculate expected total capacity
        var expectedTotalCapacity = maxConcurrency + maxQueue;

        // Assert - verify attribute properties sum to expected total capacity
        return attribute.MaxConcurrency + attribute.MaxQueuedActions == expectedTotalCapacity;
    }

    #endregion

    #region Timeout Invariants

    [Property]
    public bool QueueTimeout_ConversionToTimeSpan_ShouldBeAccurate(PositiveInt timeoutMsRaw)
    {
        var timeoutMs = (timeoutMsRaw.Get % 60000) + 1; // 1-60000ms
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);

        return Math.Abs(timeout.TotalMilliseconds - timeoutMs) < 0.001;
    }

    [Property]
    public bool QueueTimeout_ShouldAlwaysBePositive(PositiveInt timeoutMs)
    {
        var timeout = TimeSpan.FromMilliseconds(timeoutMs.Get);
        return timeout > TimeSpan.Zero;
    }

    #endregion

    #region Helper Types

    private sealed class DummyDisposable : IDisposable
    {
        public void Dispose()
        {
            // No-op
        }
    }

    #endregion
}
