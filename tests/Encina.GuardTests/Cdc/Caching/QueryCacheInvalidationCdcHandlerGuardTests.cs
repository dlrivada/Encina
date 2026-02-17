using Encina.Caching;
using Encina.Cdc.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Cdc.Caching;

/// <summary>
/// Guard clause tests for <see cref="QueryCacheInvalidationCdcHandler"/> constructor parameters.
/// Verifies that null checks throw <see cref="ArgumentNullException"/> with correct parameter names.
/// </summary>
public sealed class QueryCacheInvalidationCdcHandlerGuardTests
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IPubSubProvider _pubSubProvider;
    private readonly IOptions<QueryCacheInvalidationOptions> _options;
    private readonly Microsoft.Extensions.Logging.ILogger<QueryCacheInvalidationCdcHandler> _logger;

    public QueryCacheInvalidationCdcHandlerGuardTests()
    {
        _cacheProvider = Substitute.For<ICacheProvider>();
        _pubSubProvider = Substitute.For<IPubSubProvider>();
        _options = Options.Create(new QueryCacheInvalidationOptions());
        _logger = NullLogger<QueryCacheInvalidationCdcHandler>.Instance;
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that a null <c>cacheProvider</c> throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullCacheProvider_ShouldThrowArgumentNullException()
    {
        var act = () => new QueryCacheInvalidationCdcHandler(null!, _pubSubProvider, _options, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cacheProvider");
    }

    /// <summary>
    /// Verifies that a null <c>options</c> throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        var act = () => new QueryCacheInvalidationCdcHandler(_cacheProvider, _pubSubProvider, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that a null <c>logger</c> throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        var act = () => new QueryCacheInvalidationCdcHandler(_cacheProvider, _pubSubProvider, _options, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that a null <c>pubSubProvider</c> is allowed (it's optional).
    /// </summary>
    [Fact]
    public void Constructor_NullPubSubProvider_ShouldNotThrow()
    {
        var act = () => new QueryCacheInvalidationCdcHandler(_cacheProvider, null, _options, _logger);

        Should.NotThrow(act);
    }

    #endregion
}
