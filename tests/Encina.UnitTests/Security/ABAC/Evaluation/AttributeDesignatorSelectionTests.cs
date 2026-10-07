using System.Dynamic;

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Enforcement;
using Encina.Security.ABAC.Evaluation;
using Encina.UnitTests.Security.ABAC.EEL;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Target = Encina.Security.ABAC.Target;

namespace Encina.UnitTests.Security.ABAC.Evaluation;

/// <summary>
/// Regression tests for #1983: XACML target matches and condition designators select attribute
/// values by (Category, AttributeId, DataType), so a value of one attribute never satisfies a
/// designator for another; <see cref="AttributeDesignator.MustBePresent"/> applies per attribute.
/// </summary>
public sealed class AttributeDesignatorSelectionTests
{
    // The built-in subject attribute identifiers of #1705 (ABACSubjectAttributes is not on main yet).
    private const string SubjectId = "subject-id";
    private const string IdentityKind = "identity-kind";

    private readonly DefaultFunctionRegistry _registry = new();

    // ── Target matches ──────────────────────────────────────────────

    [Fact]
    public void Target_OnIdentityKind_DoesNotMatchASubjectWhoseOtherAttributeHasTheValue()
    {
        // A user whose subject id is the string "service" is not a service identity.
        var context = Build(new Dictionary<string, object> { [SubjectId] = "service", [IdentityKind] = "user" });

        var effect = new TargetEvaluator(_registry).EvaluateTarget(
            SubjectTarget(IdentityKind, "service"), context);

        effect.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void Target_OnIdentityKind_MatchesTheAttributeItNames()
    {
        var context = Build(new Dictionary<string, object> { [SubjectId] = "billing", [IdentityKind] = "service" });

        var effect = new TargetEvaluator(_registry).EvaluateTarget(
            SubjectTarget(IdentityKind, "service"), context);

        effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void Target_OnDepartment_DoesNotMatchWhenOnlyAnotherAttributeEqualsTheValue()
    {
        var context = Build(new Dictionary<string, object> { ["cost-center"] = "Finance", ["department"] = "HR" });

        var effect = new TargetEvaluator(_registry).EvaluateTarget(
            SubjectTarget("department", "Finance"), context);

        effect.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void Target_OnAbsentAttribute_WithMustBePresent_IsIndeterminateEvenWhenOtherAttributesExist()
    {
        var context = Build(new Dictionary<string, object> { ["cost-center"] = "Finance" });

        var effect = new TargetEvaluator(_registry).EvaluateTarget(
            SubjectTarget("department", "Finance", mustBePresent: true), context);

        effect.ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public void Target_OnAbsentAttribute_WithoutMustBePresent_IsNotApplicable()
    {
        var context = Build(new Dictionary<string, object> { ["cost-center"] = "Finance" });

        var effect = new TargetEvaluator(_registry).EvaluateTarget(
            SubjectTarget("department", "Finance"), context);

        effect.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public void Target_OnAttributeOfAnotherDataType_WithMustBePresent_IsIndeterminate()
    {
        // "level" holds the integer 5; a string designator for it finds no value.
        var context = Build(new Dictionary<string, object> { ["level"] = 5 });

        var effect = new TargetEvaluator(_registry).EvaluateTarget(
            SubjectTarget("level", "5", mustBePresent: true), context);

        effect.ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public void Target_OnMultiValuedAttribute_MatchesWhenAnyValueOfThatAttributeMatches()
    {
        var context = Context(subject: new Dictionary<string, AttributeBag>
        {
            ["role"] = AttributeBag.Of(
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "viewer" },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" })
        });

        var effect = new TargetEvaluator(_registry).EvaluateTarget(SubjectTarget("role", "admin"), context);

        effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public void Target_ComparisonFunctionThrows_IsIndeterminate()
    {
        var function = Substitute.For<IXACMLFunction>();
        function.Evaluate(Arg.Any<IReadOnlyList<object?>>()).Returns(_ => throw new InvalidOperationException("boom"));
        var registry = Substitute.For<IFunctionRegistry>();
        registry.GetFunction(Arg.Any<string>()).Returns(function);
        var context = Build(new Dictionary<string, object> { ["department"] = "Finance" });

        var effect = new TargetEvaluator(registry).EvaluateTarget(SubjectTarget("department", "Finance"), context);

        effect.ShouldBe(Effect.Indeterminate);
    }

    // ── Condition designators ───────────────────────────────────────

    [Fact]
    public void Condition_Designator_ReceivesOnlyTheBagOfItsAttribute()
    {
        var context = Build(new Dictionary<string, object>
        {
            [SubjectId] = "alice",
            [IdentityKind] = "user",
            ["department"] = "Finance"
        });

        var result = new ConditionEvaluator(_registry).Evaluate(Designator("department"), context);

        Right(result).ShouldBe("Finance");
    }

    [Fact]
    public void Condition_StringEqual_OnOneSubjectAttribute_IsTrueWhenTheSubjectHasSeveralAttributes()
    {
        // Before #1983 the designator returned the whole two-value subject bag, so string-equal
        // failed and the condition was Indeterminate.
        var context = Build(new Dictionary<string, object> { [SubjectId] = "alice", ["department"] = "Finance" });
        var condition = new Apply
        {
            FunctionId = XACMLFunctionIds.StringEqual,
            Arguments = [Designator("department"), new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" }]
        };

        var result = new ConditionEvaluator(_registry).Evaluate(condition, context);

        Right(result).ShouldBe(true);
    }

    [Fact]
    public void Condition_Designator_AbsentAttributeWithMustBePresent_IsAttributeResolutionFailed()
    {
        var context = Build(new Dictionary<string, object> { [SubjectId] = "alice" });

        var result = new ConditionEvaluator(_registry).Evaluate(Designator("clearance", mustBePresent: true), context);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.AttributeResolutionFailedCode));
    }

    [Fact]
    public void Condition_Designator_AbsentAttributeWithoutMustBePresent_IsTheEmptyBag()
    {
        var context = Build(new Dictionary<string, object> { [SubjectId] = "alice" });

        var result = new ConditionEvaluator(_registry).Evaluate(Designator("clearance"), context);

        Right(result).ShouldBeSameAs(AttributeBag.Empty);
    }

    [Fact]
    public void Condition_Designator_KeepsOnlyTheValuesOfItsDataType()
    {
        var context = Context(subject: new Dictionary<string, AttributeBag>
        {
            ["level"] = AttributeBag.Of(
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 5 },
                new AttributeValue { DataType = XACMLDataTypes.String, Value = "five" },
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 7 })
        });

        var integers = new ConditionEvaluator(_registry).Evaluate(
            Designator("level", XACMLDataTypes.Integer), context);
        var strings = new ConditionEvaluator(_registry).Evaluate(
            Designator("level", XACMLDataTypes.String), context);

        var integerBag = Right(integers).ShouldBeOfType<AttributeBag>();
        integerBag.Values.Select(value => value.Value).ShouldBe([5, 7]);
        Right(strings).ShouldBe("five");
    }

    [Fact]
    public void Condition_Designator_OnlyValuesOfAnotherDataType_WithMustBePresent_IsAttributeResolutionFailed()
    {
        var context = Build(new Dictionary<string, object> { ["level"] = 5 });

        var result = new ConditionEvaluator(_registry).Evaluate(Designator("level", mustBePresent: true), context);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Condition_Designator_NullBagStoredForTheAttribute_IsAbsent()
    {
        var context = Context(subject: new Dictionary<string, AttributeBag> { ["department"] = null! });

        var result = new ConditionEvaluator(_registry).Evaluate(Designator("department", mustBePresent: true), context);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Condition_Designator_UndefinedCategory_IsAbsent()
    {
        var context = Build(new Dictionary<string, object> { ["department"] = "Finance" });
        var designator = new AttributeDesignator
        {
            Category = (AttributeCategory)99,
            AttributeId = "department",
            DataType = XACMLDataTypes.String,
            MustBePresent = true
        };

        var result = new ConditionEvaluator(_registry).Evaluate(designator, context);

        result.IsLeft.ShouldBeTrue();
    }

    [Theory]
    [InlineData(AttributeCategory.Resource, "classification", "confidential")]
    [InlineData(AttributeCategory.Environment, "region", "eu-west")]
    [InlineData(AttributeCategory.Action, "name", nameof(AttributeDesignatorSelectionTests))]
    public void Condition_Designator_SelectsByAttributeIdInEveryCategory(
        AttributeCategory category, string attributeId, string expected)
    {
        var context = AttributeContextBuilder.Build(
            new Dictionary<string, object> { [attributeId] = "subject-value" },
            new Dictionary<string, object> { ["classification"] = "confidential", ["owner"] = "confidential-owner" },
            new Dictionary<string, object> { ["region"] = "eu-west", ["tenantId"] = "eu-west-tenant" },
            typeof(AttributeDesignatorSelectionTests));
        var designator = new AttributeDesignator
        {
            Category = category,
            AttributeId = attributeId,
            DataType = XACMLDataTypes.String
        };

        var result = new ConditionEvaluator(_registry).Evaluate(designator, context);

        Right(result).ShouldBe(expected);
    }

    // ── The issue's steps 1-4 (#1983) ───────────────────────────────

    [Fact]
    public async Task Issue1983_ServiceOnlyPolicy_DoesNotApplyToAUserWhoseIdIsService()
    {
        // 1. A policy whose target matches Subject identity-kind string-equal "service".
        var pap = new InMemoryPolicyAdministrationPoint(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);
        (await pap.AddPolicyAsync(new Policy
        {
            Id = "service-only",
            Target = SubjectTarget(IdentityKind, "service"),
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [new Rule { Id = "permit-services", Effect = Effect.Permit, Obligations = [], Advice = [] }],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        }, null)).IsRight.ShouldBeTrue();

        var pdp = new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(_registry),
            new ConditionEvaluator(_registry),
            new CombiningAlgorithmFactory(),
            NullLogger<XACMLPolicyDecisionPoint>.Instance);

        // 2-3. The subject attributes the PEP collects for TestIdentity.User("service"): the
        // built-in subject-id and identity-kind attributes of #1705 decision N3.
        var context = Build(new Dictionary<string, object> { [SubjectId] = "service", [IdentityKind] = "user" });

        // 4. [RequirePolicy("service-only")] evaluates the named policy on its own.
        var decision = await pdp.EvaluatePolicyAsync("service-only", context);

        Right(decision).Effect.ShouldBe(Effect.NotApplicable, "the policy targets services, the caller is a user");
    }

    // ── A mistyped designator on a Deny rule (documented behaviour) ─

    [Theory]
    [InlineData(false, Effect.NotApplicable)]
    [InlineData(true, Effect.Indeterminate)]
    public async Task DenyPolicy_DesignatorOfAnotherDataType_IsNotApplicableUnlessMustBePresent(
        bool mustBePresent, Effect expected)
    {
        // "blocked" holds the boolean true, the Deny target designates it as a string: the
        // designator sees no value. Without MustBePresent the Deny silently stops applying.
        var pap = new InMemoryPolicyAdministrationPoint(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);
        (await pap.AddPolicyAsync(new Policy
        {
            Id = "deny-blocked",
            Target = SubjectTarget("blocked", "true", mustBePresent),
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [new Rule { Id = "deny", Effect = Effect.Deny, Obligations = [], Advice = [] }],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        }, null)).IsRight.ShouldBeTrue();
        var pdp = new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(_registry),
            new ConditionEvaluator(_registry),
            new CombiningAlgorithmFactory(),
            NullLogger<XACMLPolicyDecisionPoint>.Instance);
        var context = Build(new Dictionary<string, object> { ["blocked"] = true });

        var decision = await pdp.EvaluatePolicyAsync("deny-blocked", context);

        Right(decision).Effect.ShouldBe(expected);
    }

    // ── XACML and EEL read the same attributes (decision 4) ─────────

    [Fact]
    public void XacmlDesignatorsAndEelGlobals_ReadTheSameValueForEveryAttribute()
    {
        var subject = new Dictionary<string, object>
        {
            [SubjectId] = "alice",
            ["department"] = "Finance",
            ["level"] = 5,
            ["active"] = true
        };
        var resource = new Dictionary<string, object> { ["classification"] = "confidential" };
        var environment = new Dictionary<string, object> { ["region"] = "eu-west" };
        var requestType = typeof(AttributeDesignatorSelectionTests);
        var context = AttributeContextBuilder.Build(subject, resource, environment, requestType);
        var globals = ABACRequirementEvaluator.CreateGlobals(
            new ABACCollectedAttributes(subject, resource, environment, context), requestType);
        var evaluator = new ConditionEvaluator(_registry);

        var categories = new (AttributeCategory Category, IReadOnlyDictionary<string, object> Values, ExpandoObject Eel)[]
        {
            (AttributeCategory.Subject, subject, (ExpandoObject)globals.user),
            (AttributeCategory.Resource, resource, (ExpandoObject)globals.resource),
            (AttributeCategory.Environment, environment, (ExpandoObject)globals.environment),
            (AttributeCategory.Action, new Dictionary<string, object> { ["name"] = requestType.Name }, (ExpandoObject)globals.action)
        };

        foreach (var (category, values, eel) in categories)
        {
            var eelMembers = (IDictionary<string, object?>)eel;
            eelMembers.Keys.ShouldBe(values.Keys, ignoreOrder: true);

            foreach (var (attributeId, value) in values)
            {
                var designator = new AttributeDesignator
                {
                    Category = category,
                    AttributeId = attributeId,
                    DataType = StoredDataType(context, category, attributeId),
                    MustBePresent = true
                };

                Right(evaluator.Evaluate(designator, context)).ShouldBe(eelMembers[attributeId], $"{category}/{attributeId}");
                eelMembers[attributeId].ShouldBe(value);
            }
        }
    }

    private static string StoredDataType(PolicyEvaluationContext context, AttributeCategory category, string attributeId)
    {
        var attributes = category switch
        {
            AttributeCategory.Subject => context.SubjectAttributes,
            AttributeCategory.Resource => context.ResourceAttributes,
            AttributeCategory.Environment => context.EnvironmentAttributes,
            _ => context.ActionAttributes
        };

        return attributes[attributeId].SingleValue().DataType;
    }

    [Theory]
    [InlineData("Finance", "HR", true)]
    [InlineData("HR", "Finance", false)]
    public async Task XacmlConditionAndEelCondition_AgreeOnTheSameAttribute(
        string department, string costCenter, bool expected)
    {
        var subject = new Dictionary<string, object> { ["department"] = department, ["cost-center"] = costCenter };
        var requestType = typeof(AttributeDesignatorSelectionTests);
        var context = AttributeContextBuilder.Build(subject, new Dictionary<string, object>(), new Dictionary<string, object>(), requestType);
        var globals = ABACRequirementEvaluator.CreateGlobals(
            new ABACCollectedAttributes(subject, new Dictionary<string, object>(), new Dictionary<string, object>(), context), requestType);

        var xacml = new ConditionEvaluator(_registry).Evaluate(
            new Apply
            {
                FunctionId = XACMLFunctionIds.StringEqual,
                Arguments = [Designator("department"), new AttributeValue { DataType = XACMLDataTypes.String, Value = "Finance" }]
            },
            context);
        var compiled = await SharedEELCompiler.Instance.CompileAsync("user.department == \"Finance\"");
        var runner = compiled.Match(Right: r => r, Left: _ => throw new InvalidOperationException("EEL compile failed"));
        var eel = await runner(globals);

        Right(xacml).ShouldBe(expected);
        eel.ShouldBe(expected);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static PolicyEvaluationContext Build(IReadOnlyDictionary<string, object> subject) =>
        AttributeContextBuilder.Build(
            subject,
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            typeof(AttributeDesignatorSelectionTests));

    private static PolicyEvaluationContext Context(IReadOnlyDictionary<string, AttributeBag> subject) => new()
    {
        SubjectAttributes = subject,
        ResourceAttributes = new Dictionary<string, AttributeBag>(),
        EnvironmentAttributes = new Dictionary<string, AttributeBag>(),
        ActionAttributes = new Dictionary<string, AttributeBag>(),
        RequestType = typeof(AttributeDesignatorSelectionTests)
    };

    private static AttributeDesignator Designator(
        string attributeId,
        string dataType = XACMLDataTypes.String,
        bool mustBePresent = false) => new()
        {
            Category = AttributeCategory.Subject,
            AttributeId = attributeId,
            DataType = dataType,
            MustBePresent = mustBePresent
        };

    private static Target SubjectTarget(string attributeId, string value, bool mustBePresent = false) => new()
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
                                AttributeDesignator = Designator(attributeId, mustBePresent: mustBePresent),
                                AttributeValue = new AttributeValue { DataType = XACMLDataTypes.String, Value = value }
                            }
                        ]
                    }
                ]
            }
        ]
    };

    private static T Right<T>(Either<EncinaError, T> either)
    {
        either.IsRight.ShouldBeTrue("expected Right but got Left");
        return either.Match(Right: value => value, Left: _ => default!);
    }
}
