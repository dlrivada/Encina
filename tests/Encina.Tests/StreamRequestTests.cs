using System.Runtime.CompilerServices;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Tests;

/// <summary>
/// Tests for streaming request functionality (<see cref="IStreamRequest{TItem}"/> and <see cref="IStreamRequestHandler{TRequest, TItem}"/>).
/// </summary>
public sealed class StreamRequestTests
{
    #region Test Data

    public sealed record StreamNumbersQuery(int Count) : IStreamRequest<int>;

    public sealed class StreamNumbersHandler : IStreamRequestHandler<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamNumbersQuery request,
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

    public sealed record StreamWithErrorsQuery(int TotalCount, int ErrorEveryN) : IStreamRequest<int>;

    public sealed class StreamWithErrorsHandler : IStreamRequestHandler<StreamWithErrorsQuery, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamWithErrorsQuery request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.TotalCount; i++)
            {
                if (i % request.ErrorEveryN == 0)
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

    [Fact]
    public async Task Stream_WithValidRequest_ShouldYieldAllItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamRequestTests).Assembly);
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(5);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.IsRight.Should().BeTrue());

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task Stream_WithNullRequest_ShouldYieldError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamRequestTests).Assembly);
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        IStreamRequest<int> nullRequest = null!;

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(nullRequest))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].IsLeft.Should().BeTrue();
        _ = results[0].Match(
            Left: error =>
            {
                error.GetEncinaCode().Should().Be(EncinaErrorCodes.RequestNull);
                return Unit.Default;
            },
            Right: _ => Unit.Default);
    }

    [Fact]
    public async Task Stream_WithMixedSuccessAndErrors_ShouldYieldBoth()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamRequestTests).Assembly);
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamWithErrorsQuery(TotalCount: 10, ErrorEveryN: 3);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(10);

        var successCount = results.Count(r => r.IsRight);
        var errorCount = results.Count(r => r.IsLeft);

        successCount.Should().Be(7); // Items 1, 2, 4, 5, 7, 8, 10
        errorCount.Should().Be(3);   // Items 3, 6, 9
    }

    [Fact]
    public async Task Stream_WithCancellation_ShouldStopYieldingItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamRequestTests).Assembly);
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(100);
        using var cts = new CancellationTokenSource();

        // Act
        var results = new List<Either<EncinaError, int>>();
        try
        {
            await foreach (var item in Encina.Stream(query, cts.Token))
            {
                results.Add(item);

                // Cancel after receiving 5 items
                if (results.Count == 5)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation propagates
        }

        // Assert
        results.Should().HaveCountLessThan(100);
        results.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task Stream_WithNoHandler_ShouldYieldHandlerNotFoundError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(); // No handlers registered
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(5);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].IsLeft.Should().BeTrue();
        _ = results[0].Match(
            Left: error =>
            {
                error.GetEncinaCode().Should().Be(EncinaErrorCodes.HandlerMissing);
                error.Message.Should().Contain("No handler registered");
                return Unit.Default;
            },
            Right: _ => Unit.Default);
    }

    [Fact]
    public async Task Stream_WithEarlyBreak_ShouldNotEnumerateRemainingItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(typeof(StreamRequestTests).Assembly);
        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(100);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);

            // Break after 3 items
            if (results.Count == 3)
            {
                break;
            }
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.IsRight.Should().BeTrue());
    }
}
