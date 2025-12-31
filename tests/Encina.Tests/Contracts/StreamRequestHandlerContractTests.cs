using System.Runtime.CompilerServices;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

#pragma warning disable CA1861 // Prefer static readonly array for test data

namespace Encina.Tests.Contracts;

/// <summary>
/// Contract tests for <see cref="IStreamRequestHandler{TRequest, TItem}"/>
/// to verify correct implementation and behavior of stream handlers.
/// </summary>
public sealed class StreamRequestHandlerContractTests
{
    #region IStreamRequestHandler Contract Tests

    [Fact]
    public async Task Handle_ShouldYieldAllItemsInOrder()
    {
        // Arrange
        var handler = new OrderedStreamHandler();
        var request = new TestStreamRequest(Count: 5);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.Count.ShouldBe(5, "handler should yield all requested items");
        results.AllShouldBeSuccess("all items should be successful");

        var values = results.Select(r => r.Match(Left: _ => -1, Right: v => v)).ToList();
        values.ShouldBe([1, 2, 3, 4, 5]);  // items should be yielded in sequential order
    }

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var handler = new LongRunningStreamHandler();
        var request = new TestStreamRequest(Count: 1000);
        using var cts = new CancellationTokenSource();

        // Act
        var results = new List<Either<EncinaError, int>>();
        try
        {
            await foreach (var item in handler.Handle(request, cts.Token))
            {
                results.Add(item);

                // Cancel after 5 items
                if (results.Count == 5)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        results.Count.ShouldBeLessThan(1000, "enumeration should stop when cancelled");
        results.Count.ShouldBeGreaterThanOrEqualTo(5, "should yield at least items before cancellation");
    }

    [Fact]
    public async Task Handle_WithEmptyStream_ShouldYieldNothing()
    {
        // Arrange
        var handler = new OrderedStreamHandler();
        var request = new TestStreamRequest(Count: 0);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.ShouldBeEmpty("handler should yield no items when count is zero");
    }

    [Fact]
    public async Task Handle_WithErrors_ShouldYieldLeftValues()
    {
        // Arrange
        var handler = new ErrorStreamHandler();
        var request = new TestStreamRequest(Count: 10);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.Count.ShouldBe(10);
        results.ShouldContainError("some items should be errors");
        results.ShouldContainSuccess("some items should be successful");

        var errorCount = results.Count(r => r.IsLeft);
        errorCount.ShouldBe(3, "errors should occur at positions 3, 6, 9");
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent()
    {
        // Arrange
        var handler = new OrderedStreamHandler();
        var request = new TestStreamRequest(Count: 3);

        // Act - call twice
        var results1 = new List<Either<EncinaError, int>>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            results1.Add(item);
        }

        var results2 = new List<Either<EncinaError, int>>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            results2.Add(item);
        }

        // Assert
        results1.Count.ShouldBe(3);
        results2.Count.ShouldBe(3);

        var values1 = results1.Select(r => r.Match(Left: _ => -1, Right: v => v)).ToList();
        var values2 = results2.Select(r => r.Match(Left: _ => -1, Right: v => v)).ToList();

        values1.ShouldBe(values2, "handler should yield same results when called multiple times");
    }

    #endregion

    #region Integration with IEncina Tests

    [Fact]
    public async Task IEncina_Stream_WithRegisteredHandler_ShouldExecuteCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<TestStreamRequest, int>, OrderedStreamHandler>();
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var request = new TestStreamRequest(Count: 3);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(request))
        {
            results.Add(item);
        }

        // Assert
        results.Count.ShouldBe(3, "Encina should delegate to registered handler");
        results.AllShouldBeSuccess();
    }

    [Fact]
    public async Task IEncina_Stream_WithoutRegisteredHandler_ShouldYieldError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(); // No handlers registered
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var request = new TestStreamRequest(Count: 5);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(request))
        {
            results.Add(item);
        }

        // Assert
        results.Count.ShouldBe(1, "should yield single error for missing handler");
        results[0].ShouldBeError("result should be an error");

        var error = results[0].Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left"));

        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.HandlerMissing);
        error.Message.ShouldContain("No handler registered");
    }

    [Fact]
    public async Task IEncina_Stream_WithCancellation_ShouldPropagateToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<TestStreamRequest, int>, OrderedStreamHandler>();
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var request = new TestStreamRequest(Count: 100);
        using var cts = new CancellationTokenSource();

        // Act
        var results = new List<Either<EncinaError, int>>();
        try
        {
            await foreach (var item in Encina.Stream(request, cts.Token))
            {
                results.Add(item);

                if (results.Count == 3)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        results.Count.ShouldBeLessThan(100, "cancellation should stop enumeration");
    }

    #endregion

    #region Test Data

    private sealed record TestStreamRequest(int Count) : IStreamRequest<int>;

    private sealed class OrderedStreamHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken); // Simulate async work
                yield return Right<EncinaError, int>(i);
            }
        }
    }

    private sealed class LongRunningStreamHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken); // Longer delay to test cancellation
                yield return Right<EncinaError, int>(i);
            }
        }
    }

    private sealed class ErrorStreamHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                if (i % 3 == 0)
                {
                    yield return Left<EncinaError, int>(
                        EncinaErrors.Create("TEST_ERROR", $"Error at item {i}"));
                }
                else
                {
                    await Task.Delay(1, cancellationToken);
                    yield return Right<EncinaError, int>(i);
                }
            }
        }
    }

    #endregion
}
