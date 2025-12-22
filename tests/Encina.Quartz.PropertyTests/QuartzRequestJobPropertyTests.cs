using Encina.Quartz;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Quartz.PropertyTests;

/// <summary>
/// Property-based tests for QuartzRequestJob.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class QuartzRequestJobPropertyTests
{
    [Fact]
    public async Task Property_SuccessfulExecution_AlwaysSetsContextResult()
    {
        // Property: When mediator returns Right, context.Result ALWAYS set

        var testCases = new[]
        {
            ("result1", new TestRequest("data1")),
            ("result2", new TestRequest("data2")),
            ("result3", new TestRequest("data3")),
        };

        foreach (var (expectedResult, request) in testCases)
        {
            // Arrange
            var mediator = Substitute.For<IMediator>();
            var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();
            var job = new QuartzRequestJob<TestRequest, string>(mediator, logger);
            var context = CreateJobExecutionContext(request);

            mediator.Send(request, Arg.Any<CancellationToken>())
                .Returns(Right<MediatorError, string>(expectedResult));

            // Act
            await job.Execute(context);

            // Assert
            context.Result.ShouldBe(expectedResult);
        }
    }

    [Fact]
    public async Task Property_MediatorError_AlwaysThrowsJobExecutionException()
    {
        // Property: When mediator returns Left, ALWAYS throws JobExecutionException

        var testErrors = new[]
        {
            MediatorErrors.Create("error1", "Error 1"),
            MediatorErrors.Create("error2", "Error 2"),
            MediatorErrors.Create("error3", "Error 3"),
        };

        foreach (var expectedError in testErrors)
        {
            // Arrange
            var mediator = Substitute.For<IMediator>();
            var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();
            var job = new QuartzRequestJob<TestRequest, string>(mediator, logger);
            var request = new TestRequest("test");
            var context = CreateJobExecutionContext(request);

            mediator.Send(request, Arg.Any<CancellationToken>())
                .Returns(Left<MediatorError, string>(expectedError));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<JobExecutionException>(() => job.Execute(context));
            exception.Message.ShouldContain(expectedError.Message);
        }
    }

    [Fact]
    public async Task Property_Idempotency_SameRequestSameResult()
    {
        // Property: Same request ALWAYS produces same result

        var request = new TestRequest("idempotent-test");
        var expectedResult = "consistent-result";
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();
        var job = new QuartzRequestJob<TestRequest, string>(mediator, logger);

        mediator.Send(request, Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, string>(expectedResult));

        // Act - Multiple executions
        var context1 = CreateJobExecutionContext(request);
        var context2 = CreateJobExecutionContext(request);
        var context3 = CreateJobExecutionContext(request);

        await job.Execute(context1);
        await job.Execute(context2);
        await job.Execute(context3);

        // Assert - All results identical
        context1.Result.ShouldBe(expectedResult);
        context2.Result.ShouldBe(expectedResult);
        context3.Result.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task Property_ConcurrentExecution_ThreadSafe()
    {
        // Property: Concurrent executions are thread-safe

        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();
        var job = new QuartzRequestJob<TestRequest, string>(mediator, logger);

        mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, string>("success"));

        // Act - Execute concurrently
        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () =>
            {
                var context = CreateJobExecutionContext(new TestRequest($"request-{i}"));
                await job.Execute(context);
                return context;
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All calls succeed
        tasks.All(t => t.Result.Result != null).ShouldBeTrue();
    }

    [Fact]
    public async Task Property_MediatorInvocation_AlwaysCalledExactlyOnce()
    {
        // Property: Mediator ALWAYS invoked exactly once per execution

        var testRequests = new[]
        {
            new TestRequest("req1"),
            new TestRequest("req2"),
            new TestRequest("req3"),
        };

        foreach (var request in testRequests)
        {
            var mediator = Substitute.For<IMediator>();
            var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();
            var job = new QuartzRequestJob<TestRequest, string>(mediator, logger);
            var context = CreateJobExecutionContext(request);

            mediator.Send(request, Arg.Any<CancellationToken>())
                .Returns(Right<MediatorError, string>("result"));

            // Act
            await job.Execute(context);

            // Assert
            await mediator.Received(1).Send(request, Arg.Any<CancellationToken>());
        }
    }

    // Helper method
    private static IJobExecutionContext CreateJobExecutionContext<TRequest>(TRequest request)
        where TRequest : class
    {
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();

        jobDataMap.Put(QuartzConstants.RequestKey, request);

        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("test-job"));
        context.JobDetail.Returns(jobDetail);
        context.CancellationToken.Returns(CancellationToken.None);

        return context;
    }
}

// Test types
public sealed record TestRequest(string Data) : IRequest<string>;
