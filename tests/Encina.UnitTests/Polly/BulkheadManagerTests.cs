using Encina.Polly;
using Encina.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="BulkheadManager"/>.
/// Tests concurrency limiting, queueing, and metrics.
/// </summary>
public class BulkheadManagerTests : IDisposable
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly BulkheadManager _manager;

    public BulkheadManagerTests()
    {
        _timeProvider = new FakeTimeProvider();
        _manager = new BulkheadManager(_timeProvider);
    }

    public void Dispose()
    {
        _manager.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Basic Acquisition Tests

    [Fact]
    public async Task TryAcquireAsync_FirstRequest_ShouldSucceed()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 10 };

        // Act
        var result = await _manager.TryAcquireAsync("test", config);

        // Assert
        result.IsAcquired.ShouldBeTrue();
        result.RejectionReason.ShouldBe(BulkheadRejectionReason.None);
        result.Releaser.ShouldNotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_MultipleWithinLimit_ShouldAllSucceed()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 5 };
        var results = new List<BulkheadAcquireResult>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            results.Add(await _manager.TryAcquireAsync("test", config));
        }

        // Assert
        results.ShouldAllBe(r => r.IsAcquired);
    }

    [Fact]
    public async Task TryAcquireAsync_ExceedsConcurrencyAndQueue_ShouldReject()
    {
        // Arrange
        var config = new BulkheadAttribute
        {
            MaxConcurrency = 2,
            MaxQueuedActions = 0, // No queue
            QueueTimeoutMs = 100
        };

        // Acquire all permits
        var permit1 = await _manager.TryAcquireAsync("test", config);
        var permit2 = await _manager.TryAcquireAsync("test", config);

        // Act - try to acquire one more
        var result = await _manager.TryAcquireAsync("test", config);

        // Assert
        permit1.IsAcquired.ShouldBeTrue();
        permit2.IsAcquired.ShouldBeTrue();
        result.IsAcquired.ShouldBeFalse();
        result.RejectionReason.ShouldBe(BulkheadRejectionReason.BulkheadFull);

        // Cleanup
        permit1.Releaser?.Dispose();
        permit2.Releaser?.Dispose();
    }

    #endregion

    #region Release Tests

    [Fact]
    public async Task TryAcquireAsync_AfterRelease_ShouldSucceed()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 1, MaxQueuedActions = 0 };

        // Acquire and release
        var permit1 = await _manager.TryAcquireAsync("test", config);
        permit1.Releaser?.Dispose();

        // Act
        var result = await _manager.TryAcquireAsync("test", config);

        // Assert
        result.IsAcquired.ShouldBeTrue();
        result.Releaser?.Dispose();
    }

    [Fact]
    public async Task Releaser_DisposedTwice_ShouldNotThrow()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 10 };
        var permit = await _manager.TryAcquireAsync("test", config);

        // Act & Assert
        var action = () =>
        {
            permit.Releaser?.Dispose();
            permit.Releaser?.Dispose();
        };

        Should.NotThrow(action);
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public async Task GetMetrics_AfterAcquisitions_ShouldReflectState()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 10, MaxQueuedActions = 20 };

        // Acquire some permits
        var permits = new List<BulkheadAcquireResult>();
        for (int i = 0; i < 3; i++)
        {
            permits.Add(await _manager.TryAcquireAsync("test", config));
        }

        // Act
        var metrics = _manager.GetMetrics("test");

        // Assert
        metrics.ShouldNotBeNull();
        metrics!.Value.CurrentConcurrency.ShouldBe(3);
        metrics.Value.MaxConcurrency.ShouldBe(10);
        metrics.Value.TotalAcquired.ShouldBe(3);
        metrics.Value.TotalRejected.ShouldBe(0);
        metrics.Value.ConcurrencyUtilization.ShouldBe(30.0);

        // Cleanup
        foreach (var permit in permits)
        {
            permit.Releaser?.Dispose();
        }
    }

    [Fact]
    public async Task GetMetrics_AfterRejections_ShouldTrackRejectedCount()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 1, MaxQueuedActions = 0 };

        // Acquire the only permit
        var permit = await _manager.TryAcquireAsync("test", config);

        // Try to acquire more (should be rejected)
        await _manager.TryAcquireAsync("test", config);
        await _manager.TryAcquireAsync("test", config);

        // Act
        var metrics = _manager.GetMetrics("test");

        // Assert
        metrics.ShouldNotBeNull();
        metrics!.Value.TotalAcquired.ShouldBe(1);
        metrics.Value.TotalRejected.ShouldBe(2);
        metrics.Value.RejectionRate.ShouldBe(66.67, 0.1);

        // Cleanup
        permit.Releaser?.Dispose();
    }

    [Fact]
    public void GetMetrics_NonExistentKey_ShouldReturnNull()
    {
        // Act
        var metrics = _manager.GetMetrics("nonexistent");

        // Assert
        metrics.ShouldBeNull();
    }

    #endregion

    #region Independent Keys Tests

    [Fact]
    public async Task TryAcquireAsync_DifferentKeys_ShouldBeIndependent()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 1, MaxQueuedActions = 0 };

        // Acquire permit for key1
        var permit1 = await _manager.TryAcquireAsync("key1", config);

        // Act - try to acquire for key2
        var result = await _manager.TryAcquireAsync("key2", config);

        // Assert
        permit1.IsAcquired.ShouldBeTrue();
        result.IsAcquired.ShouldBeTrue();

        // Cleanup
        permit1.Releaser?.Dispose();
        result.Releaser?.Dispose();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_ShouldClearBulkhead()
    {
        // Arrange
        var config = new BulkheadAttribute { MaxConcurrency = 10 };
        await _manager.TryAcquireAsync("test", config);

        // Act
        _manager.Reset("test");
        var metrics = _manager.GetMetrics("test");

        // Assert
        metrics.ShouldBeNull();
    }

    [Fact]
    public void Reset_NonExistentKey_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _manager.Reset("nonexistent");
        Should.NotThrow(action);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task TryAcquireAsync_Cancelled_ShouldReturnCancelled()
    {
        // Arrange
        var config = new BulkheadAttribute
        {
            MaxConcurrency = 1,
            MaxQueuedActions = 10,
            QueueTimeoutMs = 10000
        };

        // Acquire the only permit
        var permit = await _manager.TryAcquireAsync("test", config);

        // Create a cancellation token that's already cancelled
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _manager.TryAcquireAsync("test", config, cts.Token);

        // Assert
        result.IsAcquired.ShouldBeFalse();
        result.RejectionReason.ShouldBe(BulkheadRejectionReason.Cancelled);

        // Cleanup
        permit.Releaser?.Dispose();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_ShouldPreventFurtherAcquisitions()
    {
        // Arrange
        var manager = new BulkheadManager();
        var config = new BulkheadAttribute { MaxConcurrency = 10 };

        // Act
        manager.Dispose();

        // Assert
        await Should.ThrowAsync<ObjectDisposedException>(async () => await manager.TryAcquireAsync("test", config));
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var manager = new BulkheadManager();

        // Act & Assert
        var action = () =>
        {
            manager.Dispose();
            manager.Dispose();
        };

        Should.NotThrow(action);
    }

    #endregion

    #region BulkheadMetrics Tests

    [Fact]
    public void BulkheadMetrics_ConcurrencyUtilization_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = new BulkheadMetrics(5, 0, 10, 20, 100, 10);

        // Act & Assert
        metrics.ConcurrencyUtilization.ShouldBe(50.0);
    }

    [Fact]
    public void BulkheadMetrics_QueueUtilization_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = new BulkheadMetrics(10, 5, 10, 20, 100, 10);

        // Act & Assert
        metrics.QueueUtilization.ShouldBe(25.0);
    }

    [Fact]
    public void BulkheadMetrics_RejectionRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var metrics = new BulkheadMetrics(0, 0, 10, 20, 90, 10);

        // Act & Assert
        metrics.RejectionRate.ShouldBe(10.0);
    }

    [Fact]
    public void BulkheadMetrics_ZeroMaxConcurrency_ShouldReturnZeroUtilization()
    {
        // Arrange
        var metrics = new BulkheadMetrics(0, 0, 0, 0, 0, 0);

        // Act & Assert
        metrics.ConcurrencyUtilization.ShouldBe(0.0);
        metrics.QueueUtilization.ShouldBe(0.0);
        metrics.RejectionRate.ShouldBe(0.0);
    }

    #endregion

    #region BulkheadAcquireResult Tests

    [Fact]
    public void BulkheadAcquireResult_Acquired_ShouldCreateCorrectly()
    {
        // Arrange
        var releaser = Substitute.For<IDisposable>();
        var metrics = new BulkheadMetrics(1, 0, 10, 20, 1, 0);

        // Act
        var result = BulkheadAcquireResult.Acquired(releaser, metrics);

        // Assert
        result.IsAcquired.ShouldBeTrue();
        result.RejectionReason.ShouldBe(BulkheadRejectionReason.None);
        result.Releaser.ShouldBe(releaser);
    }

    [Fact]
    public void BulkheadAcquireResult_RejectedBulkheadFull_ShouldCreateCorrectly()
    {
        // Arrange
        var metrics = new BulkheadMetrics(10, 20, 10, 20, 100, 50);

        // Act
        var result = BulkheadAcquireResult.RejectedBulkheadFull(metrics);

        // Assert
        result.IsAcquired.ShouldBeFalse();
        result.RejectionReason.ShouldBe(BulkheadRejectionReason.BulkheadFull);
        result.Releaser.ShouldBeNull();
    }

    [Fact]
    public void BulkheadAcquireResult_RejectedQueueTimeout_ShouldCreateCorrectly()
    {
        // Arrange
        var metrics = new BulkheadMetrics(10, 20, 10, 20, 100, 50);

        // Act
        var result = BulkheadAcquireResult.RejectedQueueTimeout(metrics);

        // Assert
        result.IsAcquired.ShouldBeFalse();
        result.RejectionReason.ShouldBe(BulkheadRejectionReason.QueueTimeout);
    }

    [Fact]
    public void BulkheadAcquireResult_RejectedCancelled_ShouldCreateCorrectly()
    {
        // Arrange
        var metrics = new BulkheadMetrics(10, 20, 10, 20, 100, 50);

        // Act
        var result = BulkheadAcquireResult.RejectedCancelled(metrics);

        // Assert
        result.IsAcquired.ShouldBeFalse();
        result.RejectionReason.ShouldBe(BulkheadRejectionReason.Cancelled);
    }

    #endregion
}
