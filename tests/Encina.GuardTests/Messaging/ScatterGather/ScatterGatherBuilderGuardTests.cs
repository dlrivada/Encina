using Encina.Messaging.ScatterGather;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.ScatterGather;

/// <summary>
/// Guard clause tests for ScatterGatherBuilder fluent API, including parameter validation,
/// build-time validation, and edge cases.
/// </summary>
public class ScatterGatherBuilderGuardTests
{
    #region Static Create Guards

    [Fact]
    public void Create_NullName_ThrowsArgumentException()
    {
        var act = () => ScatterGatherBuilder.Create<TestRequest, TestResponse>(null!);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        var act = () => ScatterGatherBuilder.Create<TestRequest, TestResponse>(string.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => ScatterGatherBuilder.Create<TestRequest, TestResponse>("   ");
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_WithoutName_GeneratesAutoName()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>();
        builder.Should().NotBeNull();
    }

    #endregion

    #region ScatterTo Guards

    [Fact]
    public void ScatterTo_Named_NullName_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.ScatterTo((string)null!);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void ScatterTo_Named_EmptyName_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.ScatterTo(string.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void ScatterTo_InlineAsync_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        Func<TestRequest, CancellationToken, ValueTask<Either<EncinaError, TestResponse>>>? handler = null;
        var act = () => builder.ScatterTo(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void ScatterTo_InlineSync_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        Func<TestRequest, Either<EncinaError, TestResponse>>? handler = null;
        var act = () => builder.ScatterTo(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void ScatterTo_NamedAsync_NullName_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.ScatterTo(
            (string)null!,
            (req, ct) => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, TestResponse>(new TestResponse())));
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void ScatterTo_NamedAsync_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        Func<TestRequest, CancellationToken, ValueTask<Either<EncinaError, TestResponse>>>? handler = null;
        var act = () => builder.ScatterTo("Handler1", handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void ScatterTo_NamedSync_NullName_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.ScatterTo(
            (string)null!,
            (Func<TestRequest, Either<EncinaError, TestResponse>>)(req => LanguageExt.Prelude.Right<EncinaError, TestResponse>(new TestResponse())));
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void ScatterTo_NamedSync_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        Func<TestRequest, Either<EncinaError, TestResponse>>? handler = null;
        var act = () => builder.ScatterTo("Handler1", handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    #endregion

    #region ScatterBuilder.Execute Guards

    [Fact]
    public void ScatterBuilder_Execute_Async_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var scatterBuilder = builder.ScatterTo("Handler1");
        Func<TestRequest, CancellationToken, ValueTask<Either<EncinaError, TestResponse>>>? handler = null;
        var act = () => scatterBuilder.Execute(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void ScatterBuilder_Execute_Sync_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var scatterBuilder = builder.ScatterTo("Handler1");
        Func<TestRequest, Either<EncinaError, TestResponse>>? handler = null;
        var act = () => scatterBuilder.Execute(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void ScatterBuilder_Execute_AsyncPlain_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var scatterBuilder = builder.ScatterTo("Handler1");
        Func<TestRequest, CancellationToken, ValueTask<TestResponse>>? handler = null;
        var act = () => scatterBuilder.Execute(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void ScatterBuilder_Execute_SyncPlain_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var scatterBuilder = builder.ScatterTo("Handler1");
        Func<TestRequest, TestResponse>? handler = null;
        var act = () => scatterBuilder.Execute(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    #endregion

    #region WithTimeout Guards

    [Fact]
    public void WithTimeout_ZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.WithTimeout(TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("timeout");
    }

    [Fact]
    public void WithTimeout_NegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.WithTimeout(TimeSpan.FromSeconds(-1));
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("timeout");
    }

    [Fact]
    public void WithTimeout_PositiveTimeout_Succeeds()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.WithTimeout(TimeSpan.FromSeconds(10));
        act.Should().NotThrow();
    }

    #endregion

    #region GatherQuorum Guards

    [Fact]
    public void GatherQuorum_ZeroCount_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.GatherQuorum(0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quorumCount");
    }

    [Fact]
    public void GatherQuorum_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.GatherQuorum(-1);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quorumCount");
    }

    [Fact]
    public void GatherQuorum_ValidCount_Succeeds()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var gatherBuilder = builder.GatherQuorum(2);
        gatherBuilder.Should().NotBeNull();
    }

    #endregion

    #region ExecuteInParallel Guards

    [Fact]
    public void ExecuteInParallel_ZeroParallelism_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.ExecuteInParallel(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ExecuteInParallel_NegativeParallelism_ThrowsArgumentOutOfRangeException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.ExecuteInParallel(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ExecuteInParallel_NullParallelism_Succeeds()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.ExecuteInParallel(null);
        act.Should().NotThrow();
    }

    #endregion

    #region WithMetadata Guards

    [Fact]
    public void WithMetadata_NullKey_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.WithMetadata(null!, "value");
        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void WithMetadata_EmptyKey_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var act = () => builder.WithMetadata(string.Empty, "value");
        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void ScatterBuilder_WithMetadata_NullKey_ThrowsArgumentException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var scatterBuilder = builder.ScatterTo("Handler1");
        var act = () => scatterBuilder.WithMetadata(null!, "value");
        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    #endregion

    #region Build Validation

    [Fact]
    public void Build_NoScatterHandlers_ThrowsInvalidOperationException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        // Add gather but no scatter
        builder.GatherAll().TakeFirst();

        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*scatter handler*");
    }

    [Fact]
    public void Build_NoGatherHandler_ThrowsInvalidOperationException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        builder.ScatterTo("Handler1", (req, _) =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, TestResponse>(new TestResponse())));

        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*gather handler*");
    }

    [Fact]
    public void Build_QuorumExceedsScatterCount_ThrowsInvalidOperationException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        builder.ScatterTo("Handler1", (req, _) =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, TestResponse>(new TestResponse())));
        builder.GatherQuorum(5).TakeFirst();

        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Quorum count*");
    }

    [Fact]
    public void Build_ValidConfiguration_Succeeds()
    {
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", (req, _) =>
                ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, TestResponse>(new TestResponse())))
            .GatherAll()
            .TakeFirst()
            .Build();

        definition.Should().NotBeNull();
        definition.Name.Should().Be("Test");
        definition.ScatterHandlers.Should().HaveCount(1);
    }

    #endregion

    #region GatherBuilder Aggregate Guards

    [Fact]
    public void GatherBuilder_Aggregate_Async_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var gatherBuilder = builder.GatherAll();
        Func<IReadOnlyList<ScatterExecutionResult<TestResponse>>, CancellationToken, ValueTask<Either<EncinaError, TestResponse>>>? handler = null;
        var act = () => gatherBuilder.Aggregate(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void GatherBuilder_Aggregate_Sync_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var gatherBuilder = builder.GatherAll();
        Func<IReadOnlyList<ScatterExecutionResult<TestResponse>>, Either<EncinaError, TestResponse>>? handler = null;
        var act = () => gatherBuilder.Aggregate(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void GatherBuilder_AggregateSuccessful_Async_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var gatherBuilder = builder.GatherAll();
        Func<IEnumerable<TestResponse>, CancellationToken, ValueTask<Either<EncinaError, TestResponse>>>? handler = null;
        var act = () => gatherBuilder.AggregateSuccessful(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void GatherBuilder_AggregateSuccessful_Sync_NullHandler_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var gatherBuilder = builder.GatherAll();
        Func<IEnumerable<TestResponse>, Either<EncinaError, TestResponse>>? handler = null;
        var act = () => gatherBuilder.AggregateSuccessful(handler!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void GatherBuilder_TakeBest_NullSelector_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var gatherBuilder = builder.GatherAll();
        Func<TestResponse, int>? selector = null;
        var act = () => gatherBuilder.TakeBest(selector!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void GatherBuilder_TakeMax_NullSelector_ThrowsArgumentNullException()
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var gatherBuilder = builder.GatherAll();
        Func<TestResponse, int>? selector = null;
        var act = () => gatherBuilder.TakeMax(selector!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    #endregion

    private sealed class TestRequest
    {
        public string? Value { get; set; }
    }

    private sealed class TestResponse
    {
        public string? Result { get; set; }
    }
}
