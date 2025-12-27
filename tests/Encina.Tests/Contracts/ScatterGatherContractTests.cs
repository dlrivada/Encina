using Encina.Messaging.ScatterGather;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Contracts;

public sealed class ScatterGatherContractTests
{
    [Fact]
    public void IScatterGatherRunner_Interface_HasExecuteAsyncMethod()
    {
        // Verify the interface contract
        var interfaceType = typeof(IScatterGatherRunner);

        var executeMethod = interfaceType.GetMethod("ExecuteAsync");
        executeMethod.ShouldNotBeNull();
        executeMethod.IsGenericMethod.ShouldBeTrue();
        executeMethod.GetGenericArguments().Length.ShouldBe(2);
    }

    [Fact]
    public void ScatterGatherRunner_ImplementsIScatterGatherRunner()
    {
        typeof(ScatterGatherRunner).GetInterfaces()
            .ShouldContain(typeof(IScatterGatherRunner));
    }

    [Fact]
    public void GatherStrategy_HasExpectedValues()
    {
        // Verify all expected strategy values exist
        Enum.GetNames<GatherStrategy>().ShouldContain("WaitForAll");
        Enum.GetNames<GatherStrategy>().ShouldContain("WaitForFirst");
        Enum.GetNames<GatherStrategy>().ShouldContain("WaitForQuorum");
        Enum.GetNames<GatherStrategy>().ShouldContain("WaitForAllAllowPartial");
    }

    [Fact]
    public void ScatterGatherResult_HasRequiredProperties()
    {
        var type = typeof(ScatterGatherResult<>);

        type.GetProperty("OperationId").ShouldNotBeNull();
        type.GetProperty("Response").ShouldNotBeNull();
        type.GetProperty("ScatterResults").ShouldNotBeNull();
        type.GetProperty("Strategy").ShouldNotBeNull();
        type.GetProperty("TotalDuration").ShouldNotBeNull();
        type.GetProperty("ScatterCount").ShouldNotBeNull();
        type.GetProperty("SuccessCount").ShouldNotBeNull();
        type.GetProperty("FailureCount").ShouldNotBeNull();
        type.GetProperty("AllSucceeded").ShouldNotBeNull();
        type.GetProperty("HasPartialFailures").ShouldNotBeNull();
    }

    [Fact]
    public void ScatterExecutionResult_HasRequiredProperties()
    {
        var type = typeof(ScatterExecutionResult<>);

        type.GetProperty("HandlerName").ShouldNotBeNull();
        type.GetProperty("Result").ShouldNotBeNull();
        type.GetProperty("Duration").ShouldNotBeNull();
        type.GetProperty("StartedAtUtc").ShouldNotBeNull();
        type.GetProperty("CompletedAtUtc").ShouldNotBeNull();
        type.GetProperty("IsSuccess").ShouldNotBeNull();
        type.GetProperty("IsFailure").ShouldNotBeNull();
    }

    [Fact]
    public void BuiltScatterGatherDefinition_HasRequiredProperties()
    {
        var type = typeof(BuiltScatterGatherDefinition<,>);

        type.GetProperty("Name").ShouldNotBeNull();
        type.GetProperty("ScatterHandlers").ShouldNotBeNull();
        type.GetProperty("GatherHandler").ShouldNotBeNull();
        type.GetProperty("Strategy").ShouldNotBeNull();
        type.GetProperty("Timeout").ShouldNotBeNull();
        type.GetProperty("QuorumCount").ShouldNotBeNull();
        type.GetProperty("ExecuteInParallel").ShouldNotBeNull();
        type.GetProperty("ScatterCount").ShouldNotBeNull();
    }

    [Fact]
    public void ScatterGatherOptions_HasDefaultValues()
    {
        var options = new ScatterGatherOptions();

        options.DefaultTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.ExecuteScattersInParallel.ShouldBeTrue();
        options.MaxDegreeOfParallelism.ShouldBe(Environment.ProcessorCount);
        options.DefaultGatherStrategy.ShouldBe(GatherStrategy.WaitForAll);
        options.DefaultQuorumCount.ShouldBeNull();
        options.IncludeFailedResultsInGather.ShouldBeFalse();
        options.CancelRemainingOnStrategyComplete.ShouldBeTrue();
    }

    [Fact]
    public void ScatterGatherErrorCodes_HasExpectedCodes()
    {
        ScatterGatherErrorCodes.NoScatterHandlers.ShouldBe("scattergather.no_scatter_handlers");
        ScatterGatherErrorCodes.GatherNotConfigured.ShouldBe("scattergather.gather_not_configured");
        ScatterGatherErrorCodes.ScatterFailed.ShouldBe("scattergather.scatter_failed");
        ScatterGatherErrorCodes.GatherFailed.ShouldBe("scattergather.gather_failed");
        ScatterGatherErrorCodes.Cancelled.ShouldBe("scattergather.cancelled");
        ScatterGatherErrorCodes.HandlerFailed.ShouldBe("scattergather.handler_failed");
        ScatterGatherErrorCodes.Timeout.ShouldBe("scattergather.timeout");
        ScatterGatherErrorCodes.AllScattersFailed.ShouldBe("scattergather.all_scatters_failed");
        ScatterGatherErrorCodes.QuorumNotReached.ShouldBe("scattergather.quorum_not_reached");
    }

    [Fact]
    public async Task ScatterGatherRunner_ReturnsEitherType()
    {
        var runner = new ScatterGatherRunner(new ScatterGatherOptions(), NullLogger<ScatterGatherRunner>.Instance);
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo(req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .GatherAll()
            .TakeFirst()
            .Build();

        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Verify it returns proper Either type
        result.ShouldBeOfType<Either<EncinaError, ScatterGatherResult<TestResponse>>>();
    }

    [Fact]
    public async Task ScatterGatherResult_ContainsAllScatterResults()
    {
        var runner = new ScatterGatherRunner(new ScatterGatherOptions(), NullLogger<ScatterGatherRunner>.Instance);
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("H1", req => Right<EncinaError, TestResponse>(new TestResponse(1)))
            .ScatterTo("H2", req => Right<EncinaError, TestResponse>(new TestResponse(2)))
            .ScatterTo("H3", req => Right<EncinaError, TestResponse>(new TestResponse(3)))
            .GatherAll()
            .TakeFirst()
            .Build();

        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        result.Match(
            Right: r =>
            {
                r.ScatterResults.Count.ShouldBe(3);
                r.ScatterResults.Select(s => s.HandlerName).ShouldContain("H1");
                r.ScatterResults.Select(s => s.HandlerName).ShouldContain("H2");
                r.ScatterResults.Select(s => s.HandlerName).ShouldContain("H3");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    public sealed record TestRequest(string Query);
    public sealed record TestResponse(decimal Value);
}
