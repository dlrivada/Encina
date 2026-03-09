using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence.Xacml;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Property-based tests for XACML XML serialization round-trip invariants.
/// Verifies that all valid ABAC model inputs survive serialization and deserialization
/// through the <see cref="XacmlXmlPolicySerializer"/> without data loss.
/// </summary>
public sealed class XacmlXmlRoundTripPropertyTests
{
    private readonly XacmlXmlPolicySerializer _serializer =
        new(NullLoggerFactory.Instance.CreateLogger<XacmlXmlPolicySerializer>());

    /// <summary>
    /// Generator for XML-safe non-empty strings (letters, digits, hyphens, underscores).
    /// Avoids control characters and special XML characters that would cause parsing failures.
    /// </summary>
    private static Arbitrary<string> XmlSafeStringArb()
    {
        var xmlSafeChars = Gen.Elements(
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_."
                .ToCharArray());

        return Gen.NonEmptyListOf(xmlSafeChars)
            .Select(chars => new string([.. chars]))
            .ToArbitrary();
    }

    // ── PolicySet Serialization Round-Trip ────────────────────────────

    #region PolicySet Serialization Round-Trip

    [Property(MaxTest = 50)]
    public Property PolicySet_SerializeThenDeserialize_PreservesId()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            id =>
            {
                var policySet = CreateMinimalPolicySet(id);
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

                result.IsRight.ShouldBeTrue($"Deserialization should succeed for id '{id}'");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                deserialized.Id.ShouldBe(id);
            });
    }

    [Property(MaxTest = 50)]
    public Property PolicySet_SerializeThenDeserialize_PreservesDescription()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            XmlSafeStringArb(),
            (id, description) =>
            {
                var policySet = CreateMinimalPolicySet(id) with { Description = description };
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                deserialized.Description.ShouldBe(description);
            });
    }

    [Property(MaxTest = 50)]
    public Property PolicySet_SerializeThenDeserialize_PreservesVersion()
    {
        var versionGen = Gen.Elements("1.0", "2.0", "0.1", "3.5.1", "10.0")
            .ToArbitrary();

        return Prop.ForAll(
            XmlSafeStringArb(),
            versionGen,
            (id, version) =>
            {
                var policySet = CreateMinimalPolicySet(id) with { Version = version };
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                deserialized.Version.ShouldBe(version);
            });
    }

    [Property(MaxTest = 30)]
    public Property PolicySet_SerializeThenDeserialize_PreservesIsEnabled()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            Gen.Elements(true, false).ToArbitrary(),
            (id, isEnabled) =>
            {
                var policySet = CreateMinimalPolicySet(id) with { IsEnabled = isEnabled };
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                deserialized.IsEnabled.ShouldBe(isEnabled);
            });
    }

    [Property(MaxTest = 30)]
    public Property PolicySet_SerializeThenDeserialize_PreservesPriority()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            Gen.Choose(-10000, 10000).ToArbitrary(),
            (id, priority) =>
            {
                var policySet = CreateMinimalPolicySet(id) with { Priority = priority };
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                deserialized.Priority.ShouldBe(priority);
            });
    }

    #endregion

    // ── Policy Serialization Round-Trip ─────────────────────────────

    #region Policy Serialization Round-Trip

    [Property(MaxTest = 50)]
    public Property Policy_SerializeThenDeserialize_PreservesId()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            id =>
            {
                var policy = CreateMinimalPolicy(id);
                var xml = _serializer.Serialize(policy);
                var result = _serializer.DeserializePolicy(xml);

                result.IsRight.ShouldBeTrue($"Deserialization should succeed for id '{id}'");
                var deserialized = result.Match(Right: p => p, Left: _ => null!);
                deserialized.Id.ShouldBe(id);
            });
    }

    [Property(MaxTest = 50)]
    public Property Policy_SerializeThenDeserialize_PreservesDescription()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            XmlSafeStringArb(),
            (id, description) =>
            {
                var policy = CreateMinimalPolicy(id) with { Description = description };
                var xml = _serializer.Serialize(policy);
                var result = _serializer.DeserializePolicy(xml);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: p => p, Left: _ => null!);
                deserialized.Description.ShouldBe(description);
            });
    }

    [Property(MaxTest = 30)]
    public Property Policy_SerializeThenDeserialize_PreservesIsEnabled()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            Gen.Elements(true, false).ToArbitrary(),
            (id, isEnabled) =>
            {
                var policy = CreateMinimalPolicy(id) with { IsEnabled = isEnabled };
                var xml = _serializer.Serialize(policy);
                var result = _serializer.DeserializePolicy(xml);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: p => p, Left: _ => null!);
                deserialized.IsEnabled.ShouldBe(isEnabled);
            });
    }

    #endregion

    // ── CombiningAlgorithm Enum Invariants ─────────────────────────

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
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

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
                var xml = _serializer.Serialize(policy);
                var result = _serializer.DeserializePolicy(xml);

                result.IsRight.ShouldBeTrue($"Deserialization should succeed for {algorithm}");
                var deserialized = result.Match(Right: p => p, Left: _ => null!);
                deserialized.Algorithm.ShouldBe(algorithm);
            });
    }

    [Property(MaxTest = 50)]
    public Property Rule_AllEffects_Roundtrip()
    {
        // Only Permit and Deny are valid XACML rule effects
        var effects = new[] { Effect.Permit, Effect.Deny };

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
                var xml = _serializer.Serialize(policy);
                var result = _serializer.DeserializePolicy(xml);

                result.IsRight.ShouldBeTrue($"Deserialization should succeed for effect {effect}");
                var deserialized = result.Match(Right: p => p, Left: _ => null!);
                deserialized.Rules[0].Effect.ShouldBe(effect);
            });
    }

    #endregion

    // ── PolicySet Nesting Invariants ───────────────────────────────

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
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

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
                var xml = _serializer.Serialize(policySet);
                var result = _serializer.DeserializePolicySet(xml);

                result.IsRight.ShouldBeTrue("Deserialization should succeed");
                var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
                var expectedIds = policies.Select(p => p.Id).ToHashSet();
                var actualIds = deserialized.Policies.Select(p => p.Id).ToHashSet();
                expectedIds.SetEquals(actualIds).ShouldBeTrue("Policy IDs must match");
            });
    }

    #endregion

    // ── Serialization Determinism ──────────────────────────────────

    #region Serialization Determinism

    [Property(MaxTest = 50)]
    public Property PolicySet_SerializeTwice_ProducesSameXml()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            id =>
            {
                var policySet = CreateMinimalPolicySet(id);
                var xml1 = _serializer.Serialize(policySet);
                var xml2 = _serializer.Serialize(policySet);

                xml1.ShouldBe(xml2, "Serializing the same input twice must produce identical XML");
            });
    }

    [Property(MaxTest = 50)]
    public Property Policy_SerializeTwice_ProducesSameXml()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            id =>
            {
                var policy = CreateMinimalPolicy(id);
                var xml1 = _serializer.Serialize(policy);
                var xml2 = _serializer.Serialize(policy);

                xml1.ShouldBe(xml2, "Serializing the same input twice must produce identical XML");
            });
    }

    #endregion

    // ── Cross-Serializer Compatibility ─────────────────────────────

    #region Cross-Serializer Compatibility

    [Property(MaxTest = 30)]
    public Property XacmlXml_SerializesValidXml_ForAnyPolicySetId()
    {
        return Prop.ForAll(
            XmlSafeStringArb(),
            id =>
            {
                var policySet = CreateMinimalPolicySet(id);
                var xml = _serializer.Serialize(policySet);

                // Should contain valid XML declaration and namespace
                xml.Contains("<?xml").ShouldBeTrue("Must contain XML declaration");
                xml.Contains("urn:oasis:names:tc:xacml:3.0:core:schema:wd-17")
                    .ShouldBeTrue("Must contain XACML namespace");
            });
    }

    #endregion

    // ── Helpers ────────────────────────────────────────────────────

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
}
