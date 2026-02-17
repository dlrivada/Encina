using Encina.Sharding;
using Encina.Sharding.Shadow;
using Encina.Sharding.Shadow.Behaviors;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Core.Sharding.Shadow.Behaviors;

/// <summary>
/// Unit tests for <see cref="ShadowReadPipelineBehavior{TQuery, TResponse}"/>
/// validating percentage-based sampling, result comparison, and discrepancy handling.
/// </summary>
public sealed class ShadowReadPipelineBehaviorTests
{
    private readonly IShadowShardRouter _shadowRouter;
    private readonly ShadowShardingOptions _options;
    private readonly IRequestContext _context;

    public ShadowReadPipelineBehaviorTests()
    {
        _shadowRouter = Substitute.For<IShadowShardRouter>();
        _shadowRouter.IsShadowEnabled.Returns(true);
        _options = new ShadowShardingOptions
        {
            ShadowTopology = CreateTestTopology(),
            ShadowReadPercentage = 100, // Always sample for deterministic testing
            CompareResults = true,
            ShadowWriteTimeout = TimeSpan.FromSeconds(5)
        };
        _context = Substitute.For<IRequestContext>();
    }

    private ShadowReadPipelineBehavior<TestQuery, string> CreateBehavior() =>
        new(_shadowRouter, _options,
            NullLogger<ShadowReadPipelineBehavior<TestQuery, string>>.Instance);

    // ── Constructor validation ─────────────────────────────────────

    [Fact]
    public void Constructor_NullShadowRouter_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShadowReadPipelineBehavior<TestQuery, string>(
                null!, _options,
                NullLogger<ShadowReadPipelineBehavior<TestQuery, string>>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShadowReadPipelineBehavior<TestQuery, string>(
                _shadowRouter, null!,
                NullLogger<ShadowReadPipelineBehavior<TestQuery, string>>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShadowReadPipelineBehavior<TestQuery, string>(
                _shadowRouter, _options, null!));
    }

    // ── Short-circuit: shadow disabled ─────────────────────────────

    [Fact]
    public async Task Handle_ShadowDisabled_ExecutesProductionOnly()
    {
        // Arrange
        _shadowRouter.IsShadowEnabled.Returns(false);
        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        // Act
        var result = await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _shadowRouter.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Short-circuit: read percentage = 0 ─────────────────────────

    [Fact]
    public async Task Handle_ReadPercentageZero_ExecutesProductionOnly()
    {
        // Arrange
        _options.ShadowReadPercentage = 0;
        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        // Act
        var result = await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _shadowRouter.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Production success triggers shadow read ────────────────────

    [Fact]
    public async Task Handle_ProductionSucceeds_FiresShadowRead()
    {
        // Arrange
        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateComparisonResult(routingMatch: true)));

        // Act
        var result = await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);

        // Assert — production result returned immediately
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(v => v.ShouldBe("result"));

        // Give the fire-and-forget task time to complete
        await Task.Delay(100);
        await _shadowRouter.Received(1).CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Production failure skips shadow read ────────────────────────

    [Fact]
    public async Task Handle_ProductionFails_DoesNotFireShadowRead()
    {
        // Arrange
        var behavior = CreateBehavior();
        var productionError = LanguageExt.Prelude.Left<EncinaError, string>(
            EncinaErrors.Create("test.error", "Production failed."));
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionError);

        // Act
        var result = await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await Task.Delay(100);
        await _shadowRouter.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Shadow read errors don't propagate ─────────────────────────

    [Fact]
    public async Task Handle_ShadowReadThrows_DoesNotAffectProductionResult()
    {
        // Arrange
        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ShadowComparisonResult>>(_ => throw new InvalidOperationException("Shadow failed"));

        // Act — should NOT throw
        var result = await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(v => v.ShouldBe("result"));
    }

    // ── Discrepancy handler invocation ─────────────────────────────

    [Fact]
    public async Task Handle_RoutingMismatch_InvokesDiscrepancyHandler()
    {
        // Arrange
        var handlerInvoked = false;
        _options.DiscrepancyHandler = (_, _, _) =>
        {
            handlerInvoked = true;
            return Task.CompletedTask;
        };

        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateComparisonResult(routingMatch: false)));

        // Act
        await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);
        await Task.Delay(200);

        // Assert
        handlerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_DiscrepancyHandlerThrows_DoesNotAffectProductionResult()
    {
        // Arrange
        _options.DiscrepancyHandler = (_, _, _) =>
            throw new InvalidOperationException("Handler failed");

        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateComparisonResult(routingMatch: false)));

        // Act — should NOT throw
        var result = await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NoDiscrepancyHandler_DoesNotThrowOnMismatch()
    {
        // Arrange
        _options.DiscrepancyHandler = null;
        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateComparisonResult(routingMatch: false)));

        // Act — should NOT throw
        var result = await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ── CompareResults disabled skips comparison ───────────────────

    [Fact]
    public async Task Handle_CompareResultsDisabled_SkipsResultComparison()
    {
        // Arrange
        _options.CompareResults = false;
        var handlerInvoked = false;
        _options.DiscrepancyHandler = (_, _, _) =>
        {
            handlerInvoked = true;
            return Task.CompletedTask;
        };

        var behavior = CreateBehavior();
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("result");
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        // Even if routing doesn't match, with CompareResults=false, no comparison happens
        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateComparisonResult(routingMatch: false)));

        // Act
        await behavior.Handle(new TestQuery(), _context, next, CancellationToken.None);
        await Task.Delay(200);

        // Assert — handler not invoked because CompareResults is false
        handlerInvoked.ShouldBeFalse();
    }

    // ── Handle parameter validation ────────────────────────────────

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(
                LanguageExt.Prelude.Right<EncinaError, string>("ok"));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(null!, _context, next, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(
                LanguageExt.Prelude.Right<EncinaError, string>("ok"));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(new TestQuery(), null!, next, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(new TestQuery(), _context, null!, CancellationToken.None).AsTask());
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static ShardTopology CreateTestTopology()
    {
        var shards = new[]
        {
            new ShardInfo("shard-1", "Server=shadow;Database=Shard1"),
            new ShardInfo("shard-2", "Server=shadow;Database=Shard2")
        };
        return new ShardTopology(shards);
    }

    private static ShadowComparisonResult CreateComparisonResult(bool routingMatch) =>
        new(
            ShardKey: "test-key",
            ProductionShardId: "shard-1",
            ShadowShardId: routingMatch ? "shard-1" : "shard-2",
            RoutingMatch: routingMatch,
            ProductionLatency: TimeSpan.FromMilliseconds(1),
            ShadowLatency: TimeSpan.FromMilliseconds(2),
            ResultsMatch: null,
            ComparedAt: DateTimeOffset.UtcNow);

    /// <summary>
    /// Test query for pipeline behavior testing.
    /// </summary>
    public sealed class TestQuery : IQuery<string>;
}
