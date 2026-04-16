using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Encina.Security.ABAC.Persistence.Xacml;

using Shouldly;

using Microsoft.Extensions.Logging.Abstractions;

using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Persistence.Xacml;

/// <summary>
/// Unit tests for <see cref="XacmlXmlPolicySerializer"/>: verifies XACML 3.0 XML
/// round-trip fidelity for ABAC policy graphs including polymorphic <see cref="IExpression"/>
/// trees, enum serialization, Encina extension attributes, and error handling.
/// </summary>
public sealed class XacmlXmlPolicySerializerTests
{
    private readonly XacmlXmlPolicySerializer _sut =
        new(NullLoggerFactory.Instance.CreateLogger<XacmlXmlPolicySerializer>());

    // ── Helpers ──────────────────────────────────────────────────────

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

    private static PolicySet CreatePolicySetWithMetadata() => new()
    {
        Id = "ps-full",
        Version = "2.1.0",
        Description = "Full policy set with metadata",
        Target = null,
        Algorithm = CombiningAlgorithmId.PermitOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = [],
        IsEnabled = false,
        Priority = 5
    };

    private static Policy CreatePolicyWithRules() => new()
    {
        Id = "p-rules",
        Version = "1.0",
        Description = "Policy with rules and targets",
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
                                        DataType = "string",
                                        Value = "read"
                                    },
                                    AttributeDesignator = new AttributeDesignator
                                    {
                                        AttributeId = "action-id",
                                        Category = AttributeCategory.Action,
                                        DataType = "string",
                                        MustBePresent = true
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        },
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules =
        [
            new Rule
            {
                Id = "rule-1",
                Effect = Effect.Permit,
                Description = "Allow read access",
                Obligations = [],
                Advice = [],
                Condition = new Apply
                {
                    FunctionId = "string-equal",
                    Arguments =
                    [
                        new AttributeDesignator
                        {
                            AttributeId = "role",
                            Category = AttributeCategory.Subject,
                            DataType = "string",
                            MustBePresent = true
                        },
                        new AttributeValue { DataType = "string", Value = "admin" }
                    ]
                }
            }
        ],
        Obligations =
        [
            new Obligation
            {
                Id = "obligation-audit",
                FulfillOn = FulfillOn.Permit,
                AttributeAssignments =
                [
                    new AttributeAssignment
                    {
                        AttributeId = "audit-action",
                        Value = new AttributeValue { DataType = "string", Value = "read-access" }
                    }
                ]
            }
        ],
        Advice =
        [
            new AdviceExpression
            {
                Id = "advice-log",
                AppliesTo = FulfillOn.Permit,
                AttributeAssignments =
                [
                    new AttributeAssignment
                    {
                        AttributeId = "log-message",
                        Value = new AttributeValue { DataType = "string", Value = "Access granted" }
                    }
                ]
            }
        ],
        VariableDefinitions =
        [
            new VariableDefinition
            {
                VariableId = "is-admin",
                Expression = new Apply
                {
                    FunctionId = "string-equal",
                    Arguments =
                    [
                        new AttributeDesignator
                        {
                            AttributeId = "role",
                            Category = AttributeCategory.Subject,
                            DataType = "string",
                            MustBePresent = true
                        },
                        new AttributeValue { DataType = "string", Value = "admin" }
                    ]
                }
            }
        ]
    };

    private static PolicySet CreateNestedPolicySet() => new()
    {
        Id = "ps-root",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies =
        [
            CreateMinimalPolicy("nested-p-1")
        ],
        PolicySets =
        [
            new PolicySet
            {
                Id = "ps-child",
                Target = null,
                Algorithm = CombiningAlgorithmId.PermitOverrides,
                Policies = [CreateMinimalPolicy("nested-p-2")],
                PolicySets = [],
                Obligations = [],
                Advice = []
            }
        ],
        Obligations = [],
        Advice = []
    };

    // ── PolicySet Serialization Round-Trip ──────────────────────────

    #region PolicySet — Round-Trip

    [Fact]
    public void Serialize_DeserializePolicySet_MinimalPolicySet_RoundTrips()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Id.ShouldBe(policySet.Id);
        deserialized.Algorithm.ShouldBe(policySet.Algorithm);
        deserialized.Policies.ShouldBeEmpty();
        deserialized.PolicySets.ShouldBeEmpty();
    }

    [Fact]
    public void Serialize_DeserializePolicySet_WithMetadata_PreservesAllFields()
    {
        // Arrange
        var policySet = CreatePolicySetWithMetadata();

        // Act
        var xml = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Id.ShouldBe("ps-full");
        deserialized.Version.ShouldBe("2.1.0");
        deserialized.Description.ShouldBe("Full policy set with metadata");
        deserialized.Algorithm.ShouldBe(CombiningAlgorithmId.PermitOverrides);
        deserialized.IsEnabled.ShouldBeFalse();
        deserialized.Priority.ShouldBe(5);
    }

    [Fact]
    public void Serialize_DeserializePolicySet_NestedHierarchy_RoundTrips()
    {
        // Arrange
        var policySet = CreateNestedPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Id.ShouldBe("ps-root");
        deserialized.Policies.Count.ShouldBe(1);
        deserialized.Policies[0].Id.ShouldBe("nested-p-1");
        deserialized.PolicySets.Count.ShouldBe(1);
        deserialized.PolicySets[0].Id.ShouldBe("ps-child");
        deserialized.PolicySets[0].Policies.Count.ShouldBe(1);
        deserialized.PolicySets[0].Policies[0].Id.ShouldBe("nested-p-2");
    }

    #endregion

    // ── Policy Serialization Round-Trip ─────────────────────────────

    #region Policy — Round-Trip

    [Fact]
    public void Serialize_DeserializePolicy_MinimalPolicy_RoundTrips()
    {
        // Arrange
        var policy = CreateMinimalPolicy();

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Id.ShouldBe(policy.Id);
        deserialized.Algorithm.ShouldBe(policy.Algorithm);
        deserialized.Rules.ShouldBeEmpty();
    }

    [Fact]
    public void Serialize_DeserializePolicy_WithRulesAndExpressions_RoundTrips()
    {
        // Arrange
        var policy = CreatePolicyWithRules();

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Id.ShouldBe("p-rules");
        deserialized.Version.ShouldBe("1.0");
        deserialized.Description.ShouldBe("Policy with rules and targets");
        deserialized.Rules.Count.ShouldBe(1);
        deserialized.Rules[0].Id.ShouldBe("rule-1");
        deserialized.Rules[0].Effect.ShouldBe(Effect.Permit);
        deserialized.Obligations.Count.ShouldBe(1);
        deserialized.Obligations[0].Id.ShouldBe("obligation-audit");
        deserialized.Advice.Count.ShouldBe(1);
        deserialized.Advice[0].Id.ShouldBe("advice-log");
        deserialized.VariableDefinitions.Count.ShouldBe(1);
        deserialized.VariableDefinitions[0].VariableId.ShouldBe("is-admin");
    }

    [Fact]
    public void Serialize_DeserializePolicy_PolymorphicCondition_Apply_RoundTrips()
    {
        // Arrange
        var policy = CreatePolicyWithRules();

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var condition = deserialized.Rules[0].Condition;
        condition.ShouldNotBeNull();
        condition.ShouldBeOfType<Apply>();
        condition!.FunctionId.ShouldContain("string-equal");
        condition.Arguments.Count.ShouldBe(2);
        condition.Arguments[0].ShouldBeOfType<AttributeDesignator>();
        condition.Arguments[1].ShouldBeOfType<AttributeValue>();
    }

    [Fact]
    public void Serialize_DeserializePolicy_VariableDefinitionExpression_RoundTrips()
    {
        // Arrange
        var policy = new Policy
        {
            Id = "p-varref",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [],
            Obligations = [],
            Advice = [],
            VariableDefinitions =
            [
                new VariableDefinition
                {
                    VariableId = "is-admin",
                    Expression = new AttributeValue { DataType = "boolean", Value = true }
                },
                new VariableDefinition
                {
                    VariableId = "check-admin",
                    Expression = new VariableReference { VariableId = "is-admin" }
                }
            ]
        };

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.VariableDefinitions.Count.ShouldBe(2);
        deserialized.VariableDefinitions[0].Expression.ShouldBeOfType<AttributeValue>();
        deserialized.VariableDefinitions[1].Expression.ShouldBeOfType<VariableReference>();
        var varRef = (VariableReference)deserialized.VariableDefinitions[1].Expression;
        varRef.VariableId.ShouldBe("is-admin");
    }

    #endregion

    // ── Enum Serialization ──────────────────────────────────────────

    #region Enum Serialization

    [Theory]
    [InlineData(CombiningAlgorithmId.DenyOverrides)]
    [InlineData(CombiningAlgorithmId.PermitOverrides)]
    [InlineData(CombiningAlgorithmId.FirstApplicable)]
    [InlineData(CombiningAlgorithmId.DenyUnlessPermit)]
    [InlineData(CombiningAlgorithmId.PermitUnlessDeny)]
    public void Serialize_DeserializePolicySet_CombiningAlgorithm_RoundTrips(
        CombiningAlgorithmId algorithm)
    {
        // Arrange
        var policySet = CreateMinimalPolicySet() with { Algorithm = algorithm };

        // Act
        var xml = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Algorithm.ShouldBe(algorithm);
    }

    [Theory]
    [InlineData(Effect.Permit)]
    [InlineData(Effect.Deny)]
    public void Serialize_DeserializePolicy_RuleEffect_RoundTrips(Effect effect)
    {
        // Arrange
        var policy = new Policy
        {
            Id = "p-effect",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "r-1",
                    Effect = effect,
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Rules[0].Effect.ShouldBe(effect);
    }

    #endregion

    // ── XML Format ──────────────────────────────────────────────────

    #region XML Format

    [Fact]
    public void Serialize_PolicySet_ProducesXmlDeclaration()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert
        xml.ShouldStartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
    }

    [Fact]
    public void Serialize_PolicySet_ContainsXacmlNamespace()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert
        xml.ShouldContain("urn:oasis:names:tc:xacml:3.0:core:schema:wd-17");
    }

    [Fact]
    public void Serialize_PolicySet_ContainsEncinaExtensionNamespace()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert
        xml.ShouldContain("urn:encina:xacml:extensions:1.0");
    }

    [Fact]
    public void Serialize_PolicySet_ContainsPolicySetElement()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert
        xml.ShouldContain("<PolicySet ");
        xml.ShouldContain("PolicySetId=\"ps-1\"");
    }

    [Fact]
    public void Serialize_Policy_ContainsPolicyElement()
    {
        // Arrange
        var policy = CreateMinimalPolicy();

        // Act
        var xml = _sut.Serialize(policy);

        // Assert
        xml.ShouldContain("<Policy ");
        xml.ShouldContain("PolicyId=\"p-1\"");
    }

    [Fact]
    public void Serialize_PolicySet_ContainsCombiningAlgorithmUrn()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert
        xml.ShouldContain("PolicyCombiningAlgId=\"urn:oasis:names:tc:xacml:3.0:policy-combining-algorithm:deny-overrides\"");
    }

    [Fact]
    public void Serialize_Policy_ContainsRuleCombiningAlgorithmUrn()
    {
        // Arrange
        var policy = CreateMinimalPolicy();

        // Act
        var xml = _sut.Serialize(policy);

        // Assert
        xml.ShouldContain("RuleCombiningAlgId=\"urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides\"");
    }

    #endregion

    // ── XACML Target Serialization ──────────────────────────────────

    #region Target Serialization

    [Fact]
    public void Serialize_DeserializePolicy_Target_WithMatch_RoundTrips()
    {
        // Arrange
        var policy = CreatePolicyWithRules();

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Target.ShouldNotBeNull();
        deserialized.Target!.AnyOfElements.Count.ShouldBe(1);
        deserialized.Target.AnyOfElements[0].AllOfElements.Count.ShouldBe(1);
        deserialized.Target.AnyOfElements[0].AllOfElements[0].Matches.Count.ShouldBe(1);

        var match = deserialized.Target.AnyOfElements[0].AllOfElements[0].Matches[0];
        match.AttributeDesignator.Category.ShouldBe(AttributeCategory.Action);
        match.AttributeDesignator.AttributeId.ShouldBe("action-id");
    }

    [Fact]
    public void Serialize_PolicySet_NullTarget_OmitsTargetElement()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert — Null target is omitted; XACML allows absent Target (matches everything)
        xml.ShouldNotContain("<Target");
    }

    #endregion

    // ── Obligation & Advice Serialization ────────────────────────────

    #region Obligations & Advice

    [Fact]
    public void Serialize_DeserializePolicy_Obligations_RoundTrips()
    {
        // Arrange
        var policy = CreatePolicyWithRules();

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Obligations.Count.ShouldBe(1);
        var obligation = deserialized.Obligations[0];
        obligation.Id.ShouldBe("obligation-audit");
        obligation.FulfillOn.ShouldBe(FulfillOn.Permit);
        obligation.AttributeAssignments.Count.ShouldBe(1);
        obligation.AttributeAssignments[0].AttributeId.ShouldBe("audit-action");
    }

    [Fact]
    public void Serialize_DeserializePolicy_Advice_RoundTrips()
    {
        // Arrange
        var policy = CreatePolicyWithRules();

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Advice.Count.ShouldBe(1);
        var advice = deserialized.Advice[0];
        advice.Id.ShouldBe("advice-log");
        advice.AppliesTo.ShouldBe(FulfillOn.Permit);
        advice.AttributeAssignments.Count.ShouldBe(1);
        advice.AttributeAssignments[0].AttributeId.ShouldBe("log-message");
    }

    [Theory]
    [InlineData(FulfillOn.Permit)]
    [InlineData(FulfillOn.Deny)]
    public void Serialize_DeserializePolicy_ObligationFulfillOn_RoundTrips(FulfillOn fulfillOn)
    {
        // Arrange
        var policy = new Policy
        {
            Id = "p-obl",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [],
            Obligations =
            [
                new Obligation
                {
                    Id = "obl-1",
                    FulfillOn = fulfillOn,
                    AttributeAssignments = []
                }
            ],
            Advice = [],
            VariableDefinitions = []
        };

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Obligations[0].FulfillOn.ShouldBe(fulfillOn);
    }

    #endregion

    // ── Encina Extension Attributes ─────────────────────────────────

    #region Encina Extensions

    [Fact]
    public void Serialize_PolicySet_IsEnabledFalse_ProducesEncinaAttribute()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet() with { IsEnabled = false };

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert
        xml.ShouldContain("encina:IsEnabled=\"false\"");
    }

    [Fact]
    public void Serialize_PolicySet_Priority_ProducesEncinaAttribute()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet() with { Priority = 42 };

        // Act
        var xml = _sut.Serialize(policySet);

        // Assert
        xml.ShouldContain("encina:Priority=\"42\"");
    }

    [Fact]
    public void DeserializePolicySet_WithoutEncinaExtensions_AppliesDefaults()
    {
        // Arrange — Standard XACML XML without Encina extension attributes
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <PolicySet xmlns="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"
                       PolicySetId="ps-ext"
                       Version="1.0"
                       PolicyCombiningAlgId="urn:oasis:names:tc:xacml:3.0:policy-combining-algorithm:deny-overrides">
                <Target />
            </PolicySet>
            """;

        // Act
        var result = _sut.DeserializePolicySet(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.IsEnabled.ShouldBeTrue("Default IsEnabled should be true");
        deserialized.Priority.ShouldBe(0, "Default Priority should be 0");
    }

    [Fact]
    public void DeserializePolicy_WithoutEncinaExtensions_AppliesDefaults()
    {
        // Arrange — Standard XACML XML without Encina extensions
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Policy xmlns="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"
                    PolicyId="p-ext"
                    Version="1.0"
                    RuleCombiningAlgId="urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides">
                <Target />
            </Policy>
            """;

        // Act
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.IsEnabled.ShouldBeTrue("Default IsEnabled should be true");
        deserialized.Priority.ShouldBe(0, "Default Priority should be 0");
    }

    #endregion

    // ── Data Type Round-Trip ────────────────────────────────────────

    #region Data Type Round-Trip

    [Theory]
    [InlineData("string", "hello world")]
    [InlineData("string", "")]
    public void Serialize_DeserializePolicy_StringAttributeValue_RoundTrips(
        string dataType, string value)
    {
        // Arrange
        var policy = CreatePolicyWithAttributeValue(dataType, value);

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var av = GetFirstAttributeValue(deserialized);
        av.Value?.ToString().ShouldBe(value);
    }

    [Fact]
    public void Serialize_DeserializePolicy_BooleanAttributeValue_RoundTrips()
    {
        // Arrange
        var policy = CreatePolicyWithAttributeValue(XACMLDataTypes.Boolean, true);

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var av = GetFirstAttributeValue(deserialized);
        av.Value.ShouldBe(true);
    }

    [Fact]
    public void Serialize_DeserializePolicy_IntegerAttributeValue_RoundTrips()
    {
        // Arrange
        var policy = CreatePolicyWithAttributeValue(XACMLDataTypes.Integer, 42);

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var av = GetFirstAttributeValue(deserialized);
        Convert.ToInt64(av.Value, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(42);
    }

    #endregion

    // ── Deserialization Error Handling ───────────────────────────────

    #region Deserialization — Errors

    [Fact]
    public void DeserializePolicySet_NullInput_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet(null!);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_EmptyString_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet("");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_WhitespaceOnly_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet("   ");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_MalformedXml_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet("<not valid xml>");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_NullInput_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy(null!);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_EmptyString_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy("");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_WhitespaceOnly_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy("   ");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_MalformedXml_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy("<not valid xml>");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicySet_WrongRootElement_ReturnsLeft()
    {
        // Arrange — Valid XML but wrong root element
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Policy xmlns="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"
                    PolicyId="p-1"
                    Version="1.0"
                    RuleCombiningAlgId="urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides">
                <Target />
            </Policy>
            """;

        // Act
        var result = _sut.DeserializePolicySet(xml);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void DeserializePolicy_WrongRootElement_ReturnsLeft()
    {
        // Arrange — Valid XML but wrong root element
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <PolicySet xmlns="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"
                       PolicySetId="ps-1"
                       Version="1.0"
                       PolicyCombiningAlgId="urn:oasis:names:tc:xacml:3.0:policy-combining-algorithm:deny-overrides">
                <Target />
            </PolicySet>
            """;

        // Act
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    // ── Serialize Guard Clauses ──────────────────────────────────────

    #region Serialize — Guard Clauses

    [Fact]
    public void Serialize_NullPolicySet_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Serialize((PolicySet)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Serialize((Policy)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    // ── Function URN Round-Trip ──────────────────────────────────────

    #region Function URN Round-Trip

    [Fact]
    public void Serialize_Policy_FunctionId_ConvertedToUrn()
    {
        // Arrange
        var policy = CreatePolicyWithRules();

        // Act
        var xml = _sut.Serialize(policy);

        // Assert — Short IDs are expanded to full URNs in XML
        xml.ShouldContain("urn:oasis:names:tc:xacml:1.0:function:string-equal");
    }

    [Fact]
    public void DeserializePolicy_FunctionUrn_ConvertedToShortId()
    {
        // Arrange
        var policy = CreatePolicyWithRules();
        var xml = _sut.Serialize(policy);

        // Act
        var result = _sut.DeserializePolicy(xml);

        // Assert — Full URNs are normalized back to short IDs
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var condition = deserialized.Rules[0].Condition as Apply;
        condition.ShouldNotBeNull();
        condition!.FunctionId.ShouldBe("string-equal");
    }

    #endregion

    // ── AttributeCategory URN Round-Trip ─────────────────────────────

    #region AttributeCategory URN Round-Trip

    [Theory]
    [InlineData(AttributeCategory.Subject)]
    [InlineData(AttributeCategory.Resource)]
    [InlineData(AttributeCategory.Action)]
    [InlineData(AttributeCategory.Environment)]
    public void Serialize_DeserializePolicy_AttributeCategory_RoundTrips(AttributeCategory category)
    {
        // Arrange
        var policy = new Policy
        {
            Id = "p-cat",
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
                                        AttributeValue = new AttributeValue { DataType = "string", Value = "test" },
                                        AttributeDesignator = new AttributeDesignator
                                        {
                                            AttributeId = "attr-1",
                                            Category = category,
                                            DataType = "string",
                                            MustBePresent = true
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        // Act
        var xml = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(xml);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var designator = deserialized.Target!.AnyOfElements[0].AllOfElements[0].Matches[0].AttributeDesignator;
        designator.Category.ShouldBe(category);
    }

    #endregion

    // ── Helpers (Data Type tests) ────────────────────────────────────

    private static Policy CreatePolicyWithAttributeValue(string dataType, object? value) => new()
    {
        Id = "p-av",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules =
        [
            new Rule
            {
                Id = "r-1",
                Effect = Effect.Permit,
                Obligations = [],
                Advice = [],
                Condition = new Apply
                {
                    FunctionId = "string-equal",
                    Arguments =
                    [
                        new AttributeValue { DataType = dataType, Value = value },
                        new AttributeValue { DataType = "string", Value = "test" }
                    ]
                }
            }
        ],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static AttributeValue GetFirstAttributeValue(Policy policy)
    {
        var apply = (Apply)policy.Rules[0].Condition!;
        return (AttributeValue)apply.Arguments[0];
    }
}
