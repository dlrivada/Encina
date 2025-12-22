using Encina.Hangfire;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.PropertyTests;

/// <summary>
/// Property-based tests for HangfireRequestJobAdapter.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class HangfireRequestJobAdapterPropertyTests
{
    [Fact]
    public async Task Property_SuccessfulExecution_AlwaysReturnsRight()
    {
        // Property: When Encina returns Right, adapter ALWAYS returns Right

        var testCases = new[]
        {
            ("result1", new TestRequest("data1")),
            ("result2", new TestRequest("data2")),
            ("result3", new TestRequest("data3")),
        };

        foreach (var (expectedResult, request) in testCases)
        {
            // Arrange
            var Encina = Substitute.For<IEncina>();
            var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();
            var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);

            Encina.Send(request, Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, string>(expectedResult));

            // Act
            var result = await adapter.ExecuteAsync(request);

            // Assert
            result.IsRight.ShouldBeTrue();
            result.Match(
                Left: _ => throw new InvalidOperationException("Expected Right"),
                Right: actual => actual.ShouldBe(expectedResult));
        }
    }

    [Fact]
    public async Task Property_EncinaError_AlwaysReturnsLeft()
    {
        // Property: When Encina returns Left, adapter ALWAYS returns Left

        var testCases = new[]
        {
            EncinaErrors.Create("error1", "Error 1"),
            EncinaErrors.Create("error2", "Error 2"),
            EncinaErrors.Create("error3", "Error 3"),
        };

        foreach (var expectedError in testCases)
        {
            // Arrange
            var Encina = Substitute.For<IEncina>();
            var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();
            var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);
            var request = new TestRequest("test");

            Encina.Send(request, Arg.Any<CancellationToken>())
                .Returns(Left<EncinaError, string>(expectedError));

            // Act
            var result = await adapter.ExecuteAsync(request);

            // Assert
            result.IsLeft.ShouldBeTrue();
            result.Match(
                Left: actual => actual.ShouldBe(expectedError),
                Right: _ => throw new InvalidOperationException("Expected Left"));
        }
    }

    [Fact]
    public async Task Property_Idempotency_SameRequestSameResult()
    {
        // Property: Same request ALWAYS produces same result

        var request = new TestRequest("idempotent-test");
        var expectedResult = "consistent-result";

        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();
        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);

        Encina.Send(request, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>(expectedResult));

        // Act - Multiple executions
        var result1 = await adapter.ExecuteAsync(request);
        var result2 = await adapter.ExecuteAsync(request);
        var result3 = await adapter.ExecuteAsync(request);

        // Assert - All results identical
        result1.IsRight.ShouldBe(result2.IsRight);
        result2.IsRight.ShouldBe(result3.IsRight);

        var allMatch = result1.Match(Right: r1 =>
            result2.Match(Right: r2 =>
                result3.Match(Right: r3 =>
                {
                    r1.ShouldBe(r2);
                    r2.ShouldBe(r3);
                    return true;
                }, Left: _ => false),
                Left: _ => false),
            Left: _ => false);

        allMatch.ShouldBeTrue();
    }

    [Fact]
    public async Task Property_ConcurrentExecution_ThreadSafe()
    {
        // Property: Concurrent executions are thread-safe

        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();
        var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);

        Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("success"));

        // Act - Execute concurrently
        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () => await adapter.ExecuteAsync(new TestRequest($"request-{i}"))))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All calls succeed
        tasks.All(t => t.Result.IsRight).ShouldBeTrue();
    }

    [Fact]
    public async Task Property_EncinaInvocation_AlwaysCalledExactlyOnce()
    {
        // Property: Encina ALWAYS invoked exactly once per execution

        var testRequests = new[]
        {
            new TestRequest("req1"),
            new TestRequest("req2"),
            new TestRequest("req3"),
        };

        foreach (var request in testRequests)
        {
            var Encina = Substitute.For<IEncina>();
            var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, string>>>();
            var adapter = new HangfireRequestJobAdapter<TestRequest, string>(Encina, logger);

            Encina.Send(request, Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, string>("result"));

            // Act
            await adapter.ExecuteAsync(request);

            // Assert
            await Encina.Received(1).Send(request, Arg.Any<CancellationToken>());
        }
    }
}

// Test types
public sealed record TestRequest(string Data) : IRequest<string>;
