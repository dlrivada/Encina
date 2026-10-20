using Encina.Security.ABAC;
using Encina.Security.ABAC.EEL;
using Encina.Testing.Identity;

using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Property-based tests of the PEP on the request identity (#1705 Phase 4): whatever the mode that
/// enforces and whatever the policies would decide, an unauthenticated caller never reaches the
/// attribute provider and is denied as unauthenticated; an authenticated caller reaches the provider
/// exactly once, with the identity of its request context.
/// </summary>
public sealed class ABACIdentityProperties
{
    private static readonly EELCompiler Compiler = new();

    [RequirePolicy("policy-a")]
    private sealed record GuardedRequest : IRequest<string>;

    private static readonly Effect[] Effects = [Effect.Permit, Effect.Deny, Effect.NotApplicable, Effect.Indeterminate];

    [Property(MaxTest = 200)]
    public bool AnonymousCaller_NeverReachesTheAttributeProviderAndIsDenied(bool warnMode, byte effectCode)
    {
        var provider = new CountingAttributeProvider();
        var behavior = CreateBehavior(provider, warnMode, Effects[effectCode % Effects.Length]);

        var result = Send(behavior, TestRequestContext.For(TestIdentity.Anonymous));

        return provider.SubjectCalls == 0
            && provider.ResourceCalls == 0
            && provider.EnvironmentCalls == 0
            && result.Match(
                Right: _ => false,
                Left: error => error.GetCode().IfNone(string.Empty) == EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Property(MaxTest = 200)]
    public bool AuthenticatedCaller_ReachesTheProviderOnceWithItsOwnIdentity(bool service, bool warnMode, byte effectCode, ushort id)
    {
        var identity = service
            ? TestIdentity.Service($"job-{id}")
            : TestIdentity.User($"user-{id}");
        var provider = new CountingAttributeProvider();
        var behavior = CreateBehavior(provider, warnMode, Effects[effectCode % Effects.Length]);

        _ = Send(behavior, TestRequestContext.For(identity));

        return provider.SubjectCalls == 1 && ReferenceEquals(provider.LastIdentity, identity);
    }

    private static Either<EncinaError, string> Send(ABACPipelineBehavior<GuardedRequest, string> behavior, IRequestContext context) =>
        behavior.Handle(
                new GuardedRequest(),
                context,
                () => ValueTask.FromResult(Prelude.Right<EncinaError, string>("handled")),
                CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();

    private static ABACPipelineBehavior<GuardedRequest, string> CreateBehavior(
        IAttributeProvider provider, bool warnMode, Effect effect) =>
        new(
            new FixedPdp(effect),
            provider,
            new ObligationExecutor([], NullLogger<ObligationExecutor>.Instance),
            Compiler,
            Options.Create(new ABACOptions { EnforcementMode = warnMode ? ABACEnforcementMode.Warn : ABACEnforcementMode.Block }),
            Substitute.For<global::Encina.Security.ABAC.DecisionAudit.IABACDecisionRecorder>(),
            TimeProvider.System,
            NullLogger<ABACPipelineBehavior<GuardedRequest, string>>.Instance);

    private sealed class CountingAttributeProvider : IAttributeProvider
    {
        private static readonly IReadOnlyDictionary<string, object> Empty = new Dictionary<string, object>();

        public int SubjectCalls { get; private set; }

        public int ResourceCalls { get; private set; }

        public int EnvironmentCalls { get; private set; }

        public RequestIdentity? LastIdentity { get; private set; }

        public ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
            RequestIdentity identity, CancellationToken cancellationToken = default)
        {
            SubjectCalls++;
            LastIdentity = identity;
            return ValueTask.FromResult(Empty);
        }

        public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
            TResource resource, CancellationToken cancellationToken = default)
        {
            ResourceCalls++;
            return ValueTask.FromResult(Empty);
        }

        public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
            CancellationToken cancellationToken = default)
        {
            EnvironmentCalls++;
            return ValueTask.FromResult(Empty);
        }
    }

    private sealed class FixedPdp(Effect effect) : IPolicyDecisionPoint
    {
        public ValueTask<PolicyDecision> EvaluateAsync(
            PolicyEvaluationContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Decision());

        public ValueTask<Either<EncinaError, PolicyDecision>> EvaluatePolicyAsync(
            string policyId, PolicyEvaluationContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Prelude.Right<EncinaError, PolicyDecision>(Decision()));

        private PolicyDecision Decision() => new()
        {
            Effect = effect,
            PolicyId = "policy-a",
            Obligations = [],
            Advice = [],
            EvaluationDuration = TimeSpan.Zero
        };
    }
}
