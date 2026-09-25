using LanguageExt;
using Microsoft.Extensions.Logging;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Web.Hangfire;

/// <summary>
/// Property-based tests for HangfireRequestJobAdapter.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class HangfireRequestJobAdapterPropertyTests
{
    [Fact]
    public async Task Property_SuccessfulExecution_AlwaysReturnsTheResponse()
    {
        // Property: When Encina returns Right, adapter ALWAYS returns the response

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
            var result = await adapter.ExecuteAndReturnResultAsync(request);

            // Assert
            result.ShouldBe(expectedResult);
        }
    }

    [Fact]
    public async Task Property_EncinaError_AlwaysThrowsEncinaJobFailedException()
    {
        // Property: When Encina returns Left, adapter ALWAYS throws EncinaJobFailedException,
        // so Hangfire marks the job as failed and retries it, instead of recording success.

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
            var exception = await Should.ThrowAsync<EncinaJobFailedException>(() =>
                adapter.ExecuteAsync(request));

            // Assert - the code identifies the failure; the error message is never copied into the
            // exception, because Hangfire persists exception messages
            exception.ErrorCode.ShouldBe(expectedError.GetCode().IfNone(string.Empty));
            exception.Message.ShouldNotContain(expectedError.Message);
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
        var result1 = await adapter.ExecuteAndReturnResultAsync(request);
        var result2 = await adapter.ExecuteAndReturnResultAsync(request);
        var result3 = await adapter.ExecuteAndReturnResultAsync(request);

        // Assert - All results identical
        result1.ShouldBe(expectedResult);
        result2.ShouldBe(result1);
        result3.ShouldBe(result2);
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
            .Select(i => Task.Run(async () => await adapter.ExecuteAndReturnResultAsync(new TestRequest($"request-{i}"))))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All calls succeed
        tasks.Select(t => t.Result).ShouldAllBe(result => result == "success");
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
