using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.Tests;

/// <summary>
/// Tests for streaming pipeline behaviors (<see cref="IStreamPipelineBehavior{TRequest, TItem}"/>).
/// </summary>
public sealed class StreamPipelineBehaviorTests
{
    #region Test Data

    public sealed record StreamNumbersQuery(int Count) : IStreamRequest<int>;

    public sealed class StreamNumbersHandler : IStreamRequestHandler<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            StreamNumbersQuery request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                await Task.Delay(1, cancellationToken);
                yield return Right<MediatorError, int>(i);
            }
        }
    }

    public sealed class StreamLoggingBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        public List<string> Logs { get; } = new();

        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Logs.Add("Stream started");
            var count = 0;

            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                count++;
                Logs.Add($"Item {count}");
                yield return item;
            }

            Logs.Add($"Stream completed: {count} items");
        }
    }

    public sealed class StreamTransformBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                // Multiply each successful item by 10
                yield return item.Map(value => value * 10);
            }
        }
    }

    public sealed class StreamFilterBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                // Only yield even numbers
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

    #endregion

    [Fact]
    public async Task StreamBehavior_ShouldWrapHandlerExecution()
    {
        // Arrange
        var loggingBehavior = new StreamLoggingBehavior();
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamPipelineBehaviorTests).Assembly);
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>>(_ => loggingBehavior);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new StreamNumbersQuery(3);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in mediator.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        loggingBehavior.Logs.Should().Contain("Stream started");
        loggingBehavior.Logs.Should().Contain("Item 1");
        loggingBehavior.Logs.Should().Contain("Item 2");
        loggingBehavior.Logs.Should().Contain("Item 3");
        loggingBehavior.Logs.Should().Contain("Stream completed: 3 items");
    }

    [Fact]
    public async Task StreamBehavior_Transform_ShouldModifyItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamPipelineBehaviorTests).Assembly);
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamTransformBehavior>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new StreamNumbersQuery(3);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in mediator.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.IsRight.Should().BeTrue());

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(10, 20, 30); // Original: 1, 2, 3 → Transformed: 10, 20, 30
    }

    [Fact]
    public async Task StreamBehavior_Filter_ShouldOnlyYieldMatchingItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamPipelineBehaviorTests).Assembly);
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamFilterBehavior>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new StreamNumbersQuery(10);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in mediator.Stream(query))
        {
            results.Add(item);
        }

        // Assert - only even numbers (2, 4, 6, 8, 10)
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.IsRight.Should().BeTrue());

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(2, 4, 6, 8, 10);
    }

    [Fact]
    public async Task StreamBehavior_Multiple_ShouldChainInOrder()
    {
        // Arrange
        var loggingBehavior = new StreamLoggingBehavior();
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamPipelineBehaviorTests).Assembly);

        // Register behaviors: Logging → Transform → Filter
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>>(_ => loggingBehavior);
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamTransformBehavior>();
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamFilterBehavior>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new StreamNumbersQuery(6);

        // Act
        var results = new List<Either<MediatorError, int>>();
        await foreach (var item in mediator.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        // Original: 1, 2, 3, 4, 5, 6
        // After Transform (x10): 10, 20, 30, 40, 50, 60
        // After Filter (even only): 10, 20, 30, 40, 50, 60 (all are even after transform)
        results.Should().HaveCount(6);

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(10, 20, 30, 40, 50, 60);

        // Verify logging behavior executed
        loggingBehavior.Logs.Should().Contain("Stream started");
        loggingBehavior.Logs.Should().Contain("Stream completed: 6 items");
    }

    [Fact]
    public async Task StreamBehavior_WithContextPropagation_ShouldAccessContext()
    {
        // Arrange
        var contextCapture = new List<string>();

        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamPipelineBehaviorTests).Assembly);
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>>(sp =>
            new ContextCapturingBehavior(contextCapture));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new StreamNumbersQuery(2);

        // Act
        await foreach (var _ in mediator.Stream(query))
        {
            // Just consume the stream
        }

        // Assert
        contextCapture.Should().Contain(c => c.StartsWith("CorrelationId:", StringComparison.Ordinal));
    }

    private sealed class ContextCapturingBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        private readonly List<string> _capture;

        public ContextCapturingBehavior(List<string> capture) => _capture = capture;

        public async IAsyncEnumerable<Either<MediatorError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _capture.Add($"CorrelationId: {context.CorrelationId}");

            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}
