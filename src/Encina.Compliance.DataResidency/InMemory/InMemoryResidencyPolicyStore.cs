using System.Collections.Concurrent;

using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IResidencyPolicyStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="ResidencyPolicyDescriptor.DataCategory"/> with <see cref="StringComparer.Ordinal"/>
/// for deterministic, case-sensitive lookups.
/// </para>
/// <para>
/// This store is not intended for production use. All data is lost when the process exits.
/// For production, use a database-backed implementation (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
public sealed class InMemoryResidencyPolicyStore : IResidencyPolicyStore
{
    private readonly ConcurrentDictionary<string, ResidencyPolicyDescriptor> _policies = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryResidencyPolicyStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryResidencyPolicyStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryResidencyPolicyStore(ILogger<InMemoryResidencyPolicyStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of policies currently stored. Useful for testing assertions.
    /// </summary>
    public int Count => _policies.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> CreateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!_policies.TryAdd(policy.DataCategory, policy))
        {
            _logger.LogWarning(
                "A residency policy already exists for category '{DataCategory}'",
                policy.DataCategory);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(DataResidencyErrors.PolicyAlreadyExists(policy.DataCategory)));
        }

        _logger.LogDebug(
            "Created residency policy for category '{DataCategory}' with {RegionCount} allowed regions",
            policy.DataCategory, policy.AllowedRegions.Count);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        var result = _policies.TryGetValue(dataCategory, out var policy)
            ? Some(policy)
            : Option<ResidencyPolicyDescriptor>.None;

        return ValueTask.FromResult<Either<EncinaError, Option<ResidencyPolicyDescriptor>>>(
            Right<EncinaError, Option<ResidencyPolicyDescriptor>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ResidencyPolicyDescriptor> result = _policies.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>>(
            Right<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!_policies.ContainsKey(policy.DataCategory))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(DataResidencyErrors.PolicyNotFound(policy.DataCategory)));
        }

        _policies[policy.DataCategory] = policy;
        _logger.LogDebug("Updated residency policy for category '{DataCategory}'", policy.DataCategory);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        if (!_policies.TryRemove(dataCategory, out _))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(DataResidencyErrors.PolicyNotFound(dataCategory)));
        }

        _logger.LogDebug("Deleted residency policy for category '{DataCategory}'", dataCategory);
        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <summary>
    /// Returns all stored policies. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all policies in the store.</returns>
    public IReadOnlyList<ResidencyPolicyDescriptor> GetAllPolicies() =>
        _policies.Values.ToList().AsReadOnly();

    /// <summary>
    /// Removes all policies from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _policies.Clear();
}
