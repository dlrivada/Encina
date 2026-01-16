using Encina.Caching;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Caching;

/// <summary>
/// Unit tests for <see cref="DistributedIdempotencyPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class DistributedIdempotencyPipelineBehaviorTests
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IOptions<CachingOptions> _options;
    private readonly ILogger<DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string>> _logger;
    private readonly CachingOptions _cachingOptions;

    public DistributedIdempotencyPipelineBehaviorTests()
    {
        _cacheProvider = Substitute.For<ICacheProvider>();
        _logger = NullLogger<DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string>>.Instance;
        _cachingOptions = new CachingOptions
        {
            EnableDistributedIdempotency = true,
            IdempotencyKeyPrefix = "sm:idempotency",
            IdempotencyTtl = TimeSpan.FromHours(24),
            ThrowOnCacheErrors = false
        };
        _options = Options.Create(_cachingOptions);
    }

    // Note: Test types are defined at file scope at the end of this file

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCacheProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string>(null!, _options, _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string>(_cacheProvider, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string>(_cacheProvider, _options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var behavior = CreateBehavior();

        // Assert
        behavior.ShouldNotBeNull();
    }

    #endregion

    #region Handle Tests - Bypass Cases

    [Fact]
    public async Task Handle_WhenIdempotencyDisabled_CallsNextStep()
    {
        // Arrange
        var disabledOptions = Options.Create(new CachingOptions
        {
            EnableDistributedIdempotency = false
        });
        var behavior = new DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string>(
            _cacheProvider, disabledOptions, _logger);
        var command = new IdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: "key-123");
        var nextStepCalled = false;

        // Act
        var result = await behavior.Handle(
            command,
            context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            },
            CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenRequestNotIdempotent_CallsNextStep()
    {
        // Arrange
        var behaviorForNonIdempotent = new DistributedIdempotencyPipelineBehavior<NonIdempotentTestCommand, string>(
            _cacheProvider,
            _options,
            NullLogger<DistributedIdempotencyPipelineBehavior<NonIdempotentTestCommand, string>>.Instance);
        var command = new NonIdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: "key-123");
        var nextStepCalled = false;

        // Act
        var result = await behaviorForNonIdempotent.Handle(
            command,
            context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            },
            CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Handle Tests - Missing Idempotency Key

    [Fact]
    public async Task Handle_WhenIdempotencyKeyMissing_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new IdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: null);

        // Act
        var result = await behavior.Handle(
            command,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var errorCode = error.GetCode().IfNone(string.Empty);
            errorCode.ShouldBe("idempotency.missing_key");
        });
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyEmpty_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new IdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: string.Empty);

        // Act
        var result = await behavior.Handle(
            command,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyWhitespace_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new IdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: "   ");

        // Act
        var result = await behavior.Handle(
            command,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle Tests - First Request Processing

    [Fact]
    public async Task Handle_WhenFirstRequest_ExecutesHandler()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new IdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: "new-key", tenantId: "tenant-1");

        var handlerExecuted = false;

        // Act
        var result = await behavior.Handle(
            command,
            context,
            () =>
            {
                handlerExecuted = true;
                return ValueTask.FromResult(Right<EncinaError, string>("handler-result"));
            },
            CancellationToken.None);

        // Assert
        handlerExecuted.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.ShouldBe("handler-result"));
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsError_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new IdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: "error-new-key", tenantId: "tenant-1");

        var handlerError = EncinaErrors.Create("handler.error", "Handler failed");

        // Act
        var result = await behavior.Handle(
            command,
            context,
            () => ValueTask.FromResult(Left<EncinaError, string>(handlerError)),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var errorCode = error.GetCode().IfNone(string.Empty);
            errorCode.ShouldBe("handler.error");
        });
    }

    #endregion

    #region Handle Tests - Argument Validation

    [Fact]
    public async Task Handle_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var context = CreateRequestContext(idempotencyKey: "key");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(
                null!,
                context,
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new IdempotentTestCommand("test");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(
                command,
                null!,
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var command = new IdempotentTestCommand("test");
        var context = CreateRequestContext(idempotencyKey: "key");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(
                command,
                context,
                null!,
                CancellationToken.None).AsTask());
    }

    #endregion

    #region Helper Methods

    private DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string> CreateBehavior()
    {
        return new DistributedIdempotencyPipelineBehavior<IdempotentTestCommand, string>(
            _cacheProvider, _options, _logger);
    }

    private static IRequestContext CreateRequestContext(
        string? idempotencyKey = "default-key",
        string tenantId = "tenant-1",
        string correlationId = "corr-123")
    {
        var context = Substitute.For<IRequestContext>();
        context.IdempotencyKey.Returns(idempotencyKey);
        context.TenantId.Returns(tenantId);
        context.CorrelationId.Returns(correlationId);
        return context;
    }

    #endregion
}

/// <summary>
/// Test command that implements IDistributedIdempotentRequest.
/// </summary>
internal sealed record IdempotentTestCommand(string Data) : IRequest<string>, IDistributedIdempotentRequest;

/// <summary>
/// Test command that does NOT implement IDistributedIdempotentRequest.
/// </summary>
internal sealed record NonIdempotentTestCommand(string Data) : IRequest<string>;
