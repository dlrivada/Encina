using System.Runtime.CompilerServices;
using Encina.Testing;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;
using EitherShoudlyExt = Encina.Testing.Shouldly.EitherShouldlyExtensions;

#pragma warning disable CA1861 // Prefer static readonly array for test data

namespace Encina.Tests.Integration;

/// <summary>
/// Integration tests for Stream Requests using EncinaTestFixture.
/// Tests end-to-end scenarios with real DI container and IEncina.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StreamRequestIntegrationTests
{
    #region End-to-End Stream Processing

    [Fact]
    public async Task Stream_WithSimpleHandler_ShouldProcessAllItems()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, NumberStreamHandler>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new NumberStreamRequest(Start: 1, Count: 10);

        // Act
        var results = new List<int>();
        await foreach (var item in encina.Stream(request))
        {
            item.IfRight(results.Add);
        }

        // Assert
        results.Count.ShouldBe(10);
        results.ShouldBe(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
    }

    [Fact]
    public async Task Stream_WithSingleBehavior_ShouldApplyTransformation()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, NumberStreamHandler>();
                services.AddTransient<IStreamPipelineBehavior<NumberStreamRequest, int>, DoubleValueBehavior>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new NumberStreamRequest(Start: 1, Count: 5);

        // Act
        var results = new List<int>();
        await foreach (var item in encina.Stream(request))
        {
            item.IfRight(results.Add);
        }

        // Assert
        results.ShouldBe([2, 4, 6, 8, 10]);  // behavior should double each value
    }

    [Fact]
    public async Task Stream_WithMultipleBehaviors_ShouldApplyInOrder()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, NumberStreamHandler>();
                // Register behaviors in order: DoubleValue → AddFive
                services.AddTransient<IStreamPipelineBehavior<NumberStreamRequest, int>, DoubleValueBehavior>();
                services.AddTransient<IStreamPipelineBehavior<NumberStreamRequest, int>, AddFiveBehavior>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new NumberStreamRequest(Start: 1, Count: 3);

        // Act
        var results = new List<int>();
        await foreach (var item in encina.Stream(request))
        {
            item.IfRight(results.Add);
        }

        // Assert - Behaviors execute as: Handler → AddFive → DoubleValue
        // Handler: 1, 2, 3
        // After AddFive: 6, 7, 8
        // After DoubleValue: 12, 14, 16
        results.ShouldBe([12, 14, 16]);  // behaviors should apply in registration order
    }

    [Fact]
    public async Task Stream_WithFilterBehavior_ShouldFilterItems()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, NumberStreamHandler>();
                services.AddTransient<IStreamPipelineBehavior<NumberStreamRequest, int>, GreaterThanFiveBehavior>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new NumberStreamRequest(Start: 1, Count: 10);

        // Act
        var results = new List<int>();
        await foreach (var item in encina.Stream(request))
        {
            item.IfRight(results.Add);
        }

        // Assert - Only values > 5
        results.ShouldBe([6, 7, 8, 9, 10]);  // behavior should filter values <= 5
    }

    #endregion

    #region Error Handling Integration

    [Fact]
    public async Task Stream_WithErrorProducingHandler_ShouldYieldLeftValues()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<ErrorStreamRequest, int>, ErrorStreamHandler>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new ErrorStreamRequest(TotalItems: 10, ErrorAtPosition: 5);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in encina.Stream(request))
        {
            results.Add(item);
        }

        // Assert
        results.Count.ShouldBe(10);
        EitherShoudlyExt.ShouldBeError(results[4], "error should be at position 5");

        var errorCode = results[4].Match(
            Left: error => error.GetEncinaCode(),
            Right: _ => throw new InvalidOperationException("Expected Left"));

        errorCode.ShouldBe("STREAM_ERROR");
    }

    [Fact]
    public async Task Stream_WithErrorRecoveryBehavior_ShouldRecoverFromErrors()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<ErrorStreamRequest, int>, ErrorStreamHandler>();
                services.AddTransient<IStreamPipelineBehavior<ErrorStreamRequest, int>, ErrorRecoveryBehavior>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new ErrorStreamRequest(TotalItems: 5, ErrorAtPosition: 3);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in encina.Stream(request))
        {
            results.Add(item);
        }

        // Assert - All results should be Right (errors recovered)
        results.AllShouldBeSuccess("error recovery behavior should convert all errors to success");
    }

    #endregion

    #region Cancellation Integration

    [Fact]
    public async Task Stream_WithCancellation_ShouldStopProcessing()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, NumberStreamHandler>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new NumberStreamRequest(Start: 1, Count: 100);
        using var cts = new CancellationTokenSource();

        // Act
        var count = 0;
        try
        {
            await foreach (var item in encina.Stream(request, cts.Token))
            {
                item.IfRight(_ => count++);

                if (count == 10)
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
        count.ShouldBeLessThan(100, "cancellation should stop enumeration");
        count.ShouldBeGreaterThanOrEqualTo(10, "should process at least 10 items before cancellation");
    }

    #endregion

    #region Performance and Concurrency

    [Fact]
    public async Task Stream_WithLargeDataSet_ShouldHandleEfficiently()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, FastNumberStreamHandler>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new NumberStreamRequest(Start: 1, Count: 10_000);

        // Act
        var count = 0;
        var startTime = DateTime.UtcNow;

        await foreach (var item in encina.Stream(request))
        {
            item.IfRight(_ => count++);
        }

        var duration = DateTime.UtcNow - startTime;

        // Assert
        count.ShouldBe(10_000, "should process all 10,000 items");
        duration.ShouldBeLessThan(TimeSpan.FromSeconds(5),
            "should process 10,000 items efficiently");
    }

    [Fact]
    public async Task Stream_MultipleSimultaneousStreams_ShouldNotInterfere()
    {
        // Arrange
        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, NumberStreamHandler>();
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();

        var request1 = new NumberStreamRequest(Start: 1, Count: 50);
        var request2 = new NumberStreamRequest(Start: 100, Count: 50);
        var request3 = new NumberStreamRequest(Start: 200, Count: 50);

        // Act - Process three streams concurrently
        var task1 = Task.Run(async () =>
        {
            var results = new List<int>();
            await foreach (var item in encina.Stream(request1))
            {
                item.IfRight(value => results.Add(value));
            }
            return results;
        });

        var task2 = Task.Run(async () =>
        {
            var results = new List<int>();
            await foreach (var item in encina.Stream(request2))
            {
                item.IfRight(value => results.Add(value));
            }
            return results;
        });

        var task3 = Task.Run(async () =>
        {
            var results = new List<int>();
            await foreach (var item in encina.Stream(request3))
            {
                item.IfRight(value => results.Add(value));
            }
            return results;
        });

        var allResults = await Task.WhenAll(task1, task2, task3);

        // Assert - Each stream should have completed independently
        allResults[0].Count.ShouldBe(50);
        allResults[0][0].ShouldBe(1, "first stream should start at 1");
        allResults[1].Count.ShouldBe(50);
        allResults[1][0].ShouldBe(100, "second stream should start at 100");
        allResults[2].Count.ShouldBe(50);
        allResults[2][0].ShouldBe(200, "third stream should start at 200");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task Stream_WithLoggingBehavior_ShouldCountItems()
    {
        // Arrange
        var loggingBehavior = new LoggingBehavior();

        using var fixture = new EncinaTestFixture()
            .ConfigureServices(services =>
            {
                services.AddTransient<IStreamRequestHandler<NumberStreamRequest, int>, NumberStreamHandler>();
                services.AddSingleton<IStreamPipelineBehavior<NumberStreamRequest, int>>(loggingBehavior);
            });

        fixture.Build();
        var encina = fixture.GetRequiredService<IEncina>();
        var request = new NumberStreamRequest(Start: 1, Count: 20);

        // Act
        var results = new List<int>();
        await foreach (var item in encina.Stream(request))
        {
            item.IfRight(results.Add);
        }

        // Assert
        results.Count.ShouldBe(20);
        loggingBehavior.ProcessedCount.ShouldBe(20, "behavior should log all processed items");
    }

    #endregion

    #region Test Data

    private sealed record NumberStreamRequest(int Start, int Count) : IStreamRequest<int>;

    private sealed record ErrorStreamRequest(int TotalItems, int ErrorAtPosition) : IStreamRequest<int>;

    private sealed class NumberStreamHandler : IStreamRequestHandler<NumberStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            NumberStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
                yield return Right<EncinaError, int>(request.Start + i);
            }
        }
    }

    private sealed class FastNumberStreamHandler : IStreamRequestHandler<NumberStreamRequest, int>
    {
#pragma warning disable CS1998 // Async method lacks 'await' - yield return in async IAsyncEnumerable doesn't require await
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            NumberStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // No delay for performance testing
                yield return Right<EncinaError, int>(request.Start + i);
            }
        }
#pragma warning restore CS1998
    }

    private sealed class ErrorStreamHandler : IStreamRequestHandler<ErrorStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            ErrorStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.TotalItems; i++)
            {
                if (i == request.ErrorAtPosition)
                {
                    yield return Left<EncinaError, int>(
                        EncinaErrors.Create("STREAM_ERROR", $"Error at position {i}"));
                }
                else
                {
                    await Task.Delay(1, cancellationToken);
                    yield return Right<EncinaError, int>(i);
                }
            }
        }
    }

    private sealed class DoubleValueBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            NumberStreamRequest request,
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

    private sealed class AddFiveBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            NumberStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item.Map(v => v + 5);
            }
        }
    }

    private sealed class GreaterThanFiveBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            NumberStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                var shouldYield = item.Match(
                    Left: _ => true,
                    Right: value => value > 5);

                if (shouldYield)
                {
                    yield return item;
                }
            }
        }
    }

    private sealed class ErrorRecoveryBehavior : IStreamPipelineBehavior<ErrorStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            ErrorStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                // Convert errors to success with value -1
                yield return item.Match(
                    Left: _ => Right<EncinaError, int>(-1),
                    Right: Right<EncinaError, int>);
            }
        }
    }

    private sealed class LoggingBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
    {
        public int ProcessedCount { get; private set; }

        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            NumberStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                ProcessedCount++;
                yield return item;
            }
        }
    }

    #endregion
}
