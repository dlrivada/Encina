using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Evaluation;
using Encina.Security.ABAC.Providers;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// PEP-level regression for the built-in subject attributes (#1705 N3) on top of the designator fix
/// (#1983, PR #1994): with the default attribute provider, the real PDP and an in-memory PAP, a
/// target on Subject <c>identity-kind</c> selects the caller's kind only, never a user whose id or
/// another attribute happens to carry the same text.
/// </summary>
public sealed class ABACBuiltInSubjectAttributeTargetTests
{
    private static readonly EELCompiler Compiler = new();

    [RequirePolicy("services-only")]
    private sealed record ServicesOnlyRequest : IRequest<string>;

    [RequirePolicy("role-user")]
    private sealed record RoleUserRequest : IRequest<string>;

    [Fact]
    public async Task ServiceKindTarget_DoesNotApplyToAUserWhoseIdIsService()
    {
        var behavior = await BehaviorWithAsync<ServicesOnlyRequest>(
            TargetedPolicy("services-only", ABACSubjectAttributes.IdentityKind, "service"));

        var result = await SendAsync(behavior, new ServicesOnlyRequest(), TestIdentity.User("service"));

        result.IsLeft.ShouldBeTrue("the user's subject-id is \"service\", but its identity-kind is \"user\"");
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.AccessDeniedCode));
    }

    [Fact]
    public async Task ServiceKindTarget_AppliesToADeclaredServiceIdentity()
    {
        var behavior = await BehaviorWithAsync<ServicesOnlyRequest>(
            TargetedPolicy("services-only", ABACSubjectAttributes.IdentityKind, "service"));

        var result = await SendAsync(behavior, new ServicesOnlyRequest(), TestIdentity.Service("billing-job"));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RoleTarget_DoesNotMatchAUserBecauseTheBuiltInKindIsUser()
    {
        // The default provider returns no "role" attribute; the built-in identity-kind is "user",
        // which must not satisfy a target on Subject role == "user".
        var behavior = await BehaviorWithAsync<RoleUserRequest>(
            TargetedPolicy("role-user", "role", "user"));

        var result = await SendAsync(behavior, new RoleUserRequest(), TestIdentity.User("alice"));

        result.IsLeft.ShouldBeTrue("no role attribute exists, so the required policy is NotApplicable");
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.AccessDeniedCode));
    }

    private static Policy TargetedPolicy(string id, string attributeId, string value) => new()
    {
        Id = id,
        Target = new global::Encina.Security.ABAC.Target
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
                                        AttributeId = attributeId,
                                        DataType = XACMLDataTypes.String
                                    },
                                    AttributeValue = new AttributeValue { DataType = XACMLDataTypes.String, Value = value }
                                }
                            ]
                        }
                    ]
                }
            ]
        },
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [new Rule { Id = id + "-permit", Effect = Effect.Permit, Obligations = [], Advice = [] }],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static async Task<ABACPipelineBehavior<TRequest, string>> BehaviorWithAsync<TRequest>(Policy policy)
        where TRequest : IRequest<string>
    {
        var pap = new InMemoryPolicyAdministrationPoint(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);
        (await pap.AddPolicyAsync(policy, null)).IsRight.ShouldBeTrue();
        var registry = new DefaultFunctionRegistry();
        var pdp = new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(registry),
            new ConditionEvaluator(registry),
            new CombiningAlgorithmFactory(),
            NullLogger<XACMLPolicyDecisionPoint>.Instance);

        return new ABACPipelineBehavior<TRequest, string>(
            pdp,
            new DefaultAttributeProvider(),
            new ObligationExecutor([], NullLogger<ObligationExecutor>.Instance),
            Compiler,
            Options.Create(new ABACOptions { EnforcementMode = ABACEnforcementMode.Block }),
            NullLogger<ABACPipelineBehavior<TRequest, string>>.Instance);
    }

    private static async Task<Either<EncinaError, string>> SendAsync<TRequest>(
        ABACPipelineBehavior<TRequest, string> behavior, TRequest request, RequestIdentity caller)
        where TRequest : IRequest<string> =>
        await behavior.Handle(
            request,
            TestRequestContext.For(caller),
            () => ValueTask.FromResult(Prelude.Right<EncinaError, string>("handled")),
            CancellationToken.None);
}
