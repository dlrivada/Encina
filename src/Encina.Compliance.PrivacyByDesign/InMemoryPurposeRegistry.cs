using System.Collections.Concurrent;

using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// In-memory implementation of <see cref="IPurposeRegistry"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> keyed by <c>(Name, ModuleId)</c>
/// for thread-safe, module-aware purpose resolution. Supports the module fallback pattern:
/// when a purpose is looked up with a <c>moduleId</c>, the registry first searches for a
/// module-specific definition, then falls back to the global scope.
/// </para>
/// <para>
/// A secondary dictionary keyed by <see cref="PurposeDefinition.PurposeId"/> allows
/// O(1) removal by ID. Both dictionaries are kept in sync.
/// </para>
/// <para>
/// This implementation is not intended for production use. For production, use one of the
/// 13 database provider implementations (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
internal sealed class InMemoryPurposeRegistry : IPurposeRegistry
{
    private readonly ConcurrentDictionary<(string Name, string? ModuleId), PurposeDefinition> _byScope = new();
    private readonly ConcurrentDictionary<string, PurposeDefinition> _byId = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryPurposeRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPurposeRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InMemoryPurposeRegistry(ILogger<InMemoryPurposeRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of purpose definitions currently registered.
    /// </summary>
    internal int Count => _byId.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<PurposeDefinition>>> GetPurposeAsync(
        string purposeName,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purposeName);

        // Module-specific lookup first, then global fallback.
        if (moduleId is not null && _byScope.TryGetValue((purposeName, moduleId), out var modulePurpose))
        {
            return ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(Some(modulePurpose)));
        }

        if (_byScope.TryGetValue((purposeName, null), out var globalPurpose))
        {
            return ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(Some(globalPurpose)));
        }

        return ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(Option<PurposeDefinition>.None));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<PurposeDefinition>>> GetAllPurposesAsync(
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        List<PurposeDefinition> results;

        if (moduleId is not null)
        {
            // Return module-specific + global, with module overriding global for same name.
            var globalPurposes = _byScope
                .Where(kvp => kvp.Key.ModuleId is null)
                .ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);

            var modulePurposes = _byScope
                .Where(kvp => kvp.Key.ModuleId == moduleId)
                .ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);

            // Merge: module-specific overrides global.
            foreach (var (name, purpose) in modulePurposes)
            {
                globalPurposes[name] = purpose;
            }

            results = [.. globalPurposes.Values];
        }
        else
        {
            // Global only.
            results = _byScope
                .Where(kvp => kvp.Key.ModuleId is null)
                .Select(kvp => kvp.Value)
                .ToList();
        }

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PurposeDefinition>>>(results);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RegisterPurposeAsync(
        PurposeDefinition purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purpose);

        var scopeKey = (purpose.Name, purpose.ModuleId);

        // Check for name collision within scope (different PurposeId, same Name+ModuleId).
        if (_byScope.TryGetValue(scopeKey, out var existing) && existing.PurposeId != purpose.PurposeId)
        {
            _logger.LogWarning(
                "Duplicate purpose name '{PurposeName}' in module '{ModuleId}' — existing PurposeId='{ExistingId}', attempted PurposeId='{AttemptedId}'",
                purpose.Name, purpose.ModuleId, existing.PurposeId, purpose.PurposeId);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                PrivacyByDesignErrors.DuplicatePurpose(purpose.Name, purpose.ModuleId));
        }

        // Upsert by PurposeId — remove old scope key if the purpose existed with a different name.
        if (_byId.TryGetValue(purpose.PurposeId, out var previousVersion))
        {
            _byScope.TryRemove((previousVersion.Name, previousVersion.ModuleId), out _);
        }

        _byId[purpose.PurposeId] = purpose;
        _byScope[scopeKey] = purpose;

        _logger.LogDebug(
            "Purpose '{PurposeName}' registered with PurposeId='{PurposeId}' in module '{ModuleId}'",
            purpose.Name, purpose.PurposeId, purpose.ModuleId ?? "(global)");

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RemovePurposeAsync(
        string purposeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purposeId);

        if (!_byId.TryRemove(purposeId, out var removed))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                PrivacyByDesignErrors.PurposeNotFound(purposeId));
        }

        _byScope.TryRemove((removed.Name, removed.ModuleId), out _);

        _logger.LogDebug(
            "Purpose '{PurposeName}' removed (PurposeId='{PurposeId}', module='{ModuleId}')",
            removed.Name, purposeId, removed.ModuleId ?? "(global)");

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <summary>
    /// Removes all stored purposes. Test helper method.
    /// </summary>
    internal void Clear()
    {
        _byId.Clear();
        _byScope.Clear();
    }
}
