using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Evaluation;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Evaluation;

/// <summary>
/// Unit tests for <see cref="XACMLPolicyDecisionPoint"/>: the XACML 3.0 PDP that
/// orchestrates policy evaluation, combining algorithms, and obligation/advice collection.
/// </summary>
public sealed class XACMLPolicyDecisionPointTests
{
    private readonly DefaultFunctionRegistry _registry = new();
    private readonly CombiningAlgorithmFactory _algorithmFactory = new();
    private readonly ILogger<XACMLPolicyDecisionPoint> _logger =
        NullLogger<XACMLPolicyDecisionPoint>.Instance;

    private XACMLPolicyDecisionPoint CreatePdp(IPolicyAdministrationPoint pap)
    {
        var targetEvaluator = new TargetEvaluator(_registry);
        var conditionEvaluator = new ConditionEvaluator(_registry);
        return new XACMLPolicyDecisionPoint(pap, targetEvaluator, conditionEvaluator, _algorithmFactory, _logger);
    }

    private static PolicyEvaluationContext MakeContext(
        AttributeBag? subject = null,
        AttributeBag? resource = null,
        AttributeBag? environment = null,
        AttributeBag? action = null) =>
        new()
        {
            SubjectAttributes = subject ?? AttributeBag.Empty,
            ResourceAttributes = resource ?? AttributeBag.Empty,
            EnvironmentAttributes = environment ?? AttributeBag.Empty,
            ActionAttributes = action ?? AttributeBag.Empty,
            RequestType = typeof(object)
        };

    private static AttributeBag SingleBag(string dataType, object? value) =>
        AttributeBag.Of(new AttributeValue { DataType = dataType, Value = value });

    #region No Policies

    [Fact]
    public async Task EvaluateAsync_NoPoliciesOrPolicySets_ReturnsNotApplicable()
    {
        var pap = new InMemoryPap();
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.NotApplicable);
        decision.Obligations.ShouldBeEmpty();
        decision.Advice.ShouldBeEmpty();
    }

    #endregion

    #region Single Policy — Permit and Deny

    [Fact]
    public async Task EvaluateAsync_SinglePermitPolicy_ReturnsPermit()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null, // Matches all
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public async Task EvaluateAsync_SingleDenyPolicy_ReturnsDeny()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Deny,
                    Target = null,
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Deny);
    }

    #endregion

    #region Target Matching

    [Fact]
    public async Task EvaluateAsync_PolicyTargetDoesNotMatch_ReturnsNotApplicable()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
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
                                        FunctionId = XACMLFunctionIds.StringEqual,
                                        AttributeDesignator = new AttributeDesignator
                                        {
                                            Category = AttributeCategory.Subject,
                                            AttributeId = "role",
                                            DataType = XACMLDataTypes.String
                                        },
                                        AttributeValue = new AttributeValue
                                        {
                                            DataType = XACMLDataTypes.String,
                                            Value = "admin"
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
                    Target = null,
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "viewer"));

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region Condition Evaluation

    [Fact]
    public async Task EvaluateAsync_ConditionTrue_ReturnsRuleEffect()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Condition = new Apply
                    {
                        FunctionId = XACMLFunctionIds.IntegerGreaterThan,
                        Arguments =
                        [
                            new AttributeDesignator
                            {
                                Category = AttributeCategory.Resource,
                                AttributeId = "amount",
                                DataType = XACMLDataTypes.Integer
                            },
                            new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 1000 }
                        ]
                    },
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext(resource: SingleBag(XACMLDataTypes.Integer, 5000));

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public async Task EvaluateAsync_ConditionFalse_ReturnsNotApplicable()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Condition = new Apply
                    {
                        FunctionId = XACMLFunctionIds.IntegerGreaterThan,
                        Arguments =
                        [
                            new AttributeDesignator
                            {
                                Category = AttributeCategory.Resource,
                                AttributeId = "amount",
                                DataType = XACMLDataTypes.Integer
                            },
                            new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 1000 }
                        ]
                    },
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext(resource: SingleBag(XACMLDataTypes.Integer, 500));

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region Combining Algorithms

    [Fact]
    public async Task EvaluateAsync_DenyOverrides_DenyWins()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule { Id = "rule-permit", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] },
                new Rule { Id = "rule-deny", Effect = Effect.Deny, Target = null, Obligations = [], Advice = [] }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Deny, "Deny overrides Permit per XACML §C.1");
    }

    [Fact]
    public async Task EvaluateAsync_PermitOverrides_PermitWins()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Rules =
            [
                new Rule { Id = "rule-deny", Effect = Effect.Deny, Target = null, Obligations = [], Advice = [] },
                new Rule { Id = "rule-permit", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit, "Permit overrides Deny per XACML §C.2");
    }

    #endregion

    #region Disabled Policies

    [Fact]
    public async Task EvaluateAsync_DisabledPolicy_ReturnsNotApplicable()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            IsEnabled = false,
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule { Id = "rule-1", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region Policy Sets

    [Fact]
    public async Task EvaluateAsync_PolicySet_EvaluatesChildPolicies()
    {
        var pap = new InMemoryPap();
        pap.AddPolicySet(new PolicySet
        {
            Id = "ps-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies =
            [
                new Policy
                {
                    Id = "policy-1",
                    Target = null,
                    Algorithm = CombiningAlgorithmId.DenyOverrides,
                    Rules =
                    [
                        new Rule { Id = "rule-1", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] }
                    ],
                    Obligations = [],
                    Advice = [],
                    VariableDefinitions = []
                }
            ],
            PolicySets = [],
            Obligations = [],
            Advice = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public async Task EvaluateAsync_DisabledPolicySet_ReturnsNotApplicable()
    {
        var pap = new InMemoryPap();
        pap.AddPolicySet(new PolicySet
        {
            Id = "ps-1",
            IsEnabled = false,
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies =
            [
                new Policy
                {
                    Id = "policy-1",
                    Target = null,
                    Algorithm = CombiningAlgorithmId.DenyOverrides,
                    Rules =
                    [
                        new Rule { Id = "rule-1", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] }
                    ],
                    Obligations = [],
                    Advice = [],
                    VariableDefinitions = []
                }
            ],
            PolicySets = [],
            Obligations = [],
            Advice = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region Obligations Filtering

    [Fact]
    public async Task EvaluateAsync_PermitWithPermitObligations_IncludesObligations()
    {
        var obligation = new Obligation
        {
            Id = "log-access",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Obligations = [obligation],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
        decision.Obligations.ShouldHaveSingleItem()
            .Id.ShouldBe("log-access");
    }

    [Fact]
    public async Task EvaluateAsync_PermitWithDenyObligations_ExcludesObligations()
    {
        var obligation = new Obligation
        {
            Id = "audit-deny",
            FulfillOn = FulfillOn.Deny,
            AttributeAssignments = []
        };

        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Obligations = [obligation],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
        decision.Obligations.ShouldBeEmpty("Deny obligations excluded when effect is Permit");
    }

    [Fact]
    public async Task EvaluateAsync_PolicyLevelObligations_IncludedInDecision()
    {
        var policyObligation = new Obligation
        {
            Id = "policy-audit",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule { Id = "rule-1", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] }
            ],
            Obligations = [policyObligation],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
        decision.Obligations.ShouldHaveSingleItem()
            .Id.ShouldBe("policy-audit");
    }

    #endregion

    #region Advice Filtering

    [Fact]
    public async Task EvaluateAsync_PermitWithPermitAdvice_IncludesAdvice()
    {
        var advice = new AdviceExpression
        {
            Id = "show-disclaimer",
            AppliesTo = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Obligations = [],
                    Advice = [advice]
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
        decision.Advice.ShouldHaveSingleItem()
            .Id.ShouldBe("show-disclaimer");
    }

    [Fact]
    public async Task EvaluateAsync_IncludeAdviceFalse_ExcludesAdvice()
    {
        var advice = new AdviceExpression
        {
            Id = "show-disclaimer",
            AppliesTo = FulfillOn.Permit,
            AttributeAssignments = []
        };

        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Obligations = [],
                    Advice = [advice]
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = new PolicyEvaluationContext
        {
            SubjectAttributes = AttributeBag.Empty,
            ResourceAttributes = AttributeBag.Empty,
            EnvironmentAttributes = AttributeBag.Empty,
            ActionAttributes = AttributeBag.Empty,
            RequestType = typeof(object),
            IncludeAdvice = false
        };

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Advice.ShouldBeEmpty("IncludeAdvice is false");
    }

    #endregion

    #region Evaluation Duration

    [Fact]
    public async Task EvaluateAsync_SetsEvaluationDuration()
    {
        var pap = new InMemoryPap();
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.EvaluationDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    #endregion

    #region PAP Errors

    [Fact]
    public async Task EvaluateAsync_PapPolicySetsError_ReturnsIndeterminate()
    {
        var pap = new FailingPap(failPolicySets: true, failPolicies: false);
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.Status.ShouldNotBeNull();
        decision.Status!.StatusCode.ShouldBe("processing-error");
    }

    [Fact]
    public async Task EvaluateAsync_PapPoliciesError_ReturnsIndeterminateInsteadOfDecidingOnThePolicySets()
    {
        // PAP fails on GetPoliciesAsync but succeeds on GetPolicySetsAsync, whose only set
        // permits: a Deny that lives only among the standalone policies must not be lost (#1676).
        var pap = new FailingPap(failPolicySets: false, failPolicies: true);
        pap.PolicySets.Add(new PolicySet
        {
            Id = "ps-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies =
            [
                new Policy
                {
                    Id = "policy-1",
                    Target = null,
                    Algorithm = CombiningAlgorithmId.DenyOverrides,
                    Rules =
                    [
                        new Rule { Id = "rule-1", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] }
                    ],
                    Obligations = [],
                    Advice = [],
                    VariableDefinitions = []
                }
            ],
            PolicySets = [],
            Obligations = [],
            Advice = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext();

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.Obligations.ShouldBeEmpty();
        decision.Advice.ShouldBeEmpty();
        decision.Status.ShouldNotBeNull();
        decision.Status!.StatusCode.ShouldBe("processing-error");
    }

    [Theory]
    [InlineData(true, false, "policy sets")]
    [InlineData(false, true, "standalone policies")]
    public async Task EvaluateAsync_PapRetrievalError_LogsTheCodeOnlyAndKeepsTheErrorMessageOutOfTheDecision(
        bool failPolicySets, bool failPolicies, string source)
    {
        var logger = new FakeLogger<XACMLPolicyDecisionPoint>();
        var pap = new FailingPap(failPolicySets, failPolicies, code: "store.down", message: Sentinel);
        var pdp = new XACMLPolicyDecisionPoint(
            pap, new TargetEvaluator(_registry), new ConditionEvaluator(_registry), _algorithmFactory, logger);

        var decision = await pdp.EvaluateAsync(MakeContext());

        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.Status!.StatusMessage.ShouldNotBeNull();
        decision.Status.StatusMessage!.ShouldNotContain(Sentinel);
        var record = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 9092);
        record.Message.ShouldContain("store.down");
        record.Message.ShouldContain(source);
        logger.Collector.GetSnapshot().ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EvaluateAsync_PapThrows_IsIndeterminateAndNoExceptionMessageReachesTheDecisionOrTheLog()
    {
        var logger = new FakeLogger<XACMLPolicyDecisionPoint>();
        var pap = new FailingPap(failPolicySets: false, failPolicies: false, throws: new InvalidOperationException(Sentinel));
        var pdp = new XACMLPolicyDecisionPoint(
            pap, new TargetEvaluator(_registry), new ConditionEvaluator(_registry), _algorithmFactory, logger);

        var decision = await pdp.EvaluateAsync(MakeContext());

        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.Status!.StatusMessage!.ShouldNotContain(Sentinel);
        var record = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 9093);
        record.Exception.ShouldNotBeNull();
        record.Exception!.ToString().ShouldNotContain(Sentinel);
        record.Message.ShouldNotContain(Sentinel);
    }

    [Fact]
    public async Task EvaluateAsync_Cancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var pap = new FailingPap(failPolicySets: false, failPolicies: false, throws: new OperationCanceledException(cts.Token));
        var pdp = CreatePdp(pap);

        await Should.ThrowAsync<OperationCanceledException>(async () => await pdp.EvaluateAsync(MakeContext(), cts.Token));
    }

    [Fact]
    public async Task EvaluateAsync_StoreThrowsOperationCanceledWithoutCallerCancellation_IsIndeterminate()
    {
        // A store timeout surfaces as OperationCanceledException although the caller never cancelled.
        var pap = new FailingPap(failPolicySets: false, failPolicies: false, throws: new OperationCanceledException());
        var pdp = CreatePdp(pap);

        var decision = await pdp.EvaluateAsync(MakeContext(), CancellationToken.None);

        decision.Effect.ShouldBe(Effect.Indeterminate);
    }

    private const string Sentinel = "SENTINEL-1676-secret-store-detail";

    #endregion

    #region Variable Definitions

    [Fact]
    public async Task EvaluateAsync_WithVariableDefinitions_ResolvesVariables()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "rule-1",
                    Effect = Effect.Permit,
                    Target = null,
                    Condition = new Apply
                    {
                        FunctionId = XACMLFunctionIds.StringEqual,
                        Arguments =
                        [
                            new VariableReference { VariableId = "userDept" },
                            new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" }
                        ]
                    },
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions =
            [
                new VariableDefinition
                {
                    VariableId = "userDept",
                    Expression = new AttributeDesignator
                    {
                        Category = AttributeCategory.Subject,
                        AttributeId = "department",
                        DataType = XACMLDataTypes.String
                    }
                }
            ]
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext(subject: SingleBag(XACMLDataTypes.String, "Finance"));

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Permit);
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public async Task EvaluateAsync_NullContext_Throws()
    {
        var pap = new InMemoryPap();
        var pdp = CreatePdp(pap);

        var act = () => pdp.EvaluateAsync(null!).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region Indeterminate Decision Status

    [Fact]
    public async Task EvaluateAsync_IndeterminateResult_IncludesStatus()
    {
        var pap = new InMemoryPap();
        pap.AddPolicy(new Policy
        {
            Id = "policy-1",
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
                                        FunctionId = XACMLFunctionIds.StringEqual,
                                        AttributeDesignator = new AttributeDesignator
                                        {
                                            Category = AttributeCategory.Subject,
                                            AttributeId = "missing",
                                            DataType = XACMLDataTypes.String,
                                            MustBePresent = true
                                        },
                                        AttributeValue = new AttributeValue
                                        {
                                            DataType = XACMLDataTypes.String,
                                            Value = "admin"
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
                new Rule { Id = "rule-1", Effect = Effect.Permit, Target = null, Obligations = [], Advice = [] }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        });
        var pdp = CreatePdp(pap);
        var ctx = MakeContext(); // No subject attributes

        var decision = await pdp.EvaluateAsync(ctx);

        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.Status.ShouldNotBeNull();
    }

    #endregion

    #region Test Helpers — In-Memory PAP

    /// <summary>
    /// Simple in-memory PAP for testing purposes.
    /// </summary>
    private sealed class InMemoryPap : IPolicyAdministrationPoint
    {
        private readonly List<PolicySet> _policySets = [];
        private readonly List<Policy> _policies = [];

        public void AddPolicySet(PolicySet policySet) => _policySets.Add(policySet);
        public void AddPolicy(Policy policy) => _policies.Add(policy);

        public ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetPolicySetsAsync(
            CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(_policySets));

        public ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
            string policySetId, CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, Option<PolicySet>>.Right(
                _policySets.Find(ps => ps.Id == policySetId) is { } ps
                    ? Option<PolicySet>.Some(ps)
                    : Option<PolicySet>.None));

        public ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(
            PolicySet policySet, CancellationToken cancellationToken = default)
        {
            _policySets.Add(policySet);
            return new(Either<EncinaError, Unit>.Right(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> UpdatePolicySetAsync(
            PolicySet policySet, CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, Unit>.Right(Unit.Default));

        public ValueTask<Either<EncinaError, Unit>> RemovePolicySetAsync(
            string policySetId, CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, Unit>.Right(Unit.Default));

        public ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesAsync(
            string? policySetId, CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, IReadOnlyList<Policy>>.Right(_policies));

        public ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(
            string policyId, CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, Option<Policy>>.Right(
                _policies.Find(p => p.Id == policyId) is { } p
                    ? Option<Policy>.Some(p)
                    : Option<Policy>.None));

        public ValueTask<Either<EncinaError, Unit>> AddPolicyAsync(
            Policy policy, string? parentPolicySetId, CancellationToken cancellationToken = default)
        {
            _policies.Add(policy);
            return new(Either<EncinaError, Unit>.Right(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
            Policy policy, CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, Unit>.Right(Unit.Default));

        public ValueTask<Either<EncinaError, Unit>> RemovePolicyAsync(
            string policyId, CancellationToken cancellationToken = default) =>
            new(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    /// <summary>
    /// PAP that fails on certain operations for testing error handling.
    /// </summary>
    private sealed class FailingPap(
        bool failPolicySets,
        bool failPolicies,
        string code = "pap.error",
        string message = "PAP error",
        Exception? throws = null) : IPolicyAdministrationPoint
    {
        public List<PolicySet> PolicySets { get; } = [];

        public ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetPolicySetsAsync(
            CancellationToken cancellationToken = default)
        {
            if (throws is not null)
            {
                throw throws;
            }

            if (failPolicySets)
            {
                return new(Either<EncinaError, IReadOnlyList<PolicySet>>.Left(
                    EncinaErrors.Create(code, message)));
            }

            return new(Either<EncinaError, IReadOnlyList<PolicySet>>.Right(PolicySets));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesAsync(
            string? policySetId, CancellationToken cancellationToken = default)
        {
            if (failPolicies)
            {
                return new(Either<EncinaError, IReadOnlyList<Policy>>.Left(
                    EncinaErrors.Create(code, message)));
            }

            return new(Either<EncinaError, IReadOnlyList<Policy>>.Right(
                Array.Empty<Policy>()));
        }

        // Unused operations — minimal implementation
        public ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(string policySetId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(PolicySet policySet, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<Either<EncinaError, Unit>> UpdatePolicySetAsync(PolicySet policySet, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<Either<EncinaError, Unit>> RemovePolicySetAsync(string policySetId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<Either<EncinaError, Unit>> AddPolicyAsync(Policy policy, string? parentPolicySetId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(Policy policy, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public ValueTask<Either<EncinaError, Unit>> RemovePolicyAsync(string policyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    #endregion
}
