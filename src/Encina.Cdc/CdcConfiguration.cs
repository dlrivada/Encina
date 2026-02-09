using Encina.Cdc.Messaging;

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
