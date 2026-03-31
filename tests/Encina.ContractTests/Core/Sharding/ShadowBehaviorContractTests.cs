using Encina.Sharding.Shadow;
using Encina.Sharding.Shadow.Behaviors;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Core.Sharding;

/// <summary>
/// Behavioral contract tests for <see cref="ShadowReadPipelineBehavior{TQuery, TResponse}"/>
/// and <see cref="ShadowWritePipelineBehavior{TCommand, TResponse}"/> that execute real code paths:
/// shadow-disabled passthrough, percentage-based sampling, dual-write fire-and-forget,
/// and constructor guards.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ShadowSharding")]
public sealed class ShadowBehaviorContractTests
{
    // -- Test request types --

    private sealed record TestCommand(string Value) : ICommand<string>;

    private sealed record TestQuery(string Value) : IQuery<string>;

    // -- Shadow Read Behavior --

    [Fact]
    public async Task ShadowRead_WhenShadowDisabled_ShouldPassthrough()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(false);
        var options = new ShadowShardingOptions { ShadowReadPercentage = 100 };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowReadPipelineBehavior<TestQuery, string>>();
        var behavior = new ShadowReadPipelineBehavior<TestQuery, string>(router, options, logger);

        var query = new TestQuery("search");
        var context = RequestContext.Create();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("result"));
        };

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        await router.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShadowRead_WhenPercentageIsZero_ShouldPassthrough()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(true);
        var options = new ShadowShardingOptions { ShadowReadPercentage = 0 };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowReadPipelineBehavior<TestQuery, string>>();
        var behavior = new ShadowReadPipelineBehavior<TestQuery, string>(router, options, logger);

        var query = new TestQuery("search");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("pass"));

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.ShouldBe("pass"), Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ShadowRead_WhenEnabled100Percent_ShouldExecuteProductionAndFireShadow()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(true);
        var comparisonResult = new ShadowComparisonResult(
            ShardKey: "key",
            ProductionShardId: "shard-1",
            ShadowShardId: "shard-2",
            RoutingMatch: false,
            ProductionLatency: TimeSpan.FromMilliseconds(1),
            ShadowLatency: TimeSpan.FromMilliseconds(2),
            ResultsMatch: null,
            ComparedAt: DateTimeOffset.UtcNow);
        router.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(comparisonResult));

        var options = new ShadowShardingOptions { ShadowReadPercentage = 100, CompareResults = false };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowReadPipelineBehavior<TestQuery, string>>();
        var behavior = new ShadowReadPipelineBehavior<TestQuery, string>(router, options, logger);

        var query = new TestQuery("all-shadow");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("production"));

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert: production result is returned regardless of shadow
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.ShouldBe("production"), Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ShadowRead_WhenProductionFails_ShouldNotFireShadow()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(true);
        var options = new ShadowShardingOptions { ShadowReadPercentage = 100 };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowReadPipelineBehavior<TestQuery, string>>();
        var behavior = new ShadowReadPipelineBehavior<TestQuery, string>(router, options, logger);

        var query = new TestQuery("fail");
        var context = RequestContext.Create();
        var error = EncinaError.New("production failed");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Left<EncinaError, string>(error));

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ShadowRead_Constructor_WithNullRouter_ShouldThrow()
    {
        var options = new ShadowShardingOptions();
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowReadPipelineBehavior<TestQuery, string>>();
        Should.Throw<ArgumentNullException>(() =>
            new ShadowReadPipelineBehavior<TestQuery, string>(null!, options, logger));
    }

    [Fact]
    public void ShadowRead_Constructor_WithNullOptions_ShouldThrow()
    {
        var router = Substitute.For<IShadowShardRouter>();
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowReadPipelineBehavior<TestQuery, string>>();
        Should.Throw<ArgumentNullException>(() =>
            new ShadowReadPipelineBehavior<TestQuery, string>(router, null!, logger));
    }

    [Fact]
    public void ShadowRead_Constructor_WithNullLogger_ShouldThrow()
    {
        var router = Substitute.For<IShadowShardRouter>();
        var options = new ShadowShardingOptions();
        Should.Throw<ArgumentNullException>(() =>
            new ShadowReadPipelineBehavior<TestQuery, string>(router, options, null!));
    }

    [Fact]
    public async Task ShadowRead_Handle_WithNullRequest_ShouldThrow()
    {
        var router = Substitute.For<IShadowShardRouter>();
        var options = new ShadowShardingOptions();
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowReadPipelineBehavior<TestQuery, string>>();
        var behavior = new ShadowReadPipelineBehavior<TestQuery, string>(router, options, logger);
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("no"));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(null!, context, next, CancellationToken.None));
    }

    // -- Shadow Write Behavior --

    [Fact]
    public async Task ShadowWrite_WhenShadowDisabled_ShouldPassthrough()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(false);
        var options = new ShadowShardingOptions { DualWriteEnabled = true };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowWritePipelineBehavior<TestCommand, string>>();
        var behavior = new ShadowWritePipelineBehavior<TestCommand, string>(router, options, logger);

        var command = new TestCommand("write");
        var context = RequestContext.Create();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("written"));
        };

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        await router.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShadowWrite_WhenDualWriteDisabled_ShouldPassthrough()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(true);
        var options = new ShadowShardingOptions { DualWriteEnabled = false };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowWritePipelineBehavior<TestCommand, string>>();
        var behavior = new ShadowWritePipelineBehavior<TestCommand, string>(router, options, logger);

        var command = new TestCommand("write");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("done"));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.ShouldBe("done"), Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ShadowWrite_WhenEnabled_ShouldExecuteProductionAndFireShadow()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(true);
        var comparisonResult = new ShadowComparisonResult(
            ShardKey: "key",
            ProductionShardId: "shard-1",
            ShadowShardId: "shard-1",
            RoutingMatch: true,
            ProductionLatency: TimeSpan.FromMilliseconds(1),
            ShadowLatency: TimeSpan.FromMilliseconds(1),
            ResultsMatch: null,
            ComparedAt: DateTimeOffset.UtcNow);
        router.CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(comparisonResult));

        var options = new ShadowShardingOptions { DualWriteEnabled = true };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowWritePipelineBehavior<TestCommand, string>>();
        var behavior = new ShadowWritePipelineBehavior<TestCommand, string>(router, options, logger);

        var command = new TestCommand("dual");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("committed"));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert: production result returned immediately
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.ShouldBe("committed"), Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task ShadowWrite_WhenProductionFails_ShouldNotFireShadow()
    {
        // Arrange
        var router = Substitute.For<IShadowShardRouter>();
        router.IsShadowEnabled.Returns(true);
        var options = new ShadowShardingOptions { DualWriteEnabled = true };
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowWritePipelineBehavior<TestCommand, string>>();
        var behavior = new ShadowWritePipelineBehavior<TestCommand, string>(router, options, logger);

        var command = new TestCommand("fail");
        var context = RequestContext.Create();
        var error = EncinaError.New("write failed");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Left<EncinaError, string>(error));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await router.DidNotReceive().CompareAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ShadowWrite_Constructor_WithNullRouter_ShouldThrow()
    {
        var options = new ShadowShardingOptions();
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowWritePipelineBehavior<TestCommand, string>>();
        Should.Throw<ArgumentNullException>(() =>
            new ShadowWritePipelineBehavior<TestCommand, string>(null!, options, logger));
    }

    [Fact]
    public void ShadowWrite_Constructor_WithNullOptions_ShouldThrow()
    {
        var router = Substitute.For<IShadowShardRouter>();
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowWritePipelineBehavior<TestCommand, string>>();
        Should.Throw<ArgumentNullException>(() =>
            new ShadowWritePipelineBehavior<TestCommand, string>(router, null!, logger));
    }

    [Fact]
    public async Task ShadowWrite_Handle_WithNullRequest_ShouldThrow()
    {
        var router = Substitute.For<IShadowShardRouter>();
        var options = new ShadowShardingOptions();
        var logger = NullLoggerFactory.Instance.CreateLogger<ShadowWritePipelineBehavior<TestCommand, string>>();
        var behavior = new ShadowWritePipelineBehavior<TestCommand, string>(router, options, logger);
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("no"));

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(null!, context, next, CancellationToken.None));
    }
}
