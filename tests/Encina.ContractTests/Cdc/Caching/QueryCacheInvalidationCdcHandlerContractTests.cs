using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Caching;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Caching;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Cdc.Caching;

/// <summary>
/// Contract tests verifying that <see cref="QueryCacheInvalidationCdcHandler"/> correctly
/// implements the <see cref="IChangeEventHandler{TEntity}"/> contract. All three handler
/// methods must return valid <see cref="Either{EncinaError, Unit}"/> results and handle
/// cancellation correctly.
/// </summary>
[Trait("Category", "Contract")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
    Justification = "Test setup pattern")]
public sealed class QueryCacheInvalidationCdcHandlerContractTests
{
    #region HandleInsertAsync Contract

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleInsertAsync"/> must
    /// return <c>Right(unit)</c> on successful processing.
    /// </summary>
    [Fact]
    public async Task Contract_HandleInsertAsync_ReturnsRight_OnSuccess()
    {
        // Arrange
        var handler = CreateHandler();
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleInsertAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue("HandleInsertAsync must return Right(unit) on success");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleInsertAsync"/> must not
    /// throw exceptions for valid inputs.
    /// </summary>
    [Fact]
    public async Task Contract_HandleInsertAsync_DoesNotThrow_ForValidInputs()
    {
        // Arrange
        var handler = CreateHandler();
        var context = CreateContext("Orders");

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await handler.HandleInsertAsync(JsonElement.Parse("{}"), context));
    }

    #endregion

    #region HandleUpdateAsync Contract

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleUpdateAsync"/> must
    /// return <c>Right(unit)</c> on successful processing.
    /// </summary>
    [Fact]
    public async Task Contract_HandleUpdateAsync_ReturnsRight_OnSuccess()
    {
        // Arrange
        var handler = CreateHandler();
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleUpdateAsync(
            JsonElement.Parse("{}"), JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue("HandleUpdateAsync must return Right(unit) on success");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleUpdateAsync"/> must not
    /// throw exceptions for valid inputs.
    /// </summary>
    [Fact]
    public async Task Contract_HandleUpdateAsync_DoesNotThrow_ForValidInputs()
    {
        // Arrange
        var handler = CreateHandler();
        var context = CreateContext("Orders");

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await handler.HandleUpdateAsync(
                JsonElement.Parse("{}"), JsonElement.Parse("{}"), context));
    }

    #endregion

    #region HandleDeleteAsync Contract

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleDeleteAsync"/> must
    /// return <c>Right(unit)</c> on successful processing.
    /// </summary>
    [Fact]
    public async Task Contract_HandleDeleteAsync_ReturnsRight_OnSuccess()
    {
        // Arrange
        var handler = CreateHandler();
        var context = CreateContext("Orders");

        // Act
        var result = await handler.HandleDeleteAsync(JsonElement.Parse("{}"), context);

        // Assert
        result.IsRight.ShouldBeTrue("HandleDeleteAsync must return Right(unit) on success");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleDeleteAsync"/> must not
    /// throw exceptions for valid inputs.
    /// </summary>
    [Fact]
    public async Task Contract_HandleDeleteAsync_DoesNotThrow_ForValidInputs()
    {
        // Arrange
        var handler = CreateHandler();
        var context = CreateContext("Orders");

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await handler.HandleDeleteAsync(JsonElement.Parse("{}"), context));
    }

    #endregion

    #region Cancellation Contract

    /// <summary>
    /// Contract: Handler must not throw for cancelled tokens (resilient by design).
    /// </summary>
    [Fact]
    public async Task Contract_HandleInsertAsync_CancelledToken_DoesNotThrow()
    {
        // Arrange
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = CreateContext("Orders", cts.Token);

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await handler.HandleInsertAsync(JsonElement.Parse("{}"), context));
    }

    #endregion

    #region Interface Shape Contract

    /// <summary>
    /// Contract: <see cref="QueryCacheInvalidationCdcHandler"/> must implement
    /// <see cref="IChangeEventHandler{TEntity}"/> of <see cref="JsonElement"/>.
    /// </summary>
    [Fact]
    public void Contract_ImplementsIChangeEventHandler_OfJsonElement()
    {
        typeof(IChangeEventHandler<JsonElement>)
            .IsAssignableFrom(typeof(QueryCacheInvalidationCdcHandler))
            .ShouldBeTrue("QueryCacheInvalidationCdcHandler must implement IChangeEventHandler<JsonElement>");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}"/> must declare exactly 3 methods.
    /// </summary>
    [Fact]
    public void Contract_IChangeEventHandler_HasExactlyThreeMethods()
    {
        var iface = typeof(IChangeEventHandler<>);
        var methods = iface.GetMethods(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        methods.Length.ShouldBe(3,
            "IChangeEventHandler<TEntity> must declare exactly 3 methods (Insert, Update, Delete)");
    }

    #endregion

    #region Test Helpers

    private static QueryCacheInvalidationCdcHandler CreateHandler()
    {
        var cacheProvider = new NoOpCacheProvider();
        var options = Options.Create(new QueryCacheInvalidationOptions());
        var logger = NullLogger<QueryCacheInvalidationCdcHandler>.Instance;

        return new QueryCacheInvalidationCdcHandler(
            cacheProvider, null, options, logger);
    }

    private static ChangeContext CreateContext(
        string tableName, CancellationToken cancellationToken = default)
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        return new ChangeContext(tableName, metadata, cancellationToken);
    }

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;
        public long Value { get; }
        public override byte[] ToBytes() => BitConverter.GetBytes(Value);
        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;
        public override string ToString() => $"TestPosition({Value})";
    }

    /// <summary>
    /// Minimal no-op cache provider for contract testing. Does not require NSubstitute.
    /// </summary>
    private sealed class NoOpCacheProvider : ICacheProvider
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) =>
            Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
            TimeSpan? expiration, CancellationToken cancellationToken) =>
            factory(cancellationToken);

        public Task SetWithSlidingExpirationAsync<T>(string key, T value,
            TimeSpan slidingExpiration, TimeSpan? absoluteExpiration,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    #endregion
}
