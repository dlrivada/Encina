using Encina.Cdc.Caching;
using Encina.Cdc.Messaging;
using Encina.Cdc.Sharding;

namespace Encina.Cdc;

/// <summary>
/// Fluent configuration builder for CDC features.
/// Provides methods to register handlers, configure table mappings,
/// and select CDC providers.
/// </summary>
/// <example>
/// <code>
/// services.AddEncinaCdc(config =>
/// {
///     config.UseCdc()
///           .AddHandler&lt;Order, OrderChangeHandler&gt;()
///           .WithTableMapping&lt;Order&gt;("dbo.Orders")
///           .WithMessagingBridge(opts =>
///           {
///               opts.TopicPattern = "cdc.{tableName}.{operation}";
///               opts.ExcludeTables = ["__EFMigrationsHistory"];
///           });
/// });
/// </code>
/// </example>
public sealed class CdcConfiguration
{
    private readonly CdcOptions _options = new();
    private readonly List<TableMapping> _tableMappings = [];
    private readonly List<HandlerRegistration> _handlerRegistrations = [];
    private CdcMessagingOptions? _messagingOptions;
    private ShardedCaptureOptions? _shardedCaptureOptions;
    private QueryCacheInvalidationOptions? _cacheInvalidationOptions;

    /// <summary>
    /// Gets the configured CDC options.
    /// </summary>
    internal CdcOptions Options => _options;

    /// <summary>
    /// Gets the registered table-to-entity mappings.
    /// </summary>
    internal IReadOnlyList<TableMapping> TableMappings => _tableMappings;

    /// <summary>
    /// Gets the registered handler registrations.
    /// </summary>
    internal IReadOnlyList<HandlerRegistration> HandlerRegistrations => _handlerRegistrations;

    /// <summary>
    /// Gets the messaging bridge options, or <c>null</c> if messaging bridge is not configured.
    /// </summary>
    internal CdcMessagingOptions? MessagingOptions => _messagingOptions;

    /// <summary>
    /// Gets the sharded capture options, or <c>null</c> if sharded capture is not configured.
    /// </summary>
    internal ShardedCaptureOptions? ShardedCaptureOptions => _shardedCaptureOptions;

    /// <summary>
    /// Gets the cache invalidation options, or <c>null</c> if cache invalidation is not configured.
    /// </summary>
    internal QueryCacheInvalidationOptions? CacheInvalidationOptions => _cacheInvalidationOptions;

    /// <summary>
    /// Enables CDC processing and returns this configuration for fluent chaining.
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    public CdcConfiguration UseCdc()
    {
        _options.Enabled = true;
        return this;
    }

    /// <summary>
    /// Registers a change event handler for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to handle.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <returns>This configuration instance for method chaining.</returns>
    public CdcConfiguration AddHandler<TEntity, THandler>()
        where THandler : Abstractions.IChangeEventHandler<TEntity>
    {
        _handlerRegistrations.Add(new HandlerRegistration(typeof(TEntity), typeof(THandler)));
        return this;
    }

    /// <summary>
    /// Maps a database table name to an entity type for handler routing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to map.</typeparam>
    /// <param name="tableName">The database table name (e.g., "dbo.Orders").</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CdcConfiguration WithTableMapping<TEntity>(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _tableMappings.Add(new TableMapping(tableName, typeof(TEntity)));
        return this;
    }

    /// <summary>
    /// Configures CDC options.
    /// </summary>
    /// <param name="configure">Action to configure options.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CdcConfiguration WithOptions(Action<CdcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_options);
        return this;
    }

    /// <summary>
    /// Enables the CDC-to-messaging bridge, which publishes captured change events
    /// as <see cref="CdcChangeNotification"/> via <see cref="IEncina.Publish{TNotification}"/>.
    /// </summary>
    /// <param name="configure">Optional action to configure messaging bridge options.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// config.WithMessagingBridge(opts =>
    /// {
    ///     opts.TopicPattern = "cdc.{tableName}.{operation}";
    ///     opts.IncludeTables = ["Orders", "Customers"];
    /// });
    /// </code>
    /// </example>
    public CdcConfiguration WithMessagingBridge(Action<CdcMessagingOptions>? configure = null)
    {
        _options.UseMessagingBridge = true;
        _messagingOptions = new CdcMessagingOptions();
        configure?.Invoke(_messagingOptions);
        return this;
    }

    /// <summary>
    /// Enables sharded CDC capture, which processes change events from multiple
    /// database shards using <see cref="Abstractions.IShardedCdcConnector"/>.
    /// </summary>
    /// <param name="configure">Optional action to configure sharded capture options.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// When sharded capture is enabled, the <c>ShardedCdcProcessor</c> is registered
    /// instead of the standard <c>CdcProcessor</c>. The two processors are mutually
    /// exclusive: enabling sharded capture prevents the standard processor from being
    /// registered to avoid conflicts.
    /// </para>
    /// <para>
    /// Sharded capture requires an <see cref="Sharding.IShardTopologyProvider"/> to be
    /// registered in the service collection. Each shard gets its own
    /// <see cref="Abstractions.ICdcConnector"/> created via a factory delegate.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// config.WithShardedCapture(opts =>
    /// {
    ///     opts.ProcessingMode = ShardedProcessingMode.Aggregated;
    ///     opts.MaxLagThreshold = TimeSpan.FromMinutes(5);
    ///     opts.ConnectorId = "orders-sharded-cdc";
    /// });
    /// </code>
    /// </example>
    public CdcConfiguration WithShardedCapture(Action<ShardedCaptureOptions>? configure = null)
    {
        _options.UseShardedCapture = true;
        _shardedCaptureOptions = new ShardedCaptureOptions();
        configure?.Invoke(_shardedCaptureOptions);
        return this;
    }

    /// <summary>
    /// Enables CDC-driven query cache invalidation, which detects database changes
    /// from any source (other app instances, direct SQL, migrations, external services)
    /// and invalidates matching cache entries via <c>ICacheProvider</c>.
    /// Optionally broadcasts invalidation to other instances via <c>IPubSubProvider</c>.
    /// </summary>
    /// <param name="configure">Optional action to configure cache invalidation options.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This feature complements the existing <c>QueryCacheInterceptor</c> which only
    /// invalidates cache entries for changes made by the same application instance.
    /// CDC-driven invalidation covers all change sources including other instances,
    /// direct SQL updates, and external microservices.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// config.WithCacheInvalidation(opts =>
    /// {
    ///     opts.CacheKeyPrefix = "sm:qc";
    ///     opts.UsePubSubBroadcast = true;
    ///     opts.Tables = ["Orders", "Products"];
    /// });
    /// </code>
    /// </example>
    public CdcConfiguration WithCacheInvalidation(Action<QueryCacheInvalidationOptions>? configure = null)
    {
        _options.UseCacheInvalidation = true;
        _cacheInvalidationOptions = new QueryCacheInvalidationOptions();
        configure?.Invoke(_cacheInvalidationOptions);
        return this;
    }

    /// <summary>
    /// Enables CDC-driven outbox processing, where CDC monitors the outbox table
    /// and republishes stored notifications instead of polling-based processing.
    /// </summary>
    /// <param name="outboxTableName">The name of the outbox table to monitor. Default is "OutboxMessages".</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This provides a CDC-driven alternative to the traditional polling-based
    /// <c>OutboxProcessor</c>. When a new outbox message is inserted, CDC captures
    /// the change and immediately publishes the original notification.
    /// </para>
    /// <para>
    /// The handler automatically skips rows where <c>ProcessedAtUtc</c> is already set,
    /// allowing safe coexistence with the traditional processor.
    /// </para>
    /// </remarks>
    public CdcConfiguration UseOutboxCdc(string outboxTableName = "OutboxMessages")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxTableName);
        _options.UseOutboxCdc = true;
        _tableMappings.Add(new TableMapping(outboxTableName, typeof(System.Text.Json.JsonElement)));
        _handlerRegistrations.Add(new HandlerRegistration(typeof(System.Text.Json.JsonElement), typeof(OutboxCdcHandler)));
        return this;
    }

    /// <summary>
    /// Represents a mapping between a database table name and an entity type.
    /// </summary>
    /// <param name="TableName">The database table name.</param>
    /// <param name="EntityType">The entity type to deserialize change events into.</param>
    internal sealed record TableMapping(string TableName, Type EntityType);

    /// <summary>
    /// Represents a handler registration for a specific entity type.
    /// </summary>
    /// <param name="EntityType">The entity type the handler processes.</param>
    /// <param name="HandlerType">The handler implementation type.</param>
    internal sealed record HandlerRegistration(Type EntityType, Type HandlerType);
}
