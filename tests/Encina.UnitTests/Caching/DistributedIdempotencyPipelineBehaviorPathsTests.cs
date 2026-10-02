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
/// Unit tests for the key-claim, duplicate-result and result-storage paths of
/// <see cref="DistributedIdempotencyPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class DistributedIdempotencyPipelineBehaviorPathsTests
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();
    private readonly FakeLogger<DistributedIdempotencyPipelineBehavior<PathsCommand, string>> _logger = new();
    private readonly CachingOptions _cachingOptions = new()
    {
        EnableDistributedIdempotency = true,
        IdempotencyKeyPrefix = "idem",
        IdempotencyTtl = TimeSpan.FromHours(1),
        ThrowOnCacheErrors = false
    };

    [Fact]
    public async Task Handle_WhenNestedDispatchWithoutKey_RunsHandlerWithoutTouchingTheCache()
    {
        // Arrange
        var behavior = CreateBehavior();
        var context = CreateContext(idempotencyKey: null);
        context.Metadata.Returns(new Dictionary<string, object?> { ["Encina.NestedDispatch"] = true });
        var nextCalls = 0;

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), context, () => { nextCalls++; return ValueTask.FromResult(Right<EncinaError, string>("ok")); }, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("ok");
        nextCalls.ShouldBe(1);
        _cacheProvider.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCachedEntryIsSuccessful_ReturnsCachedResponseWithoutRunningHandler()
    {
        // Arrange
        _cacheProvider.GetAsync<IdempotencyEntry<string>>("idem:tenant-1:key-1", Arg.Any<CancellationToken>())
            .Returns(new IdempotencyEntry<string> { IsSuccess = true, Response = "cached" });
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => throw new InvalidOperationException("handler must not run"), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("cached");
        await _cacheProvider.DidNotReceiveWithAnyArgs().SetAsync<IdempotencyEntry<string>>(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCachedEntryHoldsAnError_ReturnsThatError()
    {
        // Arrange
        var error = EncinaErrors.Create("stored.failure", "stored");
        _cacheProvider.GetAsync<IdempotencyEntry<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyEntry<string> { HasError = true, Error = error });
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => throw new InvalidOperationException("handler must not run"), CancellationToken.None);

        // Assert
        result.ShouldBeErrorWithCode("stored.failure");
    }

    [Fact]
    public async Task Handle_WhenCachedEntryHasNoResultYet_ReturnsInProgressError()
    {
        // Arrange
        _cacheProvider.GetAsync<IdempotencyEntry<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyEntry<string> { IsSuccess = false });
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => throw new InvalidOperationException("handler must not run"), CancellationToken.None);

        // Assert
        result.ShouldBeErrorWithCode("idempotency.in_progress");
    }

    [Fact]
    public async Task Handle_OnFirstRequest_MarksInProgressThenStoresTheCompletedResult()
    {
        // Arrange
        var stored = new List<IdempotencyEntry<string>>();
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Do<IdempotencyEntry<string>>(stored.Add), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => ValueTask.FromResult(Right<EncinaError, string>("done")), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("done");
        stored.Count.ShouldBe(2);
        stored[0].IsSuccess.ShouldBeFalse();
        stored[0].CompletedAtUtc.ShouldBeNull();
        stored[1].IsSuccess.ShouldBeTrue();
        stored[1].Response.ShouldBe("done");
        stored[1].CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WhenHandlerFails_ReturnsTheHandlerError()
    {
        // Arrange
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("handler.failed", "m"))), CancellationToken.None);

        // Assert
        result.ShouldBeErrorWithCode("handler.failed");
    }

    [Fact]
    public async Task Handle_WhenClaimFailsAndErrorsAreSwallowed_RunsHandlerAndLogsRedactedException()
    {
        // Arrange
        _cacheProvider.GetAsync<IdempotencyEntry<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<IdempotencyEntry<string>?>>(_ => throw new InvalidOperationException(SentinelMessage));
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => ValueTask.FromResult(Right<EncinaError, string>("ok")), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("ok");
        AssertLoggedRedacted();
    }

    [Fact]
    public async Task Handle_WhenClaimFailsAndThrowOnCacheErrors_Rethrows()
    {
        // Arrange
        _cachingOptions.ThrowOnCacheErrors = true;
        _cacheProvider.GetAsync<IdempotencyEntry<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<IdempotencyEntry<string>?>>(_ => throw new InvalidOperationException(SentinelMessage));
        var behavior = CreateBehavior();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => ValueTask.FromResult(Right<EncinaError, string>("ok")), CancellationToken.None));
        AssertLoggedRedacted();
    }

    [Fact]
    public async Task Handle_WhenStoringTheResultFailsAndErrorsAreSwallowed_ReturnsTheHandlerResultAndLogsRedactedException()
    {
        // Arrange: the in-progress mark succeeds, the final store throws
        var setCalls = 0;
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<IdempotencyEntry<string>>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++setCalls == 1 ? Task.CompletedTask : throw new InvalidOperationException(SentinelMessage));
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => ValueTask.FromResult(Right<EncinaError, string>("ok")), CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe("ok");
        AssertLoggedRedacted();
    }

    [Fact]
    public async Task Handle_WhenStoringTheResultFailsAndThrowOnCacheErrors_Rethrows()
    {
        // Arrange
        _cachingOptions.ThrowOnCacheErrors = true;
        var setCalls = 0;
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<IdempotencyEntry<string>>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++setCalls == 1 ? Task.CompletedTask : throw new InvalidOperationException(SentinelMessage));
        var behavior = CreateBehavior();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await behavior.Handle(
            new PathsCommand("x"), CreateContext(), () => ValueTask.FromResult(Right<EncinaError, string>("ok")), CancellationToken.None));
    }

    private void AssertLoggedRedacted()
    {
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    private DistributedIdempotencyPipelineBehavior<PathsCommand, string> CreateBehavior() =>
        new(_cacheProvider, Options.Create(_cachingOptions), _logger);

    private static IRequestContext CreateContext(string? idempotencyKey = "key-1")
    {
        var context = Substitute.For<IRequestContext>();
        context.IdempotencyKey.Returns(idempotencyKey);
        context.TenantId.Returns("tenant-1");
        context.CorrelationId.Returns("corr-1");
        return context;
    }

    private sealed record PathsCommand(string Data) : IRequest<string>, IDistributedIdempotentRequest;
}
