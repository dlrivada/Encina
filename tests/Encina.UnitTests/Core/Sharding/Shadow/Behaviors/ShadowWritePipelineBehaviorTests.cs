using Encina.Sharding;
using Encina.Sharding.Shadow;
using Encina.Sharding.Shadow.Behaviors;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Core.Sharding.Shadow.Behaviors;

/// <summary>
/// Unit tests for <see cref="ShadowWritePipelineBehavior{TCommand, TResponse}"/>
/// validating dual-write behavior, fire-and-forget semantics, and error isolation.
/// </summary>
public sealed class ShadowWritePipelineBehaviorTests
{
    private readonly IShadowShardRouter _shadowRouter;
    private readonly ShadowShardingOptions _options;
    private readonly ShadowWritePipelineBehavior<TestCommand, string> _behavior;
    private readonly IRequestContext _context;

    public ShadowWritePipelineBehaviorTests()
    {
        _shadowRouter = Substitute.For<IShadowShardRouter>();
        _shadowRouter.IsShadowEnabled.Returns(true);
        _options = new ShadowShardingOptions
        {
            ShadowTopology = CreateTestTopology(),
            DualWriteEnabled = true,
            ShadowWriteTimeout = TimeSpan.FromSeconds(5)
        };
        _behavior = new ShadowWritePipelineBehavior<TestCommand, string>(
            _shadowRouter,
            _options,
            NullLogger<ShadowWritePipelineBehavior<TestCommand, string>>.Instance);
        _context = Substitute.For<IRequestContext>();
    }

    // ── Constructor validation ─────────────────────────────────────

    [Fact]
    public void Constructor_NullShadowRouter_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShadowWritePipelineBehavior<TestCommand, string>(
                null!, _options,
                NullLogger<ShadowWritePipelineBehavior<TestCommand, string>>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShadowWritePipelineBehavior<TestCommand, string>(
                _shadowRouter, null!,
                NullLogger<ShadowWritePipelineBehavior<TestCommand, string>>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShadowWritePipelineBehavior<TestCommand, string>(
                _shadowRouter, _options, null!));
    }

    // ── Short-circuit: shadow disabled ─────────────────────────────

    [Fact]
    public async Task Handle_ShadowDisabled_ExecutesProductionOnly()
    {
        // Arrange
        _shadowRouter.IsShadowEnabled.Returns(false);
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("success");
        RequestHandlerCallback<string> next = () => new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        // Act
        var result = await _behavior.Handle(new TestCommand(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _shadowRouter.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Short-circuit: dual-write disabled ─────────────────────────

    [Fact]
    public async Task Handle_DualWriteDisabled_ExecutesProductionOnly()
    {
        // Arrange
        _options.DualWriteEnabled = false;
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("success");
        RequestHandlerCallback<string> next = () => new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        // Act
        var result = await _behavior.Handle(new TestCommand(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _shadowRouter.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Production success triggers shadow write ───────────────────

    [Fact]
    public async Task Handle_ProductionSucceeds_FiresShadowWrite()
    {
        // Arrange
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("success");
        RequestHandlerCallback<string> next = () => new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateComparisonResult()));

        // Act
        var result = await _behavior.Handle(new TestCommand(), _context, next, CancellationToken.None);

        // Assert — production result returned immediately
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(v => v.ShouldBe("success"));

        // Give the fire-and-forget task time to complete
        await Task.Delay(100);

        await _shadowRouter.Received(1).CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Production failure skips shadow write ──────────────────────

    [Fact]
    public async Task Handle_ProductionFails_DoesNotFireShadowWrite()
    {
        // Arrange
        var productionError = LanguageExt.Prelude.Left<EncinaError, string>(
            EncinaErrors.Create("test.error", "Production failed."));
        RequestHandlerCallback<string> next = () => new ValueTask<LanguageExt.Either<EncinaError, string>>(productionError);

        // Act
        var result = await _behavior.Handle(new TestCommand(), _context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();

        await Task.Delay(100);
        await _shadowRouter.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Shadow write errors don't propagate ────────────────────────

    [Fact]
    public async Task Handle_ShadowWriteThrows_DoesNotAffectProductionResult()
    {
        // Arrange
        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("success");
        RequestHandlerCallback<string> next = () => new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ShadowComparisonResult>>(_ => throw new InvalidOperationException("Shadow failed"));

        // Act — should NOT throw
        var result = await _behavior.Handle(new TestCommand(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(v => v.ShouldBe("success"));
    }

    // ── Shadow write timeout is handled ────────────────────────────

    [Fact]
    public async Task Handle_ShadowWriteTimesOut_DoesNotAffectProductionResult()
    {
        // Arrange
        _options.ShadowWriteTimeout = TimeSpan.FromMilliseconds(50);
        var behavior = new ShadowWritePipelineBehavior<TestCommand, string>(
            _shadowRouter, _options,
            NullLogger<ShadowWritePipelineBehavior<TestCommand, string>>.Instance);

        var productionResult = LanguageExt.Prelude.Right<EncinaError, string>("success");
        RequestHandlerCallback<string> next = () => new ValueTask<LanguageExt.Either<EncinaError, string>>(productionResult);

        _shadowRouter.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.ArgAt<CancellationToken>(1);
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return CreateComparisonResult();
            });

        // Act — should NOT throw
        var result = await behavior.Handle(new TestCommand(), _context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(v => v.ShouldBe("success"));
    }

    // ── Handle parameter validation ────────────────────────────────

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(
                LanguageExt.Prelude.Right<EncinaError, string>("ok"));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            _behavior.Handle(null!, _context, next, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        RequestHandlerCallback<string> next = () =>
            new ValueTask<LanguageExt.Either<EncinaError, string>>(
                LanguageExt.Prelude.Right<EncinaError, string>("ok"));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            _behavior.Handle(new TestCommand(), null!, next, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _behavior.Handle(new TestCommand(), _context, null!, CancellationToken.None).AsTask());
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

    private static ShadowComparisonResult CreateComparisonResult() =>
        new(
            ShardKey: "test-key",
            ProductionShardId: "shard-1",
            ShadowShardId: "shard-1",
            RoutingMatch: true,
            ProductionLatency: TimeSpan.FromMilliseconds(1),
            ShadowLatency: TimeSpan.FromMilliseconds(2),
            ResultsMatch: null,
            ComparedAt: DateTimeOffset.UtcNow);

    /// <summary>
    /// Test command for pipeline behavior testing.
    /// </summary>
    public sealed class TestCommand : ICommand<string>;
}
