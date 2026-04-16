using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using Shouldly;

using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Unit tests for <see cref="DefaultPolicySerializer"/>: verifies JSON round-trip
/// fidelity for XACML policy graphs including polymorphic <see cref="IExpression"/>
/// trees, enum serialization, and error handling for malformed input.
/// </summary>
public sealed class DefaultPolicySerializerTests
{
    private readonly DefaultPolicySerializer _sut = new();

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
                                    FunctionId = "urn:oasis:names:tc:xacml:1.0:function:string-equal",
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
                    FunctionId = "urn:oasis:names:tc:xacml:1.0:function:string-equal",
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
                    FunctionId = "urn:oasis:names:tc:xacml:1.0:function:string-equal",
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
        var json = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(json);

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
        var json = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(json);

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
        var json = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(json);

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
        var json = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(json);

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
        var json = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(json);

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
        var json = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(json);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var condition = deserialized.Rules[0].Condition;
        condition.ShouldNotBeNull();
        condition.ShouldBeOfType<Apply>();
        condition!.FunctionId.ShouldBe("urn:oasis:names:tc:xacml:1.0:function:string-equal");
        condition.Arguments.Count.ShouldBe(2);
        condition.Arguments[0].ShouldBeOfType<AttributeDesignator>();
        condition.Arguments[1].ShouldBeOfType<AttributeValue>();
    }

    [Fact]
    public void Serialize_DeserializePolicy_VariableDefinitionExpression_RoundTrips()
    {
        // Arrange — VariableDefinition.Expression is IExpression, test with VariableReference
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
        var json = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(json);

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
        var json = _sut.Serialize(policySet);
        var result = _sut.DeserializePolicySet(json);

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
        var json = _sut.Serialize(policy);
        var result = _sut.DeserializePolicy(json);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Rules[0].Effect.ShouldBe(effect);
    }

    #endregion

    // ── JSON Format ─────────────────────────────────────────────────

    #region JSON Format

    [Fact]
    public void Serialize_PolicySet_ProducesCompactJson()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var json = _sut.Serialize(policySet);

        // Assert — WriteIndented = false → no newlines or indentation
        json.ShouldNotContain("\n");
        json.ShouldNotContain("  ");
    }

    [Fact]
    public void Serialize_PolicySet_UsesCamelCase()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var json = _sut.Serialize(policySet);

        // Assert
        json.ShouldContain("\"id\":");
        json.ShouldContain("\"algorithm\":");
        json.ShouldNotContain("\"Id\":");
    }

    [Fact]
    public void Serialize_PolicySet_OmitsNullProperties()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var json = _sut.Serialize(policySet);

        // Assert — Target is null, should be omitted
        json.ShouldNotContain("\"target\":");
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
    public void DeserializePolicySet_MalformedJson_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet("{not valid json}");

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
    public void DeserializePolicy_MalformedJson_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy("{not valid json}");

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

    // ── CreateSerializerOptions ──────────────────────────────────────

    #region CreateSerializerOptions

    [Fact]
    public void CreateSerializerOptions_ConfiguresCamelCase()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.PropertyNamingPolicy.ShouldBe(System.Text.Json.JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void CreateSerializerOptions_ConfiguresCaseInsensitiveReading()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.PropertyNameCaseInsensitive.ShouldBeTrue();
    }

    [Fact]
    public void CreateSerializerOptions_DisablesIndentation()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.WriteIndented.ShouldBeFalse();
    }

    [Fact]
    public void CreateSerializerOptions_IgnoresNullValues()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.DefaultIgnoreCondition.ShouldBe(
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void CreateSerializerOptions_IncludesExpressionJsonConverter()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.Converters.ShouldContain(c => c is ExpressionJsonConverter);
    }

    [Fact]
    public void CreateSerializerOptions_IncludesJsonStringEnumConverter()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.Converters.ShouldContain(
            c => c is System.Text.Json.Serialization.JsonStringEnumConverter);
    }

    #endregion
}
