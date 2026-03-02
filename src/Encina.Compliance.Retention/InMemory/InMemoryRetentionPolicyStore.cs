using System.Collections.Concurrent;

using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IRetentionPolicyStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by <see cref="RetentionPolicy.Id"/>
/// with a secondary LINQ-based index on <see cref="RetentionPolicy.DataCategory"/>
/// for <see cref="GetByCategoryAsync"/> lookups.
/// </para>
/// <para>
/// This store is not intended for production use. All data is lost when the process exits.
/// For production, use a database-backed implementation (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
public sealed class InMemoryRetentionPolicyStore : IRetentionPolicyStore
{
    private readonly ConcurrentDictionary<string, RetentionPolicy> _policies = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryRetentionPolicyStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRetentionPolicyStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryRetentionPolicyStore(ILogger<InMemoryRetentionPolicyStore> logger)
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
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!_policies.TryAdd(policy.Id, policy))
        {
            _logger.LogWarning("Retention policy '{PolicyId}' already exists", policy.Id);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.PolicyAlreadyExists(policy.DataCategory)));
        }

        // Check for duplicate category
        var existingCategory = _policies.Values
            .FirstOrDefault(p => p.DataCategory == policy.DataCategory && p.Id != policy.Id);

        if (existingCategory is not null)
        {
            // Roll back the addition
            _policies.TryRemove(policy.Id, out _);
            _logger.LogWarning(
                "A retention policy already exists for category '{DataCategory}'",
                policy.DataCategory);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.PolicyAlreadyExists(policy.DataCategory)));
        }

        _logger.LogDebug("Created retention policy '{PolicyId}' for category '{DataCategory}'",
            policy.Id, policy.DataCategory);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByIdAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        var result = _policies.TryGetValue(policyId, out var policy)
            ? Some(policy)
            : Option<RetentionPolicy>.None;

        return ValueTask.FromResult<Either<EncinaError, Option<RetentionPolicy>>>(
            Right<EncinaError, Option<RetentionPolicy>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        var policy = _policies.Values.FirstOrDefault(p => p.DataCategory == dataCategory);
        var result = policy is not null
            ? Some(policy)
            : Option<RetentionPolicy>.None;

        return ValueTask.FromResult<Either<EncinaError, Option<RetentionPolicy>>>(
            Right<EncinaError, Option<RetentionPolicy>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicy>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RetentionPolicy> result = _policies.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<RetentionPolicy>>>(
            Right<EncinaError, IReadOnlyList<RetentionPolicy>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!_policies.ContainsKey(policy.Id))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.PolicyNotFound(policy.Id)));
        }

        _policies[policy.Id] = policy;
        _logger.LogDebug("Updated retention policy '{PolicyId}'", policy.Id);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        if (!_policies.TryRemove(policyId, out _))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.PolicyNotFound(policyId)));
        }

        _logger.LogDebug("Deleted retention policy '{PolicyId}'", policyId);
        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <summary>
    /// Returns all stored policies. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all policies in the store.</returns>
    public IReadOnlyList<RetentionPolicy> GetAllPolicies() =>
        _policies.Values.ToList().AsReadOnly();

    /// <summary>
    /// Removes all policies from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _policies.Clear();
}
