using System.Data.Common;
using Encina.Caching;
using Encina.EntityFrameworkCore.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Encina.UnitTests.EntityFrameworkCore.Caching;

/// <summary>
/// Unit tests for <see cref="QueryCacheInterceptor"/>.
/// </summary>
public class QueryCacheInterceptorTests
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IQueryCacheKeyGenerator _keyGenerator;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryCacheInterceptor> _logger;

    public QueryCacheInterceptorTests()
    {
        _cacheProvider = Substitute.For<ICacheProvider>();
        _keyGenerator = Substitute.For<IQueryCacheKeyGenerator>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = Substitute.For<ILogger<QueryCacheInterceptor>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCacheProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCacheInterceptor(
            null!, _keyGenerator, CreateOptions(), _serviceProvider, _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cacheProvider");
    }

    [Fact]
    public void Constructor_WithNullKeyGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCacheInterceptor(
            _cacheProvider, null!, CreateOptions(), _serviceProvider, _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyGenerator");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCacheInterceptor(
            _cacheProvider, _keyGenerator, null!, _serviceProvider, _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCacheInterceptor(
            _cacheProvider, _keyGenerator, CreateOptions(), null!, _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCacheInterceptor(
            _cacheProvider, _keyGenerator, CreateOptions(), _serviceProvider, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithValidArgs_CreatesInstance()
    {
        // Act
        var interceptor = CreateInterceptor();

        // Assert
        interceptor.ShouldNotBeNull();
    }

    #endregion

    #region ShouldCache Tests

    [Fact]
    public void ReaderExecuting_WhenDisabled_DoesNotInterceptQuery()
    {
        // Arrange
        var interceptor = CreateInterceptor(enabled: false);
        var command = Substitute.For<DbCommand>();
        var eventData = CreateCommandEventData(hasContext: true);

        // Act
        var result = interceptor.ReaderExecuting(
            command, eventData, default);

        // Assert — should NOT have called key generator
        _keyGenerator.DidNotReceive().Generate(
            Arg.Any<DbCommand>(), Arg.Any<DbContext>());
    }

    [Fact]
    public void ReaderExecuting_WhenNoContext_DoesNotInterceptQuery()
    {
        // Arrange
        var interceptor = CreateInterceptor(enabled: true);
        var command = Substitute.For<DbCommand>();
        var eventData = CreateCommandEventData(hasContext: false);

        // Act
        var result = interceptor.ReaderExecuting(
            command, eventData, default);

        // Assert — should NOT have called key generator
        _keyGenerator.DidNotReceive().Generate(
            Arg.Any<DbCommand>(), Arg.Any<DbContext>());
    }

    #endregion

    #region Excluded Entity Types Tests

    [Fact]
    public void ReaderExecuting_ExcludedEntityType_SkipsCache()
    {
        // Arrange
        var options = new QueryCacheOptions { Enabled = true };
        options.ExcludeType<AuditLog>();

        var interceptor = CreateInterceptor(options: options);
        var command = Substitute.For<DbCommand>();
        var context = Substitute.For<DbContext>();
        var eventData = CreateCommandEventData(context);

        var cacheKey = new QueryCacheKey("test:key", ["AuditLog"]);
        _keyGenerator.Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>())
            .Returns(cacheKey);

        // Act
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert — should NOT have called cache provider
        _cacheProvider.DidNotReceive().GetAsync<CachedQueryResult>(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ReaderExecuting_CacheError_WhenThrowOnErrors_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new QueryCacheOptions
        {
            Enabled = true,
            ThrowOnCacheErrors = true
        };
        var interceptor = CreateInterceptor(options: options);

        var command = Substitute.For<DbCommand>();
        var context = Substitute.For<DbContext>();
        var eventData = CreateCommandEventData(context);

        var cacheKey = new QueryCacheKey("test:key", ["Order"]);
        _keyGenerator.Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>())
            .Returns(cacheKey);
        _cacheProvider.GetAsync<CachedQueryResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CachedQueryResult?>(new InvalidOperationException("Cache down")));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            interceptor.ReaderExecuting(command, eventData, default));
    }

    [Fact]
    public void ReaderExecuting_CacheError_WhenResilient_FallsThrough()
    {
        // Arrange
        var options = new QueryCacheOptions
        {
            Enabled = true,
            ThrowOnCacheErrors = false // resilient mode (default)
        };
        var interceptor = CreateInterceptor(options: options);

        var command = Substitute.For<DbCommand>();
        var context = Substitute.For<DbContext>();
        var eventData = CreateCommandEventData(context);

        var cacheKey = new QueryCacheKey("test:key", ["Order"]);
        _keyGenerator.Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>())
            .Returns(cacheKey);
        _cacheProvider.GetAsync<CachedQueryResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CachedQueryResult?>(new InvalidOperationException("Cache down")));

        // Act — should not throw
        var result = interceptor.ReaderExecuting(command, eventData, default);

        // Assert — result is the default (no suppression)
        result.HasResult.ShouldBeFalse();
    }

    #endregion

    #region SaveChanges Invalidation Tests

    [Fact]
    public void SaveChangesFailed_ClearsPendingInvalidations()
    {
        // Arrange
        var interceptor = CreateInterceptor(enabled: true);
        var eventData = Substitute.For<DbContextErrorEventData>(
            Substitute.For<EventDefinitionBase>(
                Substitute.For<ILoggingOptions>(),
                new EventId(1),
                LogLevel.Debug,
                "test"),
            Substitute.For<Func<EventDefinitionBase, EventData, string>>(),
            Substitute.For<DbContext>(),
            new InvalidOperationException("save failed"));

        // Act — should not throw
        interceptor.SaveChangesFailed(eventData);

        // Assert — cache invalidation should NOT have been called
        _cacheProvider.DidNotReceive().RemoveByPatternAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Test Helpers

    private QueryCacheInterceptor CreateInterceptor(
        bool enabled = true,
        QueryCacheOptions? options = null)
    {
        options ??= new QueryCacheOptions { Enabled = enabled };
        return new QueryCacheInterceptor(
            _cacheProvider,
            _keyGenerator,
            Options.Create(options),
            _serviceProvider,
            _logger);
    }

    private static IOptions<QueryCacheOptions> CreateOptions(bool enabled = true)
    {
        return Options.Create(new QueryCacheOptions { Enabled = enabled });
    }

    private static CommandEventData CreateCommandEventData(bool hasContext)
    {
        var context = hasContext ? Substitute.For<DbContext>() : null;
        return CreateCommandEventData(context);
    }

    private static CommandEventData CreateCommandEventData(DbContext? context)
    {
        var eventDefinition = Substitute.For<EventDefinitionBase>(
            Substitute.For<ILoggingOptions>(),
            new EventId(1),
            LogLevel.Debug,
            "test");

        var messageGenerator = Substitute.For<Func<EventDefinitionBase, EventData, string>>();

        return new CommandEventData(
            eventDefinition,
            messageGenerator,
            connection: Substitute.For<DbConnection>(),
            command: Substitute.For<DbCommand>(),
            logCommandText: "SELECT 1",
            context: context,
            executeMethod: DbCommandMethod.ExecuteReader,
            commandId: Guid.NewGuid(),
            connectionId: Guid.NewGuid(),
            async: false,
            logParameterValues: false,
            startTime: DateTimeOffset.UtcNow,
            commandSource: CommandSource.LinqQuery);
    }

    private sealed class AuditLog;

    #endregion
}
