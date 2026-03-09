using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Property-based tests for Persistent PAP serialization and mapping invariants.
/// Verifies behavioral properties that must hold for all valid ABAC model inputs,
/// including round-trip serialization, entity mapper consistency, and timestamp handling.
/// </summary>
public sealed class PersistentPAPPropertyTests
{
    private readonly DefaultPolicySerializer _serializer = new();

    // ── Serialization Round-Trip Invariants ────────────────────────────

    #region PolicySet Serialization Round-Trip

    [Property(MaxTest = 50)]
    public bool PolicySet_SerializeThenDeserialize_PreservesId(NonEmptyString id)
    {
        var policySet = CreateMinimalPolicySet(id.Get);
        var json = _serializer.Serialize(policySet);
        var result = _serializer.DeserializePolicySet(json);

        return result.IsRight
            && result.Match(Right: ps => ps.Id == id.Get, Left: _ => false);
    }

    [Property(MaxTest = 50)]
    public bool PolicySet_SerializeThenDeserialize_PreservesDescription(
        NonEmptyString id, string? description)
    {
        var policySet = CreateMinimalPolicySet(id.Get) with { Description = description };
        var json = _serializer.Serialize(policySet);
        var result = _serializer.DeserializePolicySet(json);

        return result.IsRight
            && result.Match(Right: ps => ps.Description == description, Left: _ => false);
    }

    [Property(MaxTest = 50)]
    public bool PolicySet_SerializeThenDeserialize_PreservesVersion(
        NonEmptyString id, string? version)
    {
        var policySet = CreateMinimalPolicySet(id.Get) with { Version = version };
        var json = _serializer.Serialize(policySet);
        var result = _serializer.DeserializePolicySet(json);

        return result.IsRight
            && result.Match(Right: ps => ps.Version == version, Left: _ => false);
    }

    [Property(MaxTest = 30)]
    public bool PolicySet_SerializeThenDeserialize_PreservesIsEnabled(NonEmptyString id, bool isEnabled)
    {
        var policySet = CreateMinimalPolicySet(id.Get) with { IsEnabled = isEnabled };
        var json = _serializer.Serialize(policySet);
        var result = _serializer.DeserializePolicySet(json);

        return result.IsRight
            && result.Match(Right: ps => ps.IsEnabled == isEnabled, Left: _ => false);
    }

    [Property(MaxTest = 30)]
    public bool PolicySet_SerializeThenDeserialize_PreservesPriority(NonEmptyString id, int priority)
    {
        var policySet = CreateMinimalPolicySet(id.Get) with { Priority = priority };
        var json = _serializer.Serialize(policySet);
        var result = _serializer.DeserializePolicySet(json);

        return result.IsRight
            && result.Match(Right: ps => ps.Priority == priority, Left: _ => false);
    }

    #endregion

    #region Policy Serialization Round-Trip

    [Property(MaxTest = 50)]
    public bool Policy_SerializeThenDeserialize_PreservesId(NonEmptyString id)
    {
        var policy = CreateMinimalPolicy(id.Get);
        var json = _serializer.Serialize(policy);
        var result = _serializer.DeserializePolicy(json);

        return result.IsRight
            && result.Match(Right: p => p.Id == id.Get, Left: _ => false);
    }

    [Property(MaxTest = 50)]
    public bool Policy_SerializeThenDeserialize_PreservesDescription(
        NonEmptyString id, string? description)
    {
        var policy = CreateMinimalPolicy(id.Get) with { Description = description };
        var json = _serializer.Serialize(policy);
        var result = _serializer.DeserializePolicy(json);

        return result.IsRight
            && result.Match(Right: p => p.Description == description, Left: _ => false);
    }

    [Property(MaxTest = 30)]
    public bool Policy_SerializeThenDeserialize_PreservesIsEnabled(NonEmptyString id, bool isEnabled)
    {
        var policy = CreateMinimalPolicy(id.Get) with { IsEnabled = isEnabled };
        var json = _serializer.Serialize(policy);
        var result = _serializer.DeserializePolicy(json);

        return result.IsRight
            && result.Match(Right: p => p.IsEnabled == isEnabled, Left: _ => false);
    }

    #endregion

    // ── CombiningAlgorithm Enum Invariants ─────────────────────────────

    #region CombiningAlgorithm Invariants

    [Property(MaxTest = 50)]
    public Property PolicySet_AllCombiningAlgorithms_Roundtrip()
    {
        var algorithms = Enum.GetValues<CombiningAlgorithmId>();

        return Prop.ForAll(
            Gen.Elements(algorithms).ToArbitrary(),
            algorithm =>
            {
                var policySet = CreateMinimalPolicySet("ps-alg") with { Algorithm = algorithm };
                var json = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(json);

                result.IsRight.ShouldBeTrue($"Deserialization should succeed for {algorithm}");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                deserialized.Algorithm.ShouldBe(algorithm);
            });
    }

    [Property(MaxTest = 50)]
    public Property Policy_AllCombiningAlgorithms_Roundtrip()
    {
        var algorithms = Enum.GetValues<CombiningAlgorithmId>();

        return Prop.ForAll(
            Gen.Elements(algorithms).ToArbitrary(),
            algorithm =>
            {
                var policy = CreateMinimalPolicy("p-alg") with { Algorithm = algorithm };
                var json = _serializer.Serialize(policy);
                var result = _serializer.DeserializePolicy(json);

                result.IsRight.ShouldBeTrue($"Deserialization should succeed for {algorithm}");
                var deserialized = result.Match(Right: p => p, Left: _ => null!);
                deserialized.Algorithm.ShouldBe(algorithm);
            });
    }

    [Property(MaxTest = 50)]
    public Property Rule_AllEffects_Roundtrip()
    {
        var effects = Enum.GetValues<Effect>();

        return Prop.ForAll(
            Gen.Elements(effects).ToArbitrary(),
            effect =>
            {
                var rule = new Rule
                {
                    Id = "rule-effect",
                    Effect = effect,
                    Target = null,
                    Condition = null,
                    Obligations = [],
                    Advice = []
                };

                var policy = CreateMinimalPolicy("p-eff") with { Rules = [rule] };
                var json = _serializer.Serialize(policy);
                var result = _serializer.DeserializePolicy(json);

                result.IsRight.ShouldBeTrue($"Deserialization should succeed for effect {effect}");
                var deserialized = result.Match(Right: p => p, Left: _ => null!);
                deserialized.Rules[0].Effect.ShouldBe(effect);
            });
    }

    #endregion

    // ── PolicySet Nesting Invariants ───────────────────────────────────

    #region Nesting Invariants

    [Property(MaxTest = 30)]
    public Property PolicySet_NestedPolicies_CountPreserved()
    {
        return Prop.ForAll(
            Gen.Choose(0, 10).ToArbitrary(),
            policyCount =>
            {
                var policies = Enumerable.Range(0, policyCount)
                    .Select(i => CreateMinimalPolicy($"p-{i}"))
                    .ToList();

                var policySet = CreateMinimalPolicySet("ps-nested") with { Policies = policies };
                var json = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(json);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                deserialized.Policies.Count.ShouldBe(policyCount);
            });
    }

    [Property(MaxTest = 30)]
    public Property PolicySet_NestedPolicies_IdsPreserved()
    {
        return Prop.ForAll(
            Gen.Choose(1, 8).ToArbitrary(),
            policyCount =>
            {
                var policies = Enumerable.Range(0, policyCount)
                    .Select(i => CreateMinimalPolicy($"p-{i}"))
                    .ToList();

                var policySet = CreateMinimalPolicySet("ps-ids") with { Policies = policies };
                var json = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(json);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                var expectedIds = policies.Select(p => p.Id).ToHashSet();
                var actualIds = deserialized.Policies.Select(p => p.Id).ToHashSet();
                expectedIds.SetEquals(actualIds).ShouldBeTrue("Policy IDs must match");
            });
    }

    #endregion

    // ── Entity Mapper Round-Trip Invariants ────────────────────────────

    #region PolicySetEntity Mapper Round-Trip

    [Property(MaxTest = 50)]
    public bool PolicySetEntity_Roundtrip_PreservesId(NonEmptyString id)
    {
        var policySet = CreateMinimalPolicySet(id.Get);
        var tp = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var entity = PolicyEntityMapper.ToPolicySetEntity(policySet, _serializer, tp);
        var result = PolicyEntityMapper.ToPolicySet(entity, _serializer);

        return result.IsRight
            && result.Match(Right: ps => ps.Id == id.Get, Left: _ => false);
    }

    [Property(MaxTest = 30)]
    public bool PolicySetEntity_Roundtrip_PreservesAlgorithm(NonEmptyString id)
    {
        var policySet = CreateMinimalPolicySet(id.Get) with
        {
            Algorithm = CombiningAlgorithmId.PermitOverrides
        };
        var tp = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var entity = PolicyEntityMapper.ToPolicySetEntity(policySet, _serializer, tp);
        var result = PolicyEntityMapper.ToPolicySet(entity, _serializer);

        return result.IsRight
            && result.Match(
                Right: ps => ps.Algorithm == CombiningAlgorithmId.PermitOverrides,
                Left: _ => false);
    }

    #endregion

    #region PolicyEntity Mapper Round-Trip

    [Property(MaxTest = 50)]
    public bool PolicyEntity_Roundtrip_PreservesId(NonEmptyString id)
    {
        var policy = CreateMinimalPolicy(id.Get);
        var tp = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var entity = PolicyEntityMapper.ToPolicyEntity(policy, _serializer, tp);
        var result = PolicyEntityMapper.ToPolicy(entity, _serializer);

        return result.IsRight
            && result.Match(Right: p => p.Id == id.Get, Left: _ => false);
    }

    [Property(MaxTest = 30)]
    public bool PolicyEntity_Roundtrip_PreservesAlgorithm(NonEmptyString id)
    {
        var policy = CreateMinimalPolicy(id.Get) with
        {
            Algorithm = CombiningAlgorithmId.FirstApplicable
        };
        var tp = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var entity = PolicyEntityMapper.ToPolicyEntity(policy, _serializer, tp);
        var result = PolicyEntityMapper.ToPolicy(entity, _serializer);

        return result.IsRight
            && result.Match(
                Right: p => p.Algorithm == CombiningAlgorithmId.FirstApplicable,
                Left: _ => false);
    }

    #endregion

    // ── Entity Mapper Timestamp Invariants ─────────────────────────────

    #region Timestamp Invariants

    [Property(MaxTest = 30)]
    public bool PolicySetEntity_Insert_CreatedAtUtcMatchesTimeProvider(PositiveInt ticks)
    {
        var now = DateTimeOffset.UtcNow.AddTicks(ticks.Get);
        var tp = new FakeTimeProvider(now);
        var policySet = CreateMinimalPolicySet("ps-ts");

        var entity = PolicyEntityMapper.ToPolicySetEntity(policySet, _serializer, tp);

        return entity.CreatedAtUtc == now.UtcDateTime
            && entity.UpdatedAtUtc == now.UtcDateTime;
    }

    [Property(MaxTest = 30)]
    public bool PolicySetEntity_Update_PreservesCreatedAtUtc(PositiveInt ticks)
    {
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddTicks(ticks.Get);
        var tp = new FakeTimeProvider(updatedAt);

        var existingEntity = new PolicySetEntity
        {
            Id = "ps-upd",
            PolicyJson = "{}",
            CreatedAtUtc = createdAt.UtcDateTime,
            UpdatedAtUtc = createdAt.UtcDateTime
        };

        var policySet = CreateMinimalPolicySet("ps-upd");
        var entity = PolicyEntityMapper.ToPolicySetEntity(policySet, _serializer, tp, existingEntity);

        return entity.CreatedAtUtc == createdAt.UtcDateTime
            && entity.UpdatedAtUtc == updatedAt.UtcDateTime;
    }

    [Property(MaxTest = 30)]
    public bool PolicyEntity_Insert_TimestampsMatchTimeProvider(PositiveInt ticks)
    {
        var now = DateTimeOffset.UtcNow.AddTicks(ticks.Get);
        var tp = new FakeTimeProvider(now);
        var policy = CreateMinimalPolicy("p-ts");

        var entity = PolicyEntityMapper.ToPolicyEntity(policy, _serializer, tp);

        return entity.CreatedAtUtc == now.UtcDateTime
            && entity.UpdatedAtUtc == now.UtcDateTime;
    }

    [Property(MaxTest = 30)]
    public bool PolicyEntity_Update_PreservesCreatedAtUtc(PositiveInt ticks)
    {
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddTicks(ticks.Get);
        var tp = new FakeTimeProvider(updatedAt);

        var existingEntity = new PolicyEntity
        {
            Id = "p-upd",
            PolicyJson = "{}",
            CreatedAtUtc = createdAt.UtcDateTime,
            UpdatedAtUtc = createdAt.UtcDateTime
        };

        var policy = CreateMinimalPolicy("p-upd");
        var entity = PolicyEntityMapper.ToPolicyEntity(policy, _serializer, tp, existingEntity);

        return entity.CreatedAtUtc == createdAt.UtcDateTime
            && entity.UpdatedAtUtc == updatedAt.UtcDateTime;
    }

    #endregion

    // ── Entity Metadata Mapping Invariants ─────────────────────────────

    #region Metadata Mapping

    [Property(MaxTest = 30)]
    public bool PolicySetEntity_MapsMetadataFields(
        NonEmptyString id, NonEmptyString version, NonEmptyString description, bool isEnabled, int priority)
    {
        var policySet = CreateMinimalPolicySet(id.Get) with
        {
            Version = version.Get,
            Description = description.Get,
            IsEnabled = isEnabled,
            Priority = priority
        };
        var tp = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var entity = PolicyEntityMapper.ToPolicySetEntity(policySet, _serializer, tp);

        return entity.Id == id.Get
            && entity.Version == version.Get
            && entity.Description == description.Get
            && entity.IsEnabled == isEnabled
            && entity.Priority == priority;
    }

    [Property(MaxTest = 30)]
    public bool PolicyEntity_MapsMetadataFields(
        NonEmptyString id, NonEmptyString version, NonEmptyString description, bool isEnabled, int priority)
    {
        var policy = CreateMinimalPolicy(id.Get) with
        {
            Version = version.Get,
            Description = description.Get,
            IsEnabled = isEnabled,
            Priority = priority
        };
        var tp = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var entity = PolicyEntityMapper.ToPolicyEntity(policy, _serializer, tp);

        return entity.Id == id.Get
            && entity.Version == version.Get
            && entity.Description == description.Get
            && entity.IsEnabled == isEnabled
            && entity.Priority == priority;
    }

    #endregion

    // ── Serialization Determinism ──────────────────────────────────────

    #region Serialization Determinism

    [Property(MaxTest = 50)]
    public bool PolicySet_SerializeTwice_ProducesSameJson(NonEmptyString id)
    {
        var policySet = CreateMinimalPolicySet(id.Get);
        var json1 = _serializer.Serialize(policySet);
        var json2 = _serializer.Serialize(policySet);

        return json1 == json2;
    }

    [Property(MaxTest = 50)]
    public bool Policy_SerializeTwice_ProducesSameJson(NonEmptyString id)
    {
        var policy = CreateMinimalPolicy(id.Get);
        var json1 = _serializer.Serialize(policy);
        var json2 = _serializer.Serialize(policy);

        return json1 == json2;
    }

    #endregion

    // ── Deserialization Error Invariants ───────────────────────────────

    #region Deserialization Error Invariants

    [Property(MaxTest = 30)]
    public bool MalformedJson_AlwaysReturnsLeft(NonEmptyString garbage)
    {
        // Prefix with '{' but leave it malformed to ensure it's not accidentally valid
        var malformed = "{" + garbage.Get;
        var psResult = _serializer.DeserializePolicySet(malformed);
        var pResult = _serializer.DeserializePolicy(malformed);

        return psResult.IsLeft && pResult.IsLeft;
    }

    #endregion

    // ── Helpers ────────────────────────────────────────────────────────

    private static PolicySet CreateMinimalPolicySet(string id = "ps-1") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy CreateMinimalPolicy(string id = "p-1") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    /// <summary>
    /// Deterministic <see cref="TimeProvider"/> for property-based tests.
    /// </summary>
    private sealed class FakeTimeProvider(DateTimeOffset fixedUtcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedUtcNow;
    }
}
