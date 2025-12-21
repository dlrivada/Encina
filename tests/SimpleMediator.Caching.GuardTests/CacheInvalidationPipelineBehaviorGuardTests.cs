namespace SimpleMediator.Caching.GuardTests;

/// <summary>
/// Guard tests for <see cref="CacheInvalidationPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class CacheInvalidationPipelineBehaviorGuardTests
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>> _logger;

    public CacheInvalidationPipelineBehaviorGuardTests()
    {
        _cacheProvider = Substitute.For<ICacheProvider>();
        _keyGenerator = Substitute.For<ICacheKeyGenerator>();
        _logger = NullLogger<CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>>.Instance;
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when cacheProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullCacheProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions());

        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>(
            null!,
            _keyGenerator,
            options,
            _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cacheProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when keyGenerator is null.
    /// </summary>
    [Fact]
    public void Constructor_NullKeyGenerator_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions());

        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>(
            _cacheProvider,
            null!,
            options,
            _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("keyGenerator");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            null!,
            _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions());

        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { EnableCacheInvalidation = true });
        var behavior = new CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            _logger);
        var context = CreateContext();
        TestInvalidatingCommand request = null!;

        // Act & Assert
        var act = async () => await behavior.Handle(
            request,
            context,
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<MediatorError, string>("result")),
            CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { EnableCacheInvalidation = true });
        var behavior = new CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            _logger);
        var request = new TestInvalidatingCommand("test");
        IRequestContext context = null!;

        // Act & Assert
        var act = async () => await behavior.Handle(
            request,
            context,
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<MediatorError, string>("result")),
            CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { EnableCacheInvalidation = true });
        var behavior = new CacheInvalidationPipelineBehavior<TestInvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            _logger);
        var request = new TestInvalidatingCommand("test");
        var context = CreateContext();

        // Act & Assert
        var act = async () => await behavior.Handle(
            request,
            context,
            null!,
            CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("nextStep");
    }

    private static IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns("tenant1");
        context.UserId.Returns("user1");
        context.CorrelationId.Returns("corr-123");
        return context;
    }

    [InvalidatesCache("test:*")]
    private sealed record TestInvalidatingCommand(string Value) : IRequest<string>;
}
