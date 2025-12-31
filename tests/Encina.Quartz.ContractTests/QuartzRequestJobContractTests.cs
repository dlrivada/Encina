using Microsoft.Extensions.Logging;
using Quartz;
using static LanguageExt.Prelude;

namespace Encina.Quartz.ContractTests;

/// <summary>
/// Contract tests for QuartzRequestJob.
/// Verifies that the job correctly implements its contract.
/// </summary>
public sealed class QuartzRequestJobContractTests
{
    [Fact]
    public async Task Execute_WithValidRequest_ShouldInvokeEncinaSend()
    {
        // Arrange
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, TestResponse>>>();
        var job = new QuartzRequestJob<TestRequest, TestResponse>(Encina, logger);
        var request = new TestRequest("test");
        var expectedResponse = new TestResponse("result");

        var context = CreateJobExecutionContext(request);

        Encina.Send(request, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(expectedResponse));

        // Act
        await job.Execute(context);

        // Assert
        await Encina.Received(1).Send(request, Arg.Any<CancellationToken>());
        context.Result.ShouldBe(expectedResponse);
    }

    [Fact]
    public async Task Execute_WhenEncinaReturnsError_ShouldThrowJobExecutionException()
    {
        // Arrange
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, TestResponse>>>();
        var job = new QuartzRequestJob<TestRequest, TestResponse>(Encina, logger);
        var request = new TestRequest("test");
        var error = EncinaErrors.Create("test.error", "Test error");

        var context = CreateJobExecutionContext(request);

        Encina.Send(request, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.Execute(context));

        exception.Message.ShouldContain(error.Message);
    }


    [Fact]
    public async Task Execute_WithMissingRequest_ShouldThrowJobExecutionException()
    {
        // Arrange
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, TestResponse>>>();
        var job = new QuartzRequestJob<TestRequest, TestResponse>(Encina, logger);

        var context = CreateJobExecutionContext<TestRequest>(null); // No request in JobDataMap

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.Execute(context));

        exception.Message.ShouldContain("not found in JobDataMap");
    }

    // Helper methods
    private static IJobExecutionContext CreateJobExecutionContext<TRequest>(TRequest? request)
        where TRequest : class
    {
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();

        if (request is not null)
        {
            jobDataMap.Put(QuartzConstants.RequestKey, request);
        }

        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("test-job"));
        context.JobDetail.Returns(jobDetail);
        context.CancellationToken.Returns(CancellationToken.None);

        return context;
    }

}

// Test types (must be public for NSubstitute proxying with strong-named assemblies)
public sealed record TestRequest(string Data) : IRequest<TestResponse>;
public sealed record TestResponse(string Result);
