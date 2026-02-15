using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Default implementation of <see cref="IReferenceTableRegistry"/> using a frozen dictionary
/// for O(1) lookups after construction.
/// </summary>
/// <remarks>
/// <para>
/// The registry is built during service registration from the reference table configurations
/// collected by the fluent API. Once frozen, it is immutable and thread-safe for concurrent reads.
/// </para>
/// </remarks>
public sealed class ReferenceTableRegistry : IReferenceTableRegistry
{
    private readonly FrozenDictionary<Type, ReferenceTableConfiguration> _configurations;

    /// <summary>
    /// Initializes a new instance of <see cref="ReferenceTableRegistry"/> with the given configurations.
    /// </summary>
    /// <param name="configurations">The reference table configurations to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurations"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when duplicate entity types are detected.</exception>
    public ReferenceTableRegistry(IEnumerable<ReferenceTableConfiguration> configurations)
    {
        ArgumentNullException.ThrowIfNull(configurations);

        var configList = configurations.ToList();

        var duplicates = configList
            .GroupBy(c => c.EntityType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.Name)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ArgumentException(
                $"Duplicate reference table registrations detected: {string.Join(", ", duplicates)}.",
                nameof(configurations));
        }

        _configurations = configList.ToFrozenDictionary(c => c.EntityType, c => c);
    }

    /// <inheritdoc />
    public bool IsRegistered<TEntity>() where TEntity : class
        => _configurations.ContainsKey(typeof(TEntity));

    /// <inheritdoc />
    public bool IsRegistered(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return _configurations.ContainsKey(entityType);
    }

    /// <inheritdoc />
    public ReferenceTableConfiguration GetConfiguration<TEntity>() where TEntity : class
        => GetConfiguration(typeof(TEntity));

    /// <inheritdoc />
    public ReferenceTableConfiguration GetConfiguration(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        if (!_configurations.TryGetValue(entityType, out var configuration))
        {
            throw new InvalidOperationException(
                $"Entity type '{entityType.Name}' is not registered as a reference table. " +
                $"Register it via AddReferenceTable<{entityType.Name}>() in sharding configuration.");
        }

        return configuration;
    }

    /// <inheritdoc />
    public bool TryGetConfiguration(Type entityType, [NotNullWhen(true)] out ReferenceTableConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return _configurations.TryGetValue(entityType, out configuration);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ReferenceTableConfiguration> GetAllConfigurations()
        => _configurations.Values.ToList().AsReadOnly();
}
