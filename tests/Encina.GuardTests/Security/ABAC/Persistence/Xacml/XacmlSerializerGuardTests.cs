using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Persistence.Xacml;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rule = Encina.Security.ABAC.Rule;
using Target = Encina.Security.ABAC.Target;

namespace Encina.GuardTests.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Guard clause tests for <see cref="XacmlXmlPolicySerializer"/>.
/// Covers constructor guards, serialize null guards, deserialize null/empty/invalid inputs,
/// and round-trip validation of complex policy structures.
/// </summary>
public class XacmlSerializerGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new XacmlXmlPolicySerializer(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidLogger_DoesNotThrow()
    {
        var act = () => CreateSerializer();

        act.Should().NotThrow();
    }

    #endregion

    #region Serialize PolicySet — Guards

    [Fact]
    public void Serialize_NullPolicySet_ThrowsArgumentNullException()
    {
        var sut = CreateSerializer();

        var act = () => sut.Serialize((PolicySet)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policySet");
    }

    [Fact]
    public void Serialize_ValidPolicySet_ReturnsXml()
    {
        var sut = CreateSerializer();
        var ps = CreateMinimalPolicySet();

        var xml = sut.Serialize(ps);

        xml.Should().NotBeNullOrWhiteSpace();
        xml.Should().Contain("PolicySet");
        xml.Should().Contain("ps-1");
    }

    [Fact]
    public void Serialize_PolicySetWithDescription_IncludesDescription()
    {
        var sut = CreateSerializer();
        var ps = CreateMinimalPolicySet() with { Description = "Test policy set" };

        var xml = sut.Serialize(ps);

        xml.Should().Contain("Test policy set");
    }

    [Fact]
    public void Serialize_PolicySetWithNestedPolicies_IncludesPolicies()
    {
        var sut = CreateSerializer();
        var ps = CreateMinimalPolicySet() with
        {
            Policies = [CreateMinimalPolicy()]
        };

        var xml = sut.Serialize(ps);

        xml.Should().Contain("pol-1");
    }

    [Fact]
    public void Serialize_PolicySetWithNestedPolicySets_IncludesNestedSets()
    {
        var sut = CreateSerializer();
        var nested = CreateMinimalPolicySet() with { Id = "nested-ps" };
        var ps = CreateMinimalPolicySet() with
        {
            PolicySets = [nested]
        };

        var xml = sut.Serialize(ps);

        xml.Should().Contain("nested-ps");
    }

    [Fact]
    public void Serialize_PolicySetWithObligations_IncludesObligations()
    {
        var sut = CreateSerializer();
        var ps = CreateMinimalPolicySet() with
        {
            Obligations =
            [
                new Obligation
                {
                    Id = "ob-1",
                    FulfillOn = FulfillOn.Permit,
                    AttributeAssignments = []
                }
            ]
        };

        var xml = sut.Serialize(ps);

        xml.Should().Contain("ob-1");
    }

    [Fact]
    public void Serialize_PolicySetWithAdvice_IncludesAdvice()
    {
        var sut = CreateSerializer();
        var ps = CreateMinimalPolicySet() with
        {
            Advice =
            [
                new AdviceExpression
                {
                    Id = "advice-1",
                    AppliesTo = FulfillOn.Deny,
                    AttributeAssignments = []
                }
            ]
        };

        var xml = sut.Serialize(ps);

        xml.Should().Contain("advice-1");
    }

    #endregion

    #region Serialize Policy — Guards

    [Fact]
    public void Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        var sut = CreateSerializer();

        var act = () => sut.Serialize((Policy)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

    [Fact]
    public void Serialize_ValidPolicy_ReturnsXml()
    {
        var sut = CreateSerializer();
        var policy = CreateMinimalPolicy();

        var xml = sut.Serialize(policy);

        xml.Should().NotBeNullOrWhiteSpace();
        xml.Should().Contain("Policy");
        xml.Should().Contain("pol-1");
    }

    [Fact]
    public void Serialize_PolicyWithRules_IncludesRules()
    {
        var sut = CreateSerializer();
        var policy = CreateMinimalPolicy() with
        {
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Condition = null,
                    Obligations = [],
                    Advice = []
                }
            ]
        };

        var xml = sut.Serialize(policy);

        xml.Should().Contain("rule-1");
    }

    [Fact]
    public void Serialize_PolicyWithCondition_IncludesCondition()
    {
        var sut = CreateSerializer();
        var condition = new Apply
        {
            FunctionId = "string-equal",
            Arguments =
            [
                new AttributeDesignator
                {
                    Category = AttributeCategory.Subject,
                    AttributeId = "role",
                    DataType = XACMLDataTypes.String,
                    MustBePresent = false
                },
                new AttributeValue
                {
                    DataType = XACMLDataTypes.String,
                    Value = "admin"
                }
            ]
        };

        var policy = CreateMinimalPolicy() with
        {
            Rules =
            [
                new Rule
                {
                    Id = "cond-rule",
                    Effect = Effect.Permit,
                    Target = null,
                    Condition = condition,
                    Obligations = [],
                    Advice = []
                }
            ]
        };

        var xml = sut.Serialize(policy);

        xml.Should().Contain("Condition");
        xml.Should().Contain("Apply");
    }

    [Fact]
    public void Serialize_PolicyWithVariableDefinition_IncludesVariable()
    {
        var sut = CreateSerializer();
        var policy = CreateMinimalPolicy() with
        {
            VariableDefinitions =
            [
                new VariableDefinition
                {
                    VariableId = "threshold",
                    Expression = new AttributeValue
                    {
                        DataType = XACMLDataTypes.Integer,
                        Value = 100
                    }
                }
            ]
        };

        var xml = sut.Serialize(policy);

        xml.Should().Contain("threshold");
    }

    [Fact]
    public void Serialize_PolicyWithTarget_IncludesTarget()
    {
        var sut = CreateSerializer();
        var policy = CreateMinimalPolicy() with
        {
            Target = new Target
            {
                AnyOfElements =
                [
                    new AnyOf
                    {
                        AllOfElements =
                        [
                            new AllOf
                            {
                                Matches =
                                [
                                    new Match
                                    {
                                        FunctionId = "string-equal",
                                        AttributeValue = new AttributeValue
                                        {
                                            DataType = XACMLDataTypes.String,
                                            Value = "read"
                                        },
                                        AttributeDesignator = new AttributeDesignator
                                        {
                                            Category = AttributeCategory.Action,
                                            AttributeId = "action-id",
                                            DataType = XACMLDataTypes.String,
                                            MustBePresent = false
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var xml = sut.Serialize(policy);

        xml.Should().Contain("Target");
        xml.Should().Contain("AnyOf");
        xml.Should().Contain("AllOf");
        xml.Should().Contain("Match");
    }

    #endregion

    #region DeserializePolicySet — Guards and Error Cases

    [Fact]
    public void DeserializePolicySet_NullData_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicySet(null!);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicySet_EmptyData_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicySet("");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicySet_WhitespaceData_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicySet("   ");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicySet_InvalidXml_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicySet("<not-valid-xml");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicySet_WrongRootElement_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicySet("<Policy xmlns=\"urn:oasis:names:tc:xacml:3.0:core:schema:wd-17\" />");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region DeserializePolicy — Guards and Error Cases

    [Fact]
    public void DeserializePolicy_NullData_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicy(null!);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_EmptyData_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicy("");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_WhitespaceData_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicy("   ");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_InvalidXml_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicy("<broken xml>>");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_WrongRootElement_ReturnsError()
    {
        var sut = CreateSerializer();

        var result = sut.DeserializePolicy("<PolicySet xmlns=\"urn:oasis:names:tc:xacml:3.0:core:schema:wd-17\" />");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Round-Trip — Serialize then Deserialize

    [Fact]
    public void RoundTrip_MinimalPolicySet_PreservesStructure()
    {
        var sut = CreateSerializer();
        var original = CreateMinimalPolicySet();

        var xml = sut.Serialize(original);
        var result = sut.DeserializePolicySet(xml);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: ps =>
            {
                ps.Id.Should().Be(original.Id);
                ps.IsEnabled.Should().Be(original.IsEnabled);
                return 0;
            });
    }

    [Fact]
    public void RoundTrip_MinimalPolicy_PreservesStructure()
    {
        var sut = CreateSerializer();
        var original = CreateMinimalPolicy();

        var xml = sut.Serialize(original);
        var result = sut.DeserializePolicy(xml);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: pol =>
            {
                pol.Id.Should().Be(original.Id);
                pol.IsEnabled.Should().Be(original.IsEnabled);
                return 0;
            });
    }

    [Fact]
    public void RoundTrip_PolicySetWithDisabledFlag_PreservesFlag()
    {
        var sut = CreateSerializer();
        var original = CreateMinimalPolicySet() with { IsEnabled = false };

        var xml = sut.Serialize(original);
        var result = sut.DeserializePolicySet(xml);

        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: ps => { ps.IsEnabled.Should().BeFalse(); return 0; });
    }

    [Fact]
    public void RoundTrip_PolicySetWithPriority_PreservesPriority()
    {
        var sut = CreateSerializer();
        var original = CreateMinimalPolicySet() with { Priority = 42 };

        var xml = sut.Serialize(original);
        var result = sut.DeserializePolicySet(xml);

        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: ps => { ps.Priority.Should().Be(42); return 0; });
    }

    [Fact]
    public void RoundTrip_PolicyWithRulesAndConditions_PreservesAll()
    {
        var sut = CreateSerializer();
        var original = CreateMinimalPolicy() with
        {
            Rules =
            [
                new Rule
                {
                    Id = "rule-rt",
                    Effect = Effect.Deny,
                    Target = null,
                    Condition = new Apply
                    {
                        FunctionId = "string-equal",
                        Arguments =
                        [
                            new AttributeDesignator
                            {
                                Category = AttributeCategory.Subject,
                                AttributeId = "role",
                                DataType = XACMLDataTypes.String,
                                MustBePresent = false
                            },
                            new AttributeValue
                            {
                                DataType = XACMLDataTypes.String,
                                Value = "guest"
                            }
                        ]
                    },
                    Obligations = [],
                    Advice = []
                }
            ]
        };

        var xml = sut.Serialize(original);
        var result = sut.DeserializePolicy(xml);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: pol =>
            {
                pol.Rules.Should().HaveCount(1);
                pol.Rules[0].Id.Should().Be("rule-rt");
                pol.Rules[0].Effect.Should().Be(Effect.Deny);
                pol.Rules[0].Condition.Should().NotBeNull();
                return 0;
            });
    }

    [Fact]
    public void RoundTrip_PolicyWithObligationsAndAdvice_PreservesAll()
    {
        var sut = CreateSerializer();
        var original = CreateMinimalPolicy() with
        {
            Obligations =
            [
                new Obligation
                {
                    Id = "ob-rt",
                    FulfillOn = FulfillOn.Permit,
                    AttributeAssignments =
                    [
                        new AttributeAssignment
                        {
                            AttributeId = "log-message",
                            Category = AttributeCategory.Environment,
                            Value = new AttributeValue
                            {
                                DataType = XACMLDataTypes.String,
                                Value = "Access granted"
                            }
                        }
                    ]
                }
            ],
            Advice =
            [
                new AdviceExpression
                {
                    Id = "adv-rt",
                    AppliesTo = FulfillOn.Deny,
                    AttributeAssignments = []
                }
            ]
        };

        var xml = sut.Serialize(original);
        var result = sut.DeserializePolicy(xml);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: pol =>
            {
                pol.Obligations.Should().HaveCount(1);
                pol.Obligations[0].Id.Should().Be("ob-rt");
                pol.Obligations[0].AttributeAssignments.Should().HaveCount(1);
                pol.Advice.Should().HaveCount(1);
                pol.Advice[0].Id.Should().Be("adv-rt");
                return 0;
            });
    }

    [Fact]
    public void RoundTrip_PolicyWithVariableDefinition_PreservesVariables()
    {
        var sut = CreateSerializer();
        var original = CreateMinimalPolicy() with
        {
            VariableDefinitions =
            [
                new VariableDefinition
                {
                    VariableId = "maxRetries",
                    Expression = new AttributeValue
                    {
                        DataType = XACMLDataTypes.Integer,
                        Value = 3
                    }
                }
            ]
        };

        var xml = sut.Serialize(original);
        var result = sut.DeserializePolicy(xml);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Left: _ => throw new InvalidOperationException(),
            Right: pol =>
            {
                pol.VariableDefinitions.Should().HaveCount(1);
                pol.VariableDefinitions[0].VariableId.Should().Be("maxRetries");
                return 0;
            });
    }

    #endregion

    // ── Helpers ──────────────────────────────────────────────────────

    private static XacmlXmlPolicySerializer CreateSerializer() =>
        new(NullLoggerFactory.Instance.CreateLogger<XacmlXmlPolicySerializer>());

    private static PolicySet CreateMinimalPolicySet() => new()
    {
        Id = "ps-1",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = [],
        IsEnabled = true,
        Priority = 0
    };

    private static Policy CreateMinimalPolicy() => new()
    {
        Id = "pol-1",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = [],
        IsEnabled = true,
        Priority = 0
    };
}
