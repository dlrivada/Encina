using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using FluentAssertions;

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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Id.Should().Be(policySet.Id);
        deserialized.Algorithm.Should().Be(policySet.Algorithm);
        deserialized.Policies.Should().BeEmpty();
        deserialized.PolicySets.Should().BeEmpty();
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Id.Should().Be("ps-full");
        deserialized.Version.Should().Be("2.1.0");
        deserialized.Description.Should().Be("Full policy set with metadata");
        deserialized.Algorithm.Should().Be(CombiningAlgorithmId.PermitOverrides);
        deserialized.IsEnabled.Should().BeFalse();
        deserialized.Priority.Should().Be(5);
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Id.Should().Be("ps-root");
        deserialized.Policies.Should().HaveCount(1);
        deserialized.Policies[0].Id.Should().Be("nested-p-1");
        deserialized.PolicySets.Should().HaveCount(1);
        deserialized.PolicySets[0].Id.Should().Be("ps-child");
        deserialized.PolicySets[0].Policies.Should().HaveCount(1);
        deserialized.PolicySets[0].Policies[0].Id.Should().Be("nested-p-2");
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Id.Should().Be(policy.Id);
        deserialized.Algorithm.Should().Be(policy.Algorithm);
        deserialized.Rules.Should().BeEmpty();
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Id.Should().Be("p-rules");
        deserialized.Version.Should().Be("1.0");
        deserialized.Description.Should().Be("Policy with rules and targets");
        deserialized.Rules.Should().HaveCount(1);
        deserialized.Rules[0].Id.Should().Be("rule-1");
        deserialized.Rules[0].Effect.Should().Be(Effect.Permit);
        deserialized.Obligations.Should().HaveCount(1);
        deserialized.Obligations[0].Id.Should().Be("obligation-audit");
        deserialized.Advice.Should().HaveCount(1);
        deserialized.Advice[0].Id.Should().Be("advice-log");
        deserialized.VariableDefinitions.Should().HaveCount(1);
        deserialized.VariableDefinitions[0].VariableId.Should().Be("is-admin");
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        var condition = deserialized.Rules[0].Condition;
        condition.Should().NotBeNull();
        condition.Should().BeOfType<Apply>();
        condition!.FunctionId.Should().Be("urn:oasis:names:tc:xacml:1.0:function:string-equal");
        condition.Arguments.Should().HaveCount(2);
        condition.Arguments[0].Should().BeOfType<AttributeDesignator>();
        condition.Arguments[1].Should().BeOfType<AttributeValue>();
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.VariableDefinitions.Should().HaveCount(2);
        deserialized.VariableDefinitions[0].Expression.Should().BeOfType<AttributeValue>();
        deserialized.VariableDefinitions[1].Expression.Should().BeOfType<VariableReference>();
        var varRef = (VariableReference)deserialized.VariableDefinitions[1].Expression;
        varRef.VariableId.Should().Be("is-admin");
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Algorithm.Should().Be(algorithm);
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
        result.IsRight.Should().BeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Rules[0].Effect.Should().Be(effect);
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
        json.Should().NotContain("\n");
        json.Should().NotContain("  ");
    }

    [Fact]
    public void Serialize_PolicySet_UsesCamelCase()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var json = _sut.Serialize(policySet);

        // Assert
        json.Should().Contain("\"id\":");
        json.Should().Contain("\"algorithm\":");
        json.Should().NotContain("\"Id\":");
    }

    [Fact]
    public void Serialize_PolicySet_OmitsNullProperties()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var json = _sut.Serialize(policySet);

        // Assert — Target is null, should be omitted
        json.Should().NotContain("\"target\":");
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
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicySet_EmptyString_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet("");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicySet_WhitespaceOnly_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet("   ");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicySet_MalformedJson_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicySet("{not valid json}");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_NullInput_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy(null!);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_EmptyString_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy("");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_WhitespaceOnly_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy("   ");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void DeserializePolicy_MalformedJson_ReturnsLeft()
    {
        // Act
        var result = _sut.DeserializePolicy("{not valid json}");

        // Assert
        result.IsLeft.Should().BeTrue();
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
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Serialize_NullPolicy_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Serialize((Policy)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        options.PropertyNamingPolicy.Should().Be(System.Text.Json.JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void CreateSerializerOptions_ConfiguresCaseInsensitiveReading()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.PropertyNameCaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void CreateSerializerOptions_DisablesIndentation()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.WriteIndented.Should().BeFalse();
    }

    [Fact]
    public void CreateSerializerOptions_IgnoresNullValues()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.DefaultIgnoreCondition.Should().Be(
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void CreateSerializerOptions_IncludesExpressionJsonConverter()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.Converters.Should().Contain(c => c is ExpressionJsonConverter);
    }

    [Fact]
    public void CreateSerializerOptions_IncludesJsonStringEnumConverter()
    {
        // Act
        var options = DefaultPolicySerializer.CreateSerializerOptions();

        // Assert
        options.Converters.Should().Contain(
            c => c is System.Text.Json.Serialization.JsonStringEnumConverter);
    }

    #endregion
}
