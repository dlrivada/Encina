using System.Runtime.CompilerServices;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

#pragma warning disable CA1861 // Prefer static readonly array for test data

namespace Encina.Tests.PropertyTests;

/// <summary>
/// Property-based tests for Stream Requests.
/// Verifies invariants and properties that should hold across various inputs.
/// </summary>
public sealed class StreamRequestPropertyTests
{
    #region Stream Ordering Invariants

    /// <summary>
    /// Property: Stream always yields items in sequential order.
    /// Invariant: For stream producing 1..N, items are always in ascending order.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task StreamOrder_AlwaysSequential(int itemCount)
    {
        // Arrange
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(itemCount);

        // Act
        var results = new List<int>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            item.IfRight(results.Add);
        }

        // Assert - Results are in ascending order
        results.Count.ShouldBe(itemCount);
        for (var i = 0; i < results.Count; i++)
        {
            results[i].ShouldBe(i + 1, $"item at index {i} should be {i + 1}");
        }
    }

    /// <summary>
    /// Property: Multiple enumerations produce identical results (idempotency).
    /// Invariant: Enumerating same request N times produces same sequence.
    /// </summary>
    [Theory]
    [InlineData(1, 3)]   // 1 item, enumerate 3 times
    [InlineData(5, 2)]   // 5 items, enumerate 2 times
    [InlineData(10, 5)]  // 10 items, enumerate 5 times
    public async Task MultipleEnumerations_ProduceIdenticalResults(int itemCount, int enumerationCount)
    {
        // Arrange
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(itemCount);

        // Act - Enumerate N times
        var allResults = new List<List<int>>();
        for (var i = 0; i < enumerationCount; i++)
        {
            var results = new List<int>();
            await foreach (var item in handler.Handle(request, CancellationToken.None))
            {
                item.IfRight(results.Add);
            }
            allResults.Add(results);
        }

        // Assert - All enumerations produce identical results
        for (var i = 1; i < allResults.Count; i++)
        {
            allResults[i].ShouldBe(allResults[0],
                $"enumeration {i} should match first enumeration");
        }
    }

    #endregion

    #region Item Count Invariants

    /// <summary>
    /// Property: Stream yields exactly N items for Count=N (no duplicates, no missing).
    /// Invariant: |stream| = N
    /// </summary>
    [Theory]
    [InlineData(0)]   // Empty stream
    [InlineData(1)]   // Single item
    [InlineData(10)]  // Small batch
    [InlineData(100)] // Large batch
    [InlineData(1000)] // Very large batch
    public async Task ItemCount_MatchesRequestCount(int expectedCount)
    {
        // Arrange
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(expectedCount);

        // Act
        var count = 0;
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            if (item.IsRight) count++;
        }

        // Assert
        count.ShouldBe(expectedCount);  // stream should yield exactly expectedCount items
    }

    /// <summary>
    /// Property: Empty stream (Count=0) always yields zero items.
    /// Invariant: Count=0 ? |stream| = 0
    /// </summary>
    [Theory]
    [InlineData(0)]
    public async Task EmptyStream_YieldsNothing(int count)
    {
        // Arrange
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(count);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.ShouldBeEmpty("empty stream should yield no items");
    }

    #endregion

    #region Error Propagation Invariants

    /// <summary>
    /// Property: Errors are always yielded in sequence (not thrown).
    /// Invariant: Error items appear as Left values in stream, never as exceptions.
    /// </summary>
    [Theory]
    [InlineData(3)]  // Error every 3rd item
    [InlineData(5)]  // Error every 5th item
    [InlineData(10)] // Error every 10th item
    public async Task Errors_YieldedInSequence(int errorInterval)
    {
        // Arrange
        var handler = new ErrorAtIntervalHandler(errorInterval);
        var request = new TestStreamRequest(errorInterval * 3); // Produce 3 errors

        // Act
        var leftCount = 0;
        var rightCount = 0;
        var exceptionThrown = false;

        try
        {
            await foreach (var item in handler.Handle(request, CancellationToken.None))
            {
                _ = item.Match(
                    Left: _ => leftCount++,
                    Right: _ => rightCount++
                );
            }
        }
        catch
        {
            exceptionThrown = true;
        }

        // Assert
        exceptionThrown.ShouldBeFalse("errors should be yielded, not thrown");
        leftCount.ShouldBe(3, "should yield 3 error items");
        rightCount.ShouldBe(errorInterval * 3 - 3, "should yield success items");
    }

    /// <summary>
    /// Property: Error position in stream is preserved.
    /// Invariant: Error at position N is always at position N in results.
    /// </summary>
    [Theory]
    [InlineData(2)]  // Error at position 2
    [InlineData(5)]  // Error at position 5
    [InlineData(10)] // Error at position 10
    public async Task ErrorPosition_AlwaysPreserved(int errorPosition)
    {
        // Arrange
        var handler = new ErrorAtPositionHandler(errorPosition);
        var request = new TestStreamRequest(errorPosition + 5);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results[errorPosition - 1].ShouldBeError($"item at position {errorPosition} should be error");
    }

    #endregion

    #region Cancellation Invariants

    /// <summary>
    /// Property: Cancellation always stops enumeration.
    /// Invariant: After cancellation, no more items are yielded.
    /// </summary>
    [Theory]
    [InlineData(100, 5)]   // Cancel after 5 items of 100
    [InlineData(1000, 10)] // Cancel after 10 items of 1000
    [InlineData(50, 3)]    // Cancel after 3 items of 50
    public async Task Cancellation_StopsEnumeration(int totalItems, int cancelAfter)
    {
        // Arrange
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(totalItems);
        using var cts = new CancellationTokenSource();

        // Act
        var count = 0;
        try
        {
            await foreach (var item in handler.Handle(request, cts.Token))
            {
                if (item.IsRight) count++;

                if (count == cancelAfter)
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
        count.ShouldBeLessThan(totalItems, "cancellation should stop enumeration");
        count.ShouldBeGreaterThanOrEqualTo(cancelAfter,
            "should yield at least items before cancellation");
    }

    #endregion

    #region Pipeline Behavior Invariants

    /// <summary>
    /// Property: Behaviors execute in registration order.
    /// Invariant: Behavior A registered before B ? A executes before B.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task BehaviorOrder_MatchesRegistrationOrder(int itemCount)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<TestStreamRequest, int>, SequentialStreamHandler>();

        // Register behaviors in specific order: AddOne ? MultiplyTwo
        services.AddTransient<IStreamPipelineBehavior<TestStreamRequest, int>, AddOneBehavior>();
        services.AddTransient<IStreamPipelineBehavior<TestStreamRequest, int>, MultiplyTwoBehavior>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var request = new TestStreamRequest(itemCount);

        // Act
        var results = new List<int>();
        await foreach (var item in Encina.Stream(request))
        {
            item.IfRight(results.Add);
        }

        // Assert - Behaviors execute as: AddOne ? MultiplyTwo ? Handler
        // Handler produces: 1, 2, 3, ...
        // After MultiplyTwo: 2, 4, 6, ...
        // After AddOne: 3, 5, 7, ...
        for (var i = 0; i < results.Count; i++)
        {
            var expected = ((i + 1) * 2) + 1;
            results[i].ShouldBe(expected,
                $"behavior execution order affects result at index {i}");
        }
    }

    /// <summary>
    /// Property: Behavior transformation is consistent across all items.
    /// Invariant: ? item, behavior applies same transformation.
    /// </summary>
    [Theory]
    [InlineData(5, 2)]   // 5 items, multiply by 2
    [InlineData(10, 3)]  // 10 items, multiply by 3
    [InlineData(20, 5)]  // 20 items, multiply by 5
    public async Task BehaviorTransformation_ConsistentAcrossItems(int itemCount, int multiplier)
    {
        // Arrange
        var behavior = new MultiplyBehavior(multiplier);
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(itemCount);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        var results = new List<int>();
        await foreach (var item in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            item.IfRight(results.Add);
        }

        // Assert - Every item multiplied consistently
        for (var i = 0; i < results.Count; i++)
        {
            results[i].ShouldBe((i + 1) * multiplier,
                $"behavior should apply multiplier {multiplier} to item {i + 1}");
        }
    }

    /// <summary>
    /// Property: Filter behavior preserves order of remaining items.
    /// Invariant: Filter removes items but preserves relative order.
    /// </summary>
    [Theory]
    [InlineData(10)] // Filter evens from 1-10 ? 2,4,6,8,10
    [InlineData(20)] // Filter evens from 1-20 ? 2,4,6,...,20
    [InlineData(50)] // Filter evens from 1-50
    public async Task FilterBehavior_PreservesOrder(int itemCount)
    {
        // Arrange
        var behavior = new FilterEvensBehavior();
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(itemCount);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        var results = new List<int>();
        await foreach (var item in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            item.IfRight(results.Add);
        }

        // Assert - Results are evens in ascending order
        var expectedCount = itemCount / 2;
        results.Count.ShouldBe(expectedCount);

        for (var i = 0; i < results.Count; i++)
        {
            results[i].ShouldBe((i + 1) * 2, "filtered items should be evens in order");
        }
    }

    #endregion

    #region IEncina Integration Invariants

    /// <summary>
    /// Property: Encina delegates to registered handler correctly.
    /// Invariant: Encina.Stream(request) produces same results as handler.Handle(request).
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task Encina_DelegatesToHandler(int itemCount)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<TestStreamRequest, int>, SequentialStreamHandler>();
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var request = new TestStreamRequest(itemCount);

        // Act - Via Encina
        var EncinaResults = new List<int>();
        await foreach (var item in Encina.Stream(request))
        {
            item.IfRight(EncinaResults.Add);
        }

        // Act - Via handler directly
        var handler = new SequentialStreamHandler();
        var handlerResults = new List<int>();
        await foreach (var item in handler.Handle(request, CancellationToken.None))
        {
            item.IfRight(handlerResults.Add);
        }

        // Assert - Results are identical
        EncinaResults.ShouldBe(handlerResults);  // Encina should delegate to handler and produce same results
    }

    /// <summary>
    /// Property: Missing handler always yields single error.
    /// Invariant: No registered handler ? stream yields exactly one Left with HandlerMissing code.
    /// </summary>
    [Fact]
    public async Task MissingHandler_YieldsSingleError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(); // No handler registered
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var request = new TestStreamRequest(10);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(request))
        {
            results.Add(item);
        }

        // Assert
        results.Count.ShouldBe(1, "missing handler should yield single error");
        results[0].ShouldBeError("result should be error");

        var error = results[0].Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left"));

        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.HandlerMissing);
    }

    #endregion

    #region Request Context Invariants

    /// <summary>
    /// Property: Request context is accessible in behaviors.
    /// Invariant: IRequestContext passed to behavior is non-null with valid CorrelationId.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task RequestContext_AlwaysAccessibleInBehavior(int itemCount)
    {
        // Arrange
        var behavior = new ContextCapturingBehavior();
        var handler = new SequentialStreamHandler();
        var request = new TestStreamRequest(itemCount);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        await foreach (var _ in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            // Consume stream
        }

        // Assert
        behavior.CapturedContext.ShouldNotBeNull("behavior should receive context");
        behavior.CapturedContext!.CorrelationId.ShouldNotBeNullOrEmpty(
            "context should have valid CorrelationId");
        behavior.CapturedContext.CorrelationId.ShouldBe(context.CorrelationId,
            "behavior should receive same context instance");
    }

    #endregion

    #region Test Data

    private sealed record TestStreamRequest(int Count) : IStreamRequest<int>;

    private sealed class SequentialStreamHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return Right<EncinaError, int>(i);
            }
        }
    }

    private sealed class ErrorAtIntervalHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        private readonly int _errorInterval;

        public ErrorAtIntervalHandler(int errorInterval)
        {
            _errorInterval = errorInterval;
        }

        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                if (i % _errorInterval == 0)
                {
                    yield return Left<EncinaError, int>(
                        EncinaErrors.Create("TEST_ERROR", $"Error at item {i}"));
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                    yield return Right<EncinaError, int>(i);
                }
            }
        }
    }

    private sealed class ErrorAtPositionHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        private readonly int _errorPosition;

        public ErrorAtPositionHandler(int errorPosition)
        {
            _errorPosition = errorPosition;
        }

        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                if (i == _errorPosition)
                {
                    yield return Left<EncinaError, int>(
                        EncinaErrors.Create("POSITION_ERROR", $"Error at position {_errorPosition}"));
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                    yield return Right<EncinaError, int>(i);
                }
            }
        }
    }

    private sealed class AddOneBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item.Map(v => v + 1);
            }
        }
    }

    private sealed class MultiplyTwoBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item.Map(v => v * 2);
            }
        }
    }

    private sealed class MultiplyBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        private readonly int _multiplier;

        public MultiplyBehavior(int multiplier)
        {
            _multiplier = multiplier;
        }

        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item.Map(v => v * _multiplier);
            }
        }
    }

    private sealed class FilterEvensBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                var shouldYield = item.Match(
                    Left: _ => true, // Always yield errors
                    Right: value => value % 2 == 0); // Only evens

                if (shouldYield)
                {
                    yield return item;
                }
            }
        }
    }

    private sealed class ContextCapturingBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public IRequestContext? CapturedContext { get; private set; }

        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            CapturedContext = context;

            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }

    #endregion

    #region FsCheck Property Tests

    /// <summary>
    /// Property: Stream always yields exactly the requested count.
    /// Uses FsCheck to verify across random item counts.
    /// </summary>
    [EncinaProperty]
    public Property Stream_AlwaysYieldsExactCount()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 100)),
            async expectedCount =>
            {
                var handler = new SequentialStreamHandler();
                var request = new TestStreamRequest(expectedCount);

                var count = 0;
                await foreach (var item in handler.Handle(request, CancellationToken.None))
                {
                    if (item.IsRight) count++;
                }

                return count == expectedCount;
            });
    }

    /// <summary>
    /// Property: Stream items are always in ascending order.
    /// Verified across random item counts.
    /// </summary>
    [EncinaProperty]
    public Property Stream_ItemsAreAlwaysAscending()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 50)),
            async itemCount =>
            {
                var handler = new SequentialStreamHandler();
                var request = new TestStreamRequest(itemCount);

                var results = new List<int>();
                await foreach (var item in handler.Handle(request, CancellationToken.None))
                {
                    item.IfRight(results.Add);
                }

                // Verify ascending order
                for (var i = 1; i < results.Count; i++)
                {
                    if (results[i] <= results[i - 1])
                        return false;
                }

                return true;
            });
    }

    /// <summary>
    /// Property: Error items are always Left values, never exceptions.
    /// Uses FsCheck to verify across random error intervals.
    /// </summary>
    [EncinaProperty]
    public Property Errors_AreAlwaysLeftValues()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(2, 10)),
            async errorInterval =>
            {
                var handler = new ErrorAtIntervalHandler(errorInterval);
                var request = new TestStreamRequest(errorInterval * 2);

                var exceptionThrown = false;
                var leftCount = 0;

                try
                {
                    await foreach (var item in handler.Handle(request, CancellationToken.None))
                    {
                        if (item.IsLeft) leftCount++;
                    }
                }
                catch
                {
                    exceptionThrown = true;
                }

                return !exceptionThrown && leftCount == 2;
            });
    }

    #endregion
}
