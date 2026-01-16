namespace Encina.GuardTests.Infrastructure.Caching;

/// <summary>
/// Guard tests for <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class QueryCachingPipelineBehaviorGuardTests
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<QueryCachingPipelineBehavior<TestCachedQuery, string>> _logger;

    public QueryCachingPipelineBehaviorGuardTests()
    {
        _cacheProvider = Substitute.For<ICacheProvider>();
        _keyGenerator = Substitute.For<ICacheKeyGenerator>();
        _logger = NullLogger<QueryCachingPipelineBehavior<TestCachedQuery, string>>.Instance;
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
        var act = () => new QueryCachingPipelineBehavior<TestCachedQuery, string>(
            null!,
            _keyGenerator,
            options,
            _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cacheProvider");
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
        var act = () => new QueryCachingPipelineBehavior<TestCachedQuery, string>(
            _cacheProvider,
            null!,
            options,
            _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyGenerator");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCachingPipelineBehavior<TestCachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            null!,
            _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
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
        var act = () => new QueryCachingPipelineBehavior<TestCachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { EnableQueryCaching = true });
        var behavior = new QueryCachingPipelineBehavior<TestCachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            _logger);
        var context = CreateContext();
        TestCachedQuery request = null!;

        // Act & Assert
        var act = () => behavior.Handle(
            request,
            context,
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("result")),
            CancellationToken.None).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe(nameof(request));
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { EnableQueryCaching = true });
        var behavior = new QueryCachingPipelineBehavior<TestCachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            _logger);
        var request = new TestCachedQuery("test");
        IRequestContext context = null!;

        // Act & Assert
        var act = () => behavior.Handle(
            request,
            context,
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("result")),
            CancellationToken.None).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe(nameof(context));
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when nextStep is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new CachingOptions { EnableQueryCaching = true });
        var behavior = new QueryCachingPipelineBehavior<TestCachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            options,
            _logger);
        var request = new TestCachedQuery("test");
        var context = CreateContext();

        // Act & Assert
        var act = () => behavior.Handle(
            request,
            context,
            null!,
            CancellationToken.None).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("nextStep");
    }

    private static IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns("tenant1");
        context.UserId.Returns("user1");
        context.CorrelationId.Returns("corr-123");
        return context;
    }

    [Cache(DurationSeconds = 300)]
    private sealed record TestCachedQuery(string Value) : IRequest<string>;
}
