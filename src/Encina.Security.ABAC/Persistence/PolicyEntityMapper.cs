using LanguageExt;

namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Provides bidirectional mapping between XACML domain models (<see cref="PolicySet"/>,
/// <see cref="Policy"/>) and their persistence entities (<see cref="PolicySetEntity"/>,
/// <see cref="PolicyEntity"/>).
/// </summary>
/// <remarks>
/// <para>
/// Domain-to-entity mapping extracts metadata columns (Id, Version, Description, IsEnabled,
/// Priority) from the domain model and serializes the full object graph to JSON via
/// <see cref="IPolicySerializer"/>. Timestamps are generated using <see cref="TimeProvider"/>
/// for deterministic testing.
/// </para>
/// <para>
/// Entity-to-domain mapping deserializes the JSON back into the domain model.
/// Deserialization can fail (malformed JSON, schema changes), so entity-to-domain methods
/// return <c>Either&lt;EncinaError, T&gt;</c>.
/// </para>
/// <para>
/// This class uses static methods because it is a pure mapping utility with no mutable state.
/// Dependencies (<see cref="IPolicySerializer"/>, <see cref="TimeProvider"/>) are passed
/// explicitly per call, allowing callers to inject the appropriate instances.
/// </para>
/// </remarks>
public static class PolicyEntityMapper
{
    // ── PolicySet Mapping ────────────────────────────────────────────

    /// <summary>
    /// Maps a <see cref="PolicySet"/> domain model to a <see cref="PolicySetEntity"/> for persistence.
    /// </summary>
    /// <param name="policySet">The policy set domain model to map.</param>
    /// <param name="serializer">The serializer used to convert the policy set to JSON.</param>
    /// <param name="timeProvider">The time provider for generating timestamps.</param>
    /// <param name="existingEntity">
    /// An optional existing entity to update. When provided, <see cref="PolicySetEntity.CreatedAtUtc"/>
    /// is preserved from the existing entity (update scenario). When <c>null</c>,
    /// <see cref="PolicySetEntity.CreatedAtUtc"/> is set to the current UTC time (insert scenario).
    /// </param>
    /// <returns>A new <see cref="PolicySetEntity"/> with serialized JSON and metadata.</returns>
    public static PolicySetEntity ToPolicySetEntity(
        PolicySet policySet,
        IPolicySerializer serializer,
        TimeProvider timeProvider,
        PolicySetEntity? existingEntity = null)
    {
        ArgumentNullException.ThrowIfNull(policySet);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new PolicySetEntity
        {
            Id = policySet.Id,
            Version = policySet.Version,
            Description = policySet.Description,
            PolicyJson = serializer.Serialize(policySet),
            IsEnabled = policySet.IsEnabled,
            Priority = policySet.Priority,
            CreatedAtUtc = existingEntity?.CreatedAtUtc ?? now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Maps a <see cref="PolicySetEntity"/> back to a <see cref="PolicySet"/> domain model.
    /// </summary>
    /// <param name="entity">The persistence entity to map.</param>
    /// <param name="serializer">The serializer used to deserialize the JSON.</param>
    /// <returns>
    /// <c>Right</c> containing the deserialized <see cref="PolicySet"/> on success,
    /// or <c>Left</c> containing an <see cref="EncinaError"/> if deserialization fails.
    /// </returns>
    public static Either<EncinaError, PolicySet> ToPolicySet(
        PolicySetEntity entity,
        IPolicySerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(serializer);

        return serializer.DeserializePolicySet(entity.PolicyJson);
    }

    // ── Policy Mapping ───────────────────────────────────────────────

    /// <summary>
    /// Maps a <see cref="Policy"/> domain model to a <see cref="PolicyEntity"/> for persistence.
    /// </summary>
    /// <param name="policy">The policy domain model to map.</param>
    /// <param name="serializer">The serializer used to convert the policy to JSON.</param>
    /// <param name="timeProvider">The time provider for generating timestamps.</param>
    /// <param name="existingEntity">
    /// An optional existing entity to update. When provided, <see cref="PolicyEntity.CreatedAtUtc"/>
    /// is preserved from the existing entity (update scenario). When <c>null</c>,
    /// <see cref="PolicyEntity.CreatedAtUtc"/> is set to the current UTC time (insert scenario).
    /// </param>
    /// <returns>A new <see cref="PolicyEntity"/> with serialized JSON and metadata.</returns>
    public static PolicyEntity ToPolicyEntity(
        Policy policy,
        IPolicySerializer serializer,
        TimeProvider timeProvider,
        PolicyEntity? existingEntity = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new PolicyEntity
        {
            Id = policy.Id,
            Version = policy.Version,
            Description = policy.Description,
            PolicyJson = serializer.Serialize(policy),
            IsEnabled = policy.IsEnabled,
            Priority = policy.Priority,
            CreatedAtUtc = existingEntity?.CreatedAtUtc ?? now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Maps a <see cref="PolicyEntity"/> back to a <see cref="Policy"/> domain model.
    /// </summary>
    /// <param name="entity">The persistence entity to map.</param>
    /// <param name="serializer">The serializer used to deserialize the JSON.</param>
    /// <returns>
    /// <c>Right</c> containing the deserialized <see cref="Policy"/> on success,
    /// or <c>Left</c> containing an <see cref="EncinaError"/> if deserialization fails.
    /// </returns>
    public static Either<EncinaError, Policy> ToPolicy(
        PolicyEntity entity,
        IPolicySerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(serializer);

        return serializer.DeserializePolicy(entity.PolicyJson);
    }
}
