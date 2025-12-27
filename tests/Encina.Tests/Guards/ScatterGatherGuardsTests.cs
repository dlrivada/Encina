using Encina.Messaging.ScatterGather;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Guards;

public sealed class ScatterGatherGuardsTests
{
    [Fact]
    public void ScatterGatherBuilder_Create_NullName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            ScatterGatherBuilder.Create<TestRequest, TestResponse>(null!));
    }

    [Fact]
    public void ScatterGatherBuilder_Create_EmptyName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            ScatterGatherBuilder.Create<TestRequest, TestResponse>(""));
    }

    [Fact]
    public void ScatterGatherBuilder_Create_WhitespaceName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            ScatterGatherBuilder.Create<TestRequest, TestResponse>("   "));
    }

    [Fact]
    public void ScatterGatherBuilder_ScatterTo_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<ArgumentNullException>(() =>
            builder.ScatterTo((Func<TestRequest, CancellationToken, ValueTask<Either<EncinaError, TestResponse>>>)null!));
    }

    [Fact]
    public void ScatterGatherBuilder_ScatterTo_NullName_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<ArgumentException>(() =>
            builder.ScatterTo((string)null!));
    }

    [Fact]
    public void ScatterGatherBuilder_WithTimeout_Zero_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<ArgumentOutOfRangeException>(() =>
            builder.WithTimeout(TimeSpan.Zero));
    }

    [Fact]
    public void ScatterGatherBuilder_WithTimeout_Negative_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<ArgumentOutOfRangeException>(() =>
            builder.WithTimeout(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void ScatterGatherBuilder_GatherQuorum_Zero_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<ArgumentOutOfRangeException>(() =>
            builder.GatherQuorum(0));
    }

    [Fact]
    public void ScatterGatherBuilder_GatherQuorum_Negative_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<ArgumentOutOfRangeException>(() =>
            builder.GatherQuorum(-1));
    }

    [Fact]
    public void ScatterGatherBuilder_ExecuteInParallel_ZeroDegree_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<ArgumentOutOfRangeException>(() =>
            builder.ExecuteInParallel(0));
    }

    [Fact]
    public void ScatterGatherBuilder_Build_NoHandlers_ThrowsInvalidOperationException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void ScatterGatherBuilder_Build_NoGatherHandler_ThrowsInvalidOperationException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo(req => Right<EncinaError, TestResponse>(new TestResponse(100)));

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void ScatterGatherBuilder_Build_QuorumExceedsHandlers_ThrowsInvalidOperationException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo(req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .GatherQuorum(5)
            .TakeFirst();

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public async Task ScatterGatherRunner_ExecuteAsync_NullDefinition_ThrowsArgumentNullException()
    {
        var runner = new ScatterGatherRunner(new ScatterGatherOptions(), NullLogger<ScatterGatherRunner>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(() =>
            runner.ExecuteAsync<TestRequest, TestResponse>(null!, new TestRequest("test")).AsTask());
    }

    [Fact]
    public async Task ScatterGatherRunner_ExecuteAsync_NullRequest_ThrowsArgumentNullException()
    {
        var runner = new ScatterGatherRunner(new ScatterGatherOptions(), NullLogger<ScatterGatherRunner>.Instance);
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo(req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .GatherAll()
            .TakeFirst()
            .Build();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            runner.ExecuteAsync(definition, null!).AsTask());
    }

    [Fact]
    public void ScatterDefinition_NullName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ScatterDefinition<TestRequest, TestResponse>(
                null!,
                (req, ct) => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse(100)))));
    }

    [Fact]
    public void ScatterDefinition_NullHandler_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ScatterDefinition<TestRequest, TestResponse>("Test", null!));
    }

    [Fact]
    public void BuiltScatterGatherDefinition_NullName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BuiltScatterGatherDefinition<TestRequest, TestResponse>(
                null!,
                [new ScatterDefinition<TestRequest, TestResponse>("H", (r, c) => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse(0))))],
                (r, c) => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse(0))),
                GatherStrategy.WaitForAll,
                null, null, true, null, null));
    }

    [Fact]
    public void BuiltScatterGatherDefinition_EmptyHandlers_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BuiltScatterGatherDefinition<TestRequest, TestResponse>(
                "Test",
                [],
                (r, c) => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse(0))),
                GatherStrategy.WaitForAll,
                null, null, true, null, null));
    }

    public sealed record TestRequest(string Query);
    public sealed record TestResponse(decimal Value);
}
