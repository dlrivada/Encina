using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LanguageExt.Prelude;

#pragma warning disable CA1861 // Prefer static readonly array for test data

namespace Encina.Tests.Contracts;

/// <summary>
/// Contract tests for <see cref="IStreamPipelineBehavior{TRequest, TItem}"/>
/// to verify correct implementation and behavior of stream pipeline behaviors.
/// </summary>
public sealed class StreamPipelineBehaviorContractTests
{
    #region IStreamPipelineBehavior Contract Tests

    [Fact]
    public async Task Handle_ShouldYieldAllItemsFromNextStep()
    {
        // Arrange
        var behavior = new PassThroughStreamBehavior();
        var handler = new TestStreamHandler();
        var request = new TestStreamRequest(Count: 5);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(5, "behavior should yield all items from nextStep");
        results.Should().OnlyContain(r => r.IsRight);

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(new[] { 1, 2, 3, 4, 5 }, "items should preserve order");
    }

    [Fact]
    public async Task Handle_ShouldAllowTransformingItems()
    {
        // Arrange
        var behavior = new MultiplyByTenBehavior();
        var handler = new TestStreamHandler();
        var request = new TestStreamRequest(Count: 3);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(new[] { 10, 20, 30 }, "behavior should transform each item");
    }

    [Fact]
    public async Task Handle_ShouldAllowFilteringItems()
    {
        // Arrange
        var behavior = new EvenOnlyBehavior();
        var handler = new TestStreamHandler();
        var request = new TestStreamRequest(Count: 10);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(5, "behavior should filter out odd numbers");
        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(new[] { 2, 4, 6, 8, 10 }, "only even numbers should be yielded");
    }

    [Fact]
    public async Task Handle_ShouldPreserveErrors()
    {
        // Arrange
        var behavior = new PassThroughStreamBehavior();
        var handler = new ErrorProducingHandler();
        var request = new TestStreamRequest(Count: 3);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].IsRight.Should().BeTrue();
        results[1].IsLeft.Should().BeTrue("behavior should preserve error from nextStep");
        results[2].IsRight.Should().BeTrue();

        var error = results[1].Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left"));

        error.GetMediatorCode().Should().Be("TEST_ERROR");
    }

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var behavior = new PassThroughStreamBehavior();
        var handler = new TestStreamHandler();
        var request = new TestStreamRequest(Count: 100);
        var context = RequestContext.Create();
        using var cts = new CancellationTokenSource();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, cts.Token);

        // Act
        var results = new List<Either<MediatorError, int>>();
        try
        {
            await foreach (var item in behavior.Handle(request, context, nextStep, cts.Token))
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
        results.Should().HaveCountLessThan(100, "behavior should respect cancellation");
    }

    [Fact]
    public async Task Handle_ShouldAccessRequestContext()
    {
        // Arrange
        var behavior = new ContextInspectingBehavior();
        var handler = new TestStreamHandler();
        var request = new TestStreamRequest(Count: 1);
        var context = RequestContext.Create();

        StreamHandlerCallback<int> nextStep = () => handler.Handle(request, CancellationToken.None);

        // Act
        await foreach (var _ in behavior.Handle(request, context, nextStep, CancellationToken.None))
        {
            // Consume stream
        }

        // Assert
        behavior.CapturedCorrelationId.Should().NotBeEmpty("behavior should have access to context");
        behavior.CapturedCorrelationId.Should().Be(context.CorrelationId.ToString());
    }

    #endregion

    #region Integration with IMediator Tests

    [Fact]
    public async Task IMediator_Stream_WithSingleBehavior_ShouldExecuteBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<TestStreamRequest, int>, TestStreamHandler>();
        services.AddTransient<IStreamPipelineBehavior<TestStreamRequest, int>, MultiplyByTenBehavior>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var request = new TestStreamRequest(Count: 3);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in mediator.Stream(request))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(new[] { 10, 20, 30 }, "behavior should transform items");
    }

    [Fact]
    public async Task IMediator_Stream_WithMultipleBehaviors_ShouldExecuteInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IStreamRequestHandler<TestStreamRequest, int>, TestStreamHandler>();

        // Register behaviors in order: Multiply → Filter
        services.AddTransient<IStreamPipelineBehavior<TestStreamRequest, int>, MultiplyByTenBehavior>();
        services.AddTransient<IStreamPipelineBehavior<TestStreamRequest, int>, EvenOnlyBehavior>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var request = new TestStreamRequest(Count: 5);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in mediator.Stream(request))
        {
            results.Add(item);
        }

        // Assert
        // Behaviors execute in registration order: Multiply → Filter
        // Original: 1, 2, 3, 4, 5
        // After Filter (even only): 2, 4 (1, 3, 5 are odd, filtered out)
        // After Multiply: 20, 40
        results.Should().HaveCount(2);
        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(new[] { 20, 40 });
    }

    #endregion

    #region Test Data

    private sealed record TestStreamRequest(int Count) : IStreamRequest<int>;

    private sealed class TestStreamHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
                yield return Right<MediatorError, int>(i);
            }
        }
    }

    private sealed class ErrorProducingHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return Right<MediatorError, int>(1);
            yield return Left<MediatorError, int>(MediatorErrors.Create("TEST_ERROR", "Test error"));
            yield return Right<MediatorError, int>(3);
            await Task.CompletedTask;
        }
    }

    private sealed class PassThroughStreamBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class MultiplyByTenBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item.Map(v => v * 10);
            }
        }
    }

    private sealed class EvenOnlyBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                var shouldYield = item.Match(
                    Left: _ => true, // Always yield errors
                    Right: value => value % 2 == 0);

                if (shouldYield)
                {
                    yield return item;
                }
            }
        }
    }

    private sealed class ContextInspectingBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public string CapturedCorrelationId { get; private set; } = string.Empty;

        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            CapturedCorrelationId = context.CorrelationId.ToString();

            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }

    #endregion
}
