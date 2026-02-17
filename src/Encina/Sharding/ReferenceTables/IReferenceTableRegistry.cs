namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Tracks all registered reference tables, their configurations, and refresh strategies.
/// </summary>
/// <remarks>
/// <para>
/// The registry is populated at startup during service registration and is immutable
/// thereafter. It provides O(1) lookups by entity type using a frozen dictionary internally.
/// </para>
/// <para>
/// Implementations should be registered as singletons since reference table registrations
/// do not change after application startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if an entity is a registered reference table
/// if (registry.IsRegistered&lt;Country&gt;())
/// {
///     var config = registry.GetConfiguration&lt;Country&gt;();
///     logger.LogInformation("Country table uses {Strategy} refresh", config.Options.RefreshStrategy);
/// }
///
/// // Iterate all registered reference tables
/// foreach (var config in registry.GetAllConfigurations())
/// {
///     logger.LogInformation("Reference table: {Type}", config.EntityType.Name);
/// }
/// </code>
/// </example>
public interface IReferenceTableRegistry
{
    /// <summary>
    /// Gets whether the specified entity type is registered as a reference table.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to check.</typeparam>
    /// <returns><c>true</c> if the entity type is registered; otherwise, <c>false</c>.</returns>
    bool IsRegistered<TEntity>() where TEntity : class;

    /// <summary>
    /// Gets whether the specified entity type is registered as a reference table.
    /// </summary>
    /// <param name="entityType">The entity type to check.</param>
    /// <returns><c>true</c> if the entity type is registered; otherwise, <c>false</c>.</returns>
    bool IsRegistered(Type entityType);

    /// <summary>
    /// Gets the configuration for a registered reference table.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to look up.</typeparam>
    /// <returns>The reference table configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity type is not registered as a reference table.
    /// </exception>
    ReferenceTableConfiguration GetConfiguration<TEntity>() where TEntity : class;

    /// <summary>
    /// Gets the configuration for a registered reference table.
    /// </summary>
    /// <param name="entityType">The entity type to look up.</param>
    /// <returns>The reference table configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity type is not registered as a reference table.
    /// </exception>
    ReferenceTableConfiguration GetConfiguration(Type entityType);

    /// <summary>
    /// Tries to get the configuration for a reference table.
    /// </summary>
    /// <param name="entityType">The entity type to look up.</param>
    /// <param name="configuration">
    /// When this method returns, contains the configuration if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the entity type is registered; otherwise, <c>false</c>.</returns>
    bool TryGetConfiguration(Type entityType, out ReferenceTableConfiguration? configuration);

    /// <summary>
    /// Gets all registered reference table configurations.
    /// </summary>
    /// <returns>A read-only collection of all registered configurations.</returns>
    IReadOnlyCollection<ReferenceTableConfiguration> GetAllConfigurations();
}
