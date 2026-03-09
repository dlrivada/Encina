using BenchmarkDotNet.Attributes;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

namespace Encina.Benchmarks.Security.ABAC;

/// <summary>
/// Benchmarks for <see cref="DefaultPolicySerializer"/> serialization and deserialization
/// of XACML 3.0 policy graphs of varying sizes.
/// </summary>
/// <remarks>
/// <para>
/// Measures serialization throughput and memory allocation for three graph sizes:
/// </para>
/// <list type="bullet">
/// <item><description><b>Small</b>: 1 policy, 2 rules, no targets — minimal overhead baseline</description></item>
/// <item><description><b>Medium</b>: 3 policies, 5 rules each with targets — typical production policy set</description></item>
/// <item><description><b>Large</b>: 10 policies, 10 rules each with targets, conditions, obligations, advice, and variable definitions — stress test for deep expression trees</description></item>
/// </list>
/// <para>
/// Both <see cref="PolicySet"/> and standalone <see cref="Policy"/> serialization are benchmarked.
/// Deserialization benchmarks use pre-serialized JSON strings to isolate deserialization cost.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class PolicySerializerBenchmarks
{
    private DefaultPolicySerializer _serializer = default!;

    // PolicySets of varying sizes
    private PolicySet _smallPolicySet = default!;
    private PolicySet _mediumPolicySet = default!;
    private PolicySet _largePolicySet = default!;

    // Standalone policies
    private Policy _smallPolicy = default!;
    private Policy _largePolicy = default!;

    // Pre-serialized JSON for deserialization benchmarks
    private string _smallPolicySetJson = default!;
    private string _mediumPolicySetJson = default!;
    private string _largePolicySetJson = default!;
    private string _smallPolicyJson = default!;
    private string _largePolicyJson = default!;

    [GlobalSetup]
    public void Setup()
    {
        _serializer = new DefaultPolicySerializer();

        // Build policy graphs of varying complexity
        _smallPolicySet = CreateSmallPolicySet();
        _mediumPolicySet = CreateMediumPolicySet();
        _largePolicySet = CreateLargePolicySet();

        _smallPolicy = CreateSmallPolicy();
        _largePolicy = CreateLargePolicy();

        // Pre-serialize for deserialization benchmarks
        _smallPolicySetJson = _serializer.Serialize(_smallPolicySet);
        _mediumPolicySetJson = _serializer.Serialize(_mediumPolicySet);
        _largePolicySetJson = _serializer.Serialize(_largePolicySet);
        _smallPolicyJson = _serializer.Serialize(_smallPolicy);
        _largePolicyJson = _serializer.Serialize(_largePolicy);
    }

    // ── Serialization Benchmarks ────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Serialize PolicySet (Small: 1 policy, 2 rules)")]
    public string Serialize_SmallPolicySet() => _serializer.Serialize(_smallPolicySet);

    [Benchmark(Description = "Serialize PolicySet (Medium: 3 policies, 15 rules)")]
    public string Serialize_MediumPolicySet() => _serializer.Serialize(_mediumPolicySet);

    [Benchmark(Description = "Serialize PolicySet (Large: 10 policies, 100 rules)")]
    public string Serialize_LargePolicySet() => _serializer.Serialize(_largePolicySet);

    [Benchmark(Description = "Serialize Policy (Small: 2 rules, no target)")]
    public string Serialize_SmallPolicy() => _serializer.Serialize(_smallPolicy);

    [Benchmark(Description = "Serialize Policy (Large: 10 rules, target + conditions)")]
    public string Serialize_LargePolicy() => _serializer.Serialize(_largePolicy);

    // ── Deserialization Benchmarks ──────────────────────────────────────

    [Benchmark(Description = "Deserialize PolicySet (Small)")]
    public PolicySet Deserialize_SmallPolicySet() =>
        _serializer.DeserializePolicySet(_smallPolicySetJson).Match(
            Right: ps => ps,
            Left: _ => throw new InvalidOperationException("Deserialization failed"));

    [Benchmark(Description = "Deserialize PolicySet (Medium)")]
    public PolicySet Deserialize_MediumPolicySet() =>
        _serializer.DeserializePolicySet(_mediumPolicySetJson).Match(
            Right: ps => ps,
            Left: _ => throw new InvalidOperationException("Deserialization failed"));

    [Benchmark(Description = "Deserialize PolicySet (Large)")]
    public PolicySet Deserialize_LargePolicySet() =>
        _serializer.DeserializePolicySet(_largePolicySetJson).Match(
            Right: ps => ps,
            Left: _ => throw new InvalidOperationException("Deserialization failed"));

    [Benchmark(Description = "Deserialize Policy (Small)")]
    public Policy Deserialize_SmallPolicy() =>
        _serializer.DeserializePolicy(_smallPolicyJson).Match(
            Right: p => p,
            Left: _ => throw new InvalidOperationException("Deserialization failed"));

    [Benchmark(Description = "Deserialize Policy (Large)")]
    public Policy Deserialize_LargePolicy() =>
        _serializer.DeserializePolicy(_largePolicyJson).Match(
            Right: p => p,
            Left: _ => throw new InvalidOperationException("Deserialization failed"));

    // ── Round-Trip Benchmarks ───────────────────────────────────────────

    [Benchmark(Description = "Round-Trip PolicySet (Small)")]
    public PolicySet RoundTrip_SmallPolicySet()
    {
        var json = _serializer.Serialize(_smallPolicySet);
        return _serializer.DeserializePolicySet(json).Match(
            Right: ps => ps,
            Left: _ => throw new InvalidOperationException("Round-trip failed"));
    }

    [Benchmark(Description = "Round-Trip PolicySet (Large)")]
    public PolicySet RoundTrip_LargePolicySet()
    {
        var json = _serializer.Serialize(_largePolicySet);
        return _serializer.DeserializePolicySet(json).Match(
            Right: ps => ps,
            Left: _ => throw new InvalidOperationException("Round-trip failed"));
    }

    // ── Test Data Factories ─────────────────────────────────────────────

    /// <summary>
    /// Small: 1 policy with 2 rules, no targets. Minimal overhead baseline.
    /// </summary>
    private static PolicySet CreateSmallPolicySet() => new()
    {
        Id = "bench-ps-small",
        Version = "1.0",
        Description = "Small benchmark policy set",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies =
        [
            new Policy
            {
                Id = "bench-p-small",
                Description = "Small policy",
                Target = null,
                Algorithm = CombiningAlgorithmId.DenyOverrides,
                Rules =
                [
                    new Rule { Id = "r-1", Effect = Effect.Permit, Description = "Allow", Obligations = [], Advice = [] },
                    new Rule { Id = "r-2", Effect = Effect.Deny, Description = "Deny", Obligations = [], Advice = [] }
                ],
                Obligations = [],
                Advice = [],
                VariableDefinitions = []
            }
        ],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    /// <summary>
    /// Medium: 3 policies with 5 rules each, all with targets. Typical production workload.
    /// </summary>
    private static PolicySet CreateMediumPolicySet()
    {
        var policies = new List<Policy>();
        for (var p = 0; p < 3; p++)
        {
            var rules = new List<Rule>();
            for (var r = 0; r < 5; r++)
            {
                rules.Add(new Rule
                {
                    Id = $"r-med-{p}-{r}",
                    Effect = r % 2 == 0 ? Effect.Permit : Effect.Deny,
                    Description = $"Rule {r} of policy {p}",
                    Target = CreateSimpleTarget($"urn:bench:attr-{p}-{r}"),
                    Obligations = [],
                    Advice = []
                });
            }

            policies.Add(new Policy
            {
                Id = $"bench-p-med-{p}",
                Version = "1.0",
                Description = $"Medium policy {p}",
                Target = CreateSimpleTarget($"urn:bench:resource-{p}"),
                Algorithm = CombiningAlgorithmId.FirstApplicable,
                Rules = rules,
                Obligations = [],
                Advice = [],
                VariableDefinitions = []
            });
        }

        return new PolicySet
        {
            Id = "bench-ps-medium",
            Version = "1.0",
            Description = "Medium benchmark policy set (3 policies, 15 rules)",
            Target = null,
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Policies = policies,
            PolicySets = [],
            Obligations = [],
            Advice = []
        };
    }

    /// <summary>
    /// Large: 10 policies with 10 rules each. Rules include targets, conditions (Apply trees),
    /// obligations, advice, and variable definitions. Stress test for deep serialization.
    /// </summary>
    private static PolicySet CreateLargePolicySet()
    {
        var policies = new List<Policy>();
        for (var p = 0; p < 10; p++)
        {
            var rules = new List<Rule>();
            for (var r = 0; r < 10; r++)
            {
                rules.Add(new Rule
                {
                    Id = $"r-lg-{p}-{r}",
                    Effect = r % 3 == 0 ? Effect.Deny : Effect.Permit,
                    Description = $"Large rule {r} of policy {p}",
                    Target = CreateSimpleTarget($"urn:bench:lg-attr-{p}-{r}"),
                    Condition = CreateConditionExpression(p, r),
                    Obligations =
                    [
                        new Obligation
                        {
                            Id = $"obl-{p}-{r}",
                            FulfillOn = FulfillOn.Permit,
                            AttributeAssignments =
                            [
                                new AttributeAssignment
                                {
                                    AttributeId = $"urn:bench:obl-attr-{p}-{r}",
                                    Value = new AttributeValue
                                    {
                                        DataType = "http://www.w3.org/2001/XMLSchema#string",
                                        Value = $"obligation-value-{p}-{r}"
                                    }
                                }
                            ]
                        }
                    ],
                    Advice =
                    [
                        new AdviceExpression
                        {
                            Id = $"adv-{p}-{r}",
                            AppliesTo = FulfillOn.Deny,
                            AttributeAssignments =
                            [
                                new AttributeAssignment
                                {
                                    AttributeId = $"urn:bench:adv-attr-{p}-{r}",
                                    Value = new AttributeValue
                                    {
                                        DataType = "http://www.w3.org/2001/XMLSchema#string",
                                        Value = $"advice-value-{p}-{r}"
                                    }
                                }
                            ]
                        }
                    ]
                });
            }

            var variableDefinitions = new List<VariableDefinition>
            {
                new()
                {
                    VariableId = $"var-{p}-threshold",
                    Expression = new Apply
                    {
                        FunctionId = "urn:oasis:names:tc:xacml:1.0:function:integer-greater-than",
                        Arguments =
                        [
                            new AttributeDesignator
                            {
                                Category = AttributeCategory.Resource,
                                AttributeId = $"urn:bench:amount-{p}",
                                DataType = "http://www.w3.org/2001/XMLSchema#integer"
                            },
                            new AttributeValue
                            {
                                DataType = "http://www.w3.org/2001/XMLSchema#integer",
                                Value = 10000 + p
                            }
                        ]
                    }
                }
            };

            policies.Add(new Policy
            {
                Id = $"bench-p-lg-{p}",
                Version = "2.0",
                Description = $"Large policy {p} with conditions and obligations",
                Target = CreateSimpleTarget($"urn:bench:lg-resource-{p}"),
                Algorithm = CombiningAlgorithmId.DenyOverrides,
                Rules = rules,
                Obligations =
                [
                    new Obligation
                    {
                        Id = $"policy-obl-{p}",
                        FulfillOn = FulfillOn.Permit,
                        AttributeAssignments =
                        [
                            new AttributeAssignment
                            {
                                AttributeId = "urn:bench:audit-action",
                                Value = new AttributeValue
                                {
                                    DataType = "http://www.w3.org/2001/XMLSchema#string",
                                    Value = $"policy-{p}-accessed"
                                }
                            }
                        ]
                    }
                ],
                Advice = [],
                VariableDefinitions = variableDefinitions
            });
        }

        return new PolicySet
        {
            Id = "bench-ps-large",
            Version = "2.0",
            Description = "Large benchmark policy set (10 policies, 100 rules, full expression trees)",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies = policies,
            PolicySets = [],
            Obligations = [],
            Advice = []
        };
    }

    /// <summary>
    /// Small standalone policy: 2 rules, no target.
    /// </summary>
    private static Policy CreateSmallPolicy() => new()
    {
        Id = "bench-standalone-small",
        Description = "Small standalone policy",
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules =
        [
            new Rule { Id = "r-s-1", Effect = Effect.Permit, Obligations = [], Advice = [] },
            new Rule { Id = "r-s-2", Effect = Effect.Deny, Obligations = [], Advice = [] }
        ],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    /// <summary>
    /// Large standalone policy: 10 rules with targets, conditions, obligations, and advice.
    /// </summary>
    private static Policy CreateLargePolicy()
    {
        var rules = new List<Rule>();
        for (var r = 0; r < 10; r++)
        {
            rules.Add(new Rule
            {
                Id = $"r-sl-{r}",
                Effect = r % 2 == 0 ? Effect.Permit : Effect.Deny,
                Description = $"Standalone large rule {r}",
                Target = CreateSimpleTarget($"urn:bench:sl-attr-{r}"),
                Condition = CreateConditionExpression(0, r),
                Obligations =
                [
                    new Obligation
                    {
                        Id = $"sl-obl-{r}",
                        FulfillOn = FulfillOn.Permit,
                        AttributeAssignments =
                        [
                            new AttributeAssignment
                            {
                                AttributeId = $"urn:bench:sl-obl-{r}",
                                Value = new AttributeValue
                                {
                                    DataType = "http://www.w3.org/2001/XMLSchema#string",
                                    Value = $"standalone-obl-{r}"
                                }
                            }
                        ]
                    }
                ],
                Advice = []
            });
        }

        return new Policy
        {
            Id = "bench-standalone-large",
            Version = "1.0",
            Description = "Large standalone policy with 10 rules, targets, and conditions",
            Target = CreateSimpleTarget("urn:bench:sl-resource"),
            Algorithm = CombiningAlgorithmId.FirstApplicable,
            Rules = rules,
            Obligations = [],
            Advice = [],
            VariableDefinitions =
            [
                new VariableDefinition
                {
                    VariableId = "sl-var-flag",
                    Expression = new Apply
                    {
                        FunctionId = "urn:oasis:names:tc:xacml:1.0:function:string-equal",
                        Arguments =
                        [
                            new AttributeDesignator
                            {
                                Category = AttributeCategory.Subject,
                                AttributeId = "urn:bench:role",
                                DataType = "http://www.w3.org/2001/XMLSchema#string"
                            },
                            new AttributeValue
                            {
                                DataType = "http://www.w3.org/2001/XMLSchema#string",
                                Value = "admin"
                            }
                        ]
                    }
                }
            ]
        };
    }

    // ── Shared Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a simple XACML target with a single string-equal match on the subject category.
    /// </summary>
    private static Target CreateSimpleTarget(string attributeId) => new()
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
                                AttributeDesignator = new AttributeDesignator
                                {
                                    AttributeId = attributeId,
                                    Category = AttributeCategory.Subject,
                                    DataType = "http://www.w3.org/2001/XMLSchema#string"
                                },
                                AttributeValue = new AttributeValue
                                {
                                    DataType = "http://www.w3.org/2001/XMLSchema#string",
                                    Value = "benchmark-value"
                                }
                            }
                        ]
                    }
                ]
            }
        ]
    };

    /// <summary>
    /// Creates an Apply condition expression tree: and(string-equal(subject.role, "user"), integer-greater-than(resource.level, N)).
    /// </summary>
    private static Apply CreateConditionExpression(int policyIndex, int ruleIndex) => new()
    {
        FunctionId = "urn:oasis:names:tc:xacml:1.0:function:and",
        Arguments =
        [
            new Apply
            {
                FunctionId = "urn:oasis:names:tc:xacml:1.0:function:string-equal",
                Arguments =
                [
                    new AttributeDesignator
                    {
                        Category = AttributeCategory.Subject,
                        AttributeId = "urn:bench:role",
                        DataType = "http://www.w3.org/2001/XMLSchema#string"
                    },
                    new AttributeValue
                    {
                        DataType = "http://www.w3.org/2001/XMLSchema#string",
                        Value = $"user-{policyIndex}"
                    }
                ]
            },
            new Apply
            {
                FunctionId = "urn:oasis:names:tc:xacml:1.0:function:integer-greater-than",
                Arguments =
                [
                    new AttributeDesignator
                    {
                        Category = AttributeCategory.Resource,
                        AttributeId = "urn:bench:level",
                        DataType = "http://www.w3.org/2001/XMLSchema#integer"
                    },
                    new AttributeValue
                    {
                        DataType = "http://www.w3.org/2001/XMLSchema#integer",
                        Value = ruleIndex * 100
                    }
                ]
            }
        ]
    };
}
