using Encina.Polly;
using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="BulkheadPipelineBehavior{TRequest, TResponse}"/>.
/// Tests bulkhead isolation, rejection handling, and error messages.
/// </summary>
public class BulkheadPipelineBehaviorTests
{
    private readonly ILogger<BulkheadPipelineBehavior<TestBulkheadRequest, string>> _logger;
    private readonly IBulkheadManager _bulkheadManager;
    private readonly BulkheadPipelineBehavior<TestBulkheadRequest, string> _behavior;

    public BulkheadPipelineBehaviorTests()
    {
        _logger = Substitute.For<ILogger<BulkheadPipelineBehavior<TestBulkheadRequest, string>>>();
        _bulkheadManager = Substitute.For<IBulkheadManager>();
        _behavior = new BulkheadPipelineBehavior<TestBulkheadRequest, string>(_logger, _bulkheadManager);
    }

    #region Pass-Through Tests

    [Fact]
    public async Task Handle_NoBulkheadAttribute_ShouldPassThrough()
    {
        // Arrange
        var request = new TestRequestNoBulkhead();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

        var behavior = new BulkheadPipelineBehavior<TestRequestNoBulkhead, string>(
            Substitute.For<ILogger<BulkheadPipelineBehavior<TestRequestNoBulkhead, string>>>(),
            _bulkheadManager);

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_NoBulkheadAttribute_ShouldNotCallBulkheadManager()
    {
        // Arrange
        var request = new TestRequestNoBulkhead();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        var behavior = new BulkheadPipelineBehavior<TestRequestNoBulkhead, string>(
            Substitute.For<ILogger<BulkheadPipelineBehavior<TestRequestNoBulkhead, string>>>(),
            _bulkheadManager);

        // Act
        await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        _ = await _bulkheadManager.DidNotReceive().TryAcquireAsync(
            Arg.Any<string>(),
            Arg.Any<BulkheadAttribute>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Successful Execution Tests

    [Fact]
    public async Task Handle_WithinBulkheadLimit_ShouldAllow()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

        var releaser = Substitute.For<IDisposable>();
        SetupAcquireReturns(BulkheadAcquireResult.Acquired(
            releaser,
            new BulkheadMetrics(1, 0, 10, 20, 1, 0)));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_AfterExecution_ShouldReleasePermit()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        var releaser = Substitute.For<IDisposable>();
        SetupAcquireReturns(BulkheadAcquireResult.Acquired(
            releaser,
            new BulkheadMetrics(1, 0, 10, 20, 1, 0)));

        // Act
        await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        releaser.Received(1).Dispose();
    }

    [Fact]
    public async Task Handle_WhenNextThrows_ShouldStillReleasePermit()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Handler exception");

        var releaser = Substitute.For<IDisposable>();
        SetupAcquireReturns(BulkheadAcquireResult.Acquired(
            releaser,
            new BulkheadMetrics(1, 0, 10, 20, 1, 0)));

        // Act
        try
        {
            await _behavior.Handle(request, context, next, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert
        releaser.Received(1).Dispose();
    }

    #endregion

    #region Rejection Tests

    [Fact]
    public async Task Handle_BulkheadFull_ShouldReturnError()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(BulkheadAcquireResult.RejectedBulkheadFull(
            new BulkheadMetrics(10, 20, 10, 20, 100, 50)));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.ShouldContain("Bulkhead full")
        );
    }

    [Fact]
    public async Task Handle_BulkheadFull_ShouldNotCallNextStep()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        SetupAcquireReturns(BulkheadAcquireResult.RejectedBulkheadFull(
            new BulkheadMetrics(10, 20, 10, 20, 100, 50)));

        // Act
        await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeFalse("next step should not be called when bulkhead is full");
    }

    [Fact]
    public async Task Handle_QueueTimeout_ShouldReturnError()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(BulkheadAcquireResult.RejectedQueueTimeout(
            new BulkheadMetrics(10, 0, 10, 20, 100, 50)));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.ShouldContain("queue timeout")
        );
    }

    [Fact]
    public async Task Handle_Cancelled_ShouldReturnError()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(BulkheadAcquireResult.RejectedCancelled(
            new BulkheadMetrics(10, 0, 10, 20, 100, 50)));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.ShouldContain("cancelled")
        );
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public async Task Handle_BulkheadFull_ErrorShouldContainMetrics()
    {
        // Arrange
        var request = new TestBulkheadRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(BulkheadAcquireResult.RejectedBulkheadFull(
            new BulkheadMetrics(10, 20, 10, 20, 100, 50)));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error =>
            {
                error.Message.ShouldContain("10/10"); // Concurrent
                error.Message.ShouldContain("20/20"); // Queued
                // Check for rejection rate (locale-independent: matches both "33.3" and "33,3")
                error.Message.ShouldMatch(@"33[.,]3"); // Rejection rate (50/(100+50))
                return Unit.Default;
            }
        );
    }

    #endregion

    #region Integration with Real BulkheadManager Tests

    [Fact]
    public async Task Handle_WithRealBulkheadManager_ShouldEnforceLimit()
    {
        // Arrange
        using var realManager = new BulkheadManager();
        var behavior = new BulkheadPipelineBehavior<TestBulkheadSmall, string>(
            Substitute.For<ILogger<BulkheadPipelineBehavior<TestBulkheadSmall, string>>>(),
            realManager);

        var context = Substitute.For<IRequestContext>();
        var executingCount = 0;
        var maxConcurrent = 0;

        RequestHandlerCallback<string> next = async () =>
        {
            var current = Interlocked.Increment(ref executingCount);
            lock (this)
            {
                if (current > maxConcurrent) maxConcurrent = current;
            }
            await Task.Delay(100); // Simulate work
            Interlocked.Decrement(ref executingCount);
            return Right<EncinaError, string>("success");
        };

        // Act - Start 5 concurrent requests (limit is 2)
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => behavior.Handle(new TestBulkheadSmall(), context, next, CancellationToken.None).AsTask())
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - At most 2 should have been concurrent
        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    #endregion

    #region Helper Methods

    private void SetupAcquireReturns(BulkheadAcquireResult result)
    {
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _bulkheadManager.TryAcquireAsync(
            Arg.Any<string>(),
            Arg.Any<BulkheadAttribute>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValueTask<BulkheadAcquireResult>(result));
#pragma warning restore CA2012
    }

    #endregion

    // Test request types
    [Bulkhead(MaxConcurrency = 10, MaxQueuedActions = 20)]
    public record TestBulkheadRequest : IRequest<string>;

    [Bulkhead(MaxConcurrency = 2, MaxQueuedActions = 5, QueueTimeoutMs = 5000)]
    public record TestBulkheadSmall : IRequest<string>;

    public record TestRequestNoBulkhead : IRequest<string>;
}
