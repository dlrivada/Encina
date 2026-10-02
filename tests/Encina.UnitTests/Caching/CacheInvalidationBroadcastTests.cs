using Encina.Caching;
using Encina.Diagnostics;
using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Caching;

/// <summary>
/// Unit tests for the pub/sub broadcast step of <see cref="CacheInvalidationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class CacheInvalidationBroadcastTests
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();
    private readonly IPubSubProvider _pubSub = Substitute.For<IPubSubProvider>();
    private readonly ICacheKeyGenerator _keyGenerator = Substitute.For<ICacheKeyGenerator>();
    private readonly FakeLogger<CacheInvalidationPipelineBehavior<BroadcastCommand, string>> _logger = new();
    private readonly CachingOptions _options = new()
    {
        EnableCacheInvalidation = true,
        EnablePubSubInvalidation = true,
        InvalidationChannel = "cache:invalidate",
        ThrowOnCacheErrors = false
    };

    public CacheInvalidationBroadcastTests()
    {
        _keyGenerator.GeneratePatternFromTemplate(Arg.Any<string>(), Arg.Any<BroadcastCommand>(), Arg.Any<IRequestContext>())
            .Returns("items:*");
    }

    [Fact]
    public async Task Handle_WhenBroadcastSucceeds_PublishesThePatternOnTheInvalidationChannel()
    {
        // Act
        var result = await CreateBehavior().Handle(new BroadcastCommand(), CreateContext(), Success, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("ok");
        await _pubSub.Received(1).PublishAsync("cache:invalidate", "items:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPubSubIsDisabledInOptions_DoesNotPublish()
    {
        // Arrange
        _options.EnablePubSubInvalidation = false;

        // Act
        await CreateBehavior().Handle(new BroadcastCommand(), CreateContext(), Success, CancellationToken.None);

        // Assert
        await _pubSub.DidNotReceiveWithAnyArgs().PublishAsync(default!, default(string)!, default);
        await _cacheProvider.Received(1).RemoveByPatternAsync("items:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoPubSubProviderIsRegistered_StillInvalidatesLocally()
    {
        // Arrange
        var behavior = new CacheInvalidationPipelineBehavior<BroadcastCommand, string>(
            _cacheProvider, _keyGenerator, Options.Create(_options), _logger);

        // Act
        var result = await behavior.Handle(new BroadcastCommand(), CreateContext(), Success, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("ok");
        await _cacheProvider.Received(1).RemoveByPatternAsync("items:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBroadcastFailsAndErrorsAreSwallowed_ReturnsTheResultAndLogsRedactedException()
    {
        // Arrange
        _pubSub.PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException(SentinelMessage));

        // Act
        var result = await CreateBehavior().Handle(new BroadcastCommand(), CreateContext(), Success, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("ok");
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    [Fact]
    public async Task Handle_WhenBroadcastFailsAndThrowOnCacheErrors_PropagatesTheFailure()
    {
        // Arrange
        _options.ThrowOnCacheErrors = true;
        _pubSub.PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException(SentinelMessage));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await CreateBehavior().Handle(new BroadcastCommand(), CreateContext(), Success, CancellationToken.None));
    }

    private static ValueTask<Either<EncinaError, string>> Success() =>
        ValueTask.FromResult(Right<EncinaError, string>("ok"));

    private CacheInvalidationPipelineBehavior<BroadcastCommand, string> CreateBehavior() =>
        new(_cacheProvider, _keyGenerator, Options.Create(_options), _logger, _pubSub);

    private static IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-1");
        return context;
    }

    [InvalidatesCache("items:{Id}:*", BroadcastInvalidation = true)]
    private sealed record BroadcastCommand : IRequest<string>;
}
