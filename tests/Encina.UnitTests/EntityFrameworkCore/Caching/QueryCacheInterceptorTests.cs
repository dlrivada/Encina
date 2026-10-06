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

    #region Request Context Resolution Tests (#1147)

    [Fact]
    public void ReaderExecuting_WithAmbientRequestContextOnly_GeneratesATenantAwareKeyFromIt()
    {
        // Arrange
        var ambient = RequestContext.CreateForTest(tenantId: "ambient-tenant");
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(ambient);
        _serviceProvider.GetService(typeof(IRequestContextAccessor)).Returns(accessor);

        var (interceptor, command, eventData) = ArrangeCacheMiss();

        // Act
        interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _keyGenerator.Received(1).Generate(command, eventData.Context!, ambient);
    }

    [Fact]
    public void ReaderExecuting_WithAmbientAndRegisteredRequestContext_TheAmbientOneWins()
    {
        // Arrange
        var ambient = RequestContext.CreateForTest(tenantId: "ambient-tenant");
        var registered = RequestContext.CreateForTest(tenantId: "registered-tenant");
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(ambient);
        _serviceProvider.GetService(typeof(IRequestContextAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(IRequestContext)).Returns(registered);

        var (interceptor, command, eventData) = ArrangeCacheMiss();

        // Act
        interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _keyGenerator.Received(1).Generate(command, eventData.Context!, ambient);
        _keyGenerator.DidNotReceive().Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>(), registered);
    }

    [Fact]
    public void ReaderExecuting_WithAnEmptyAccessor_IgnoresARegisteredRequestContext()
    {
        // Arrange: the accessor is the only source (#1705 Phase 3: no DI fallback)
        var registered = RequestContext.CreateForTest(tenantId: "registered-tenant");
        var emptyAccessor = Substitute.For<IRequestContextAccessor>();
        emptyAccessor.RequestContext.Returns((IRequestContext?)null);
        _serviceProvider.GetService(typeof(IRequestContextAccessor)).Returns(emptyAccessor);
        _serviceProvider.GetService(typeof(IRequestContext)).Returns(registered);

        var (interceptor, command, eventData) = ArrangeCacheMiss();
        _keyGenerator.Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>())
            .Returns(new QueryCacheKey("key", ["Order"]));

        // Act
        interceptor.ReaderExecuting(command, eventData, default);

        // Assert
        _keyGenerator.Received(1).Generate(command, eventData.Context!);
        _keyGenerator.DidNotReceive().Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>(), Arg.Any<IRequestContext>());
    }

    // ── Multi-tenancy without a tenant (#1705, PR review): cache bypass, never a shared key ──

    private void EnableMultiTenancy(IRequestContext? ambient)
    {
        var isService = Substitute.For<IServiceProviderIsService>();
        isService.IsService(typeof(global::Encina.Tenancy.ITenantProvider)).Returns(true);
        _serviceProvider.GetService(typeof(IServiceProviderIsService)).Returns(isService);
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(ambient);
        _serviceProvider.GetService(typeof(IRequestContextAccessor)).Returns(accessor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ReaderExecutingAsync_MultiTenantWithoutATenant_BypassesTheCache_AndLogs3060(string? tenant)
    {
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<QueryCacheInterceptor>();
        EnableMultiTenancy(RequestContext.CreateForTest(tenantId: tenant));
        var interceptor = new QueryCacheInterceptor(_cacheProvider, _keyGenerator, CreateOptions(), _serviceProvider, logger);
        var command = Substitute.For<DbCommand>();
        var eventData = CreateCommandEventData(Substitute.For<DbContext>());

        await interceptor.ReaderExecutingAsync(command, eventData, default);

        _keyGenerator.ReceivedCalls().ShouldBeEmpty();
        await _cacheProvider.DidNotReceiveWithAnyArgs().GetAsync<CachedQueryResult>(default!, default);
        logger.Collector.GetSnapshot().Single().Id.Id.ShouldBe(3060);
    }

    [Fact]
    public async Task ReaderExecuting_OnAConnectionFlowOfAMultiTenantApp_NeverReadsOrWritesTheCache()
    {
        // A hub invocation runs under the connection marker: anonymous, no tenant.
        var host = new global::Encina.UnitTests.Core.Identity.ScopeTestHost();
        var interceptor = CreateInterceptor();
        var command = Substitute.For<DbCommand>();
        var eventData = CreateCommandEventData(Substitute.For<DbContext>());

        await host.Factory.RunAnonymousMarkerAsync(AnonymousMarker.Connection, _ =>
        {
            EnableMultiTenancy(host.Accessor.RequestContext);
            interceptor.ReaderExecuting(command, eventData, default);
            return Task.CompletedTask;
        });

        _keyGenerator.ReceivedCalls().ShouldBeEmpty();
        _cacheProvider.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void ABypassedQueryAfterAFailedTenantQuery_NeverWritesTheStaleKey()
    {
        // Tenant A misses the cache and its command fails; a tenant-less query then runs in the same flow.
        var interceptor = CreateInterceptor();
        var command = Substitute.For<DbCommand>();
        var eventData = CreateCommandEventData(Substitute.For<DbContext>());
        EnableMultiTenancy(RequestContext.CreateForTest(tenantId: "tenant-a"));
        _keyGenerator.Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>(), Arg.Any<IRequestContext>())
            .Returns(new QueryCacheKey("sm:qc:tenant-a:Order:hash", ["Order"]));
        _cacheProvider.GetAsync<CachedQueryResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CachedQueryResult?>(null));
        interceptor.ReaderExecuting(command, eventData, default);

        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(RequestContext.CreateForTest());
        _serviceProvider.GetService(typeof(IRequestContextAccessor)).Returns(accessor);
        interceptor.ReaderExecuting(command, eventData, default);
        interceptor.ReaderExecuted(command, null!, Substitute.For<DbDataReader>());

        _cacheProvider.DidNotReceiveWithAnyArgs().SetAsync<CachedQueryResult>(default!, default!, default, default);
    }

    [Fact]
    public void CommandFailed_DiscardsThePendingKey()
    {
        var interceptor = CreateInterceptor();
        var command = Substitute.For<DbCommand>();
        var (_, _, eventData) = ArrangeCacheMiss();
        interceptor.ReaderExecuting(command, eventData, default);

        interceptor.CommandFailed(command, null!);
        interceptor.ReaderExecuted(command, null!, Substitute.For<DbDataReader>());

        _cacheProvider.DidNotReceiveWithAnyArgs().SetAsync<CachedQueryResult>(default!, default!, default, default);
    }

    [Fact]
    public async Task ReaderExecutingAsync_MultiTenantWithATenant_KeysByThatTenant()
    {
        var tenantContext = RequestContext.CreateForTest(tenantId: "tenant-a");
        EnableMultiTenancy(tenantContext);
        var (interceptor, command, eventData) = ArrangeCacheMiss();

        await interceptor.ReaderExecutingAsync(command, eventData, default);

        _keyGenerator.Received(1).Generate(command, eventData.Context!, tenantContext);
    }

    [Fact]
    public async Task ReaderExecutingAsync_SingleTenantWithoutATenant_StillUsesTheCache()
    {
        var ambient = RequestContext.CreateForTest();
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(ambient);
        _serviceProvider.GetService(typeof(IRequestContextAccessor)).Returns(accessor);
        var (interceptor, command, eventData) = ArrangeCacheMiss();

        await interceptor.ReaderExecutingAsync(command, eventData, default);

        _keyGenerator.Received(1).Generate(command, eventData.Context!, ambient);
        await _cacheProvider.Received(1).GetAsync<CachedQueryResult>("tenant:key", Arg.Any<CancellationToken>());
    }

    private (QueryCacheInterceptor Interceptor, DbCommand Command, CommandEventData EventData) ArrangeCacheMiss()
    {
        var interceptor = CreateInterceptor(options: new QueryCacheOptions { Enabled = true });
        var command = Substitute.For<DbCommand>();
        var eventData = CreateCommandEventData(Substitute.For<DbContext>());

        _keyGenerator.Generate(Arg.Any<DbCommand>(), Arg.Any<DbContext>(), Arg.Any<IRequestContext>())
            .Returns(new QueryCacheKey("tenant:key", ["Order"]));
        _cacheProvider.GetAsync<CachedQueryResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CachedQueryResult?>(null));

        return (interceptor, command, eventData);
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
