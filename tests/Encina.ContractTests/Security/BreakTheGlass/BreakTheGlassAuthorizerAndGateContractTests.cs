#pragma warning disable CA1859 // exercised through the contract interfaces on purpose

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.ContractTests.Security.BreakTheGlass;

/// <summary>Request type the gate may elevate: marked eligible, subject read from <see cref="PatientId"/>.</summary>
[BreakTheGlassEligible("clinical-record", SubjectIdProperty = nameof(PatientId))]
public sealed record EligibleContractRequest(string PatientId);

/// <summary>
/// Contract tests: <see cref="RoleBreakTheGlassAuthorizer"/> honours <see cref="IBreakTheGlassOpenAuthorizer"/>
/// and the internal gate honours <see cref="IAccessElevationGate"/> (a refusal is an <c>Either</c> result, never a throw).
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassAuthorizerAndGateContractTests
{
    private const string Scope = "clinical-record";

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private static BreakTheGlassOptions Options()
    {
        var options = new BreakTheGlassOptions();
        options.DeclareScope(Scope, scope => scope.OpenerRoles.Add("clinician"));
        options.ReviewerRoles.Add("reviewer");
        return options;
    }

    private static IBreakTheGlassOpenAuthorizer Authorizer() => new RoleBreakTheGlassAuthorizer(Microsoft.Extensions.Options.Options.Create(Options()));

    private static string CodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));

    private static BreakTheGlassElevation Elevation(string userId) => new()
    {
        ElevationId = Guid.NewGuid(),
        TenantId = "tenant-1",
        UserId = userId,
        SubjectId = "patient-1",
        Scope = Scope,
        Justification = "justification",
        OpenedAtUtc = Now,
        ExpiresAtUtc = Now.AddHours(1)
    };

    // ── IBreakTheGlassOpenAuthorizer ────────────────────────────────────

    [Fact]
    public async Task Authorizer_OpenAsUserWithAnOpenerRole_IsAllowed()
    {
        var result = await Authorizer().AuthorizeOpenAsync(TestIdentity.User("clin-1", roles: ["clinician"]), Scope);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Authorizer_OpenWithoutAnOpenerRole_RefusesAsNotEligible()
    {
        var result = await Authorizer().AuthorizeOpenAsync(TestIdentity.User("u-1", roles: ["other"]), Scope);

        CodeOf(result).ShouldBe(BreakTheGlassErrors.NotEligible().GetCode().IfNone(string.Empty));
    }

    [Fact]
    public async Task Authorizer_OpenForAnUndeclaredScope_RefusesAsNotEligible()
    {
        var result = await Authorizer().AuthorizeOpenAsync(TestIdentity.User("clin-1", roles: ["clinician"]), "unknown-scope");

        CodeOf(result).ShouldBe(BreakTheGlassErrors.NotEligible().GetCode().IfNone(string.Empty));
    }

    [Fact]
    public async Task Authorizer_OpenAsServiceIdentity_RefusesAsNotEligible()
    {
        var result = await Authorizer().AuthorizeOpenAsync(TestIdentity.Service("worker", roles: ["clinician"]), Scope);

        CodeOf(result).ShouldBe(BreakTheGlassErrors.NotEligible().GetCode().IfNone(string.Empty));
    }

    [Fact]
    public async Task Authorizer_ReviewByAReviewerOtherThanTheOpener_IsAllowed()
    {
        var result = await Authorizer().AuthorizeReviewAsync(TestIdentity.User("rev-1", roles: ["reviewer"]), Elevation("clin-1"));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Authorizer_ReviewWithoutAReviewerRole_RefusesAsReviewerNotEligible()
    {
        var result = await Authorizer().AuthorizeReviewAsync(TestIdentity.User("u-1", roles: ["other"]), Elevation("clin-1"));

        CodeOf(result).ShouldBe(BreakTheGlassErrors.ReviewerNotEligible().GetCode().IfNone(string.Empty));
    }

    [Fact]
    public async Task Authorizer_ReviewByTheOpener_RefusesAsSelfReview()
    {
        var result = await Authorizer().AuthorizeReviewAsync(TestIdentity.User("clin-1", roles: ["reviewer"]), Elevation("clin-1"));

        CodeOf(result).ShouldBe(BreakTheGlassErrors.SelfReviewCode);
    }

    // ── IAccessElevationGate ────────────────────────────────────────────

    private static IAccessElevationGate Gate(BreakTheGlassOptions options, IBreakTheGlassStore store) => new BreakTheGlassAccessGate(
        store,
        new InMemoryOperationAuditStore(),
        new ReadAuditContext(),
        new BreakTheGlassRequestGrants(),
        Microsoft.Extensions.Options.Options.Create(options),
        new FakeTimeProvider(Now),
        NullLogger<BreakTheGlassAccessGate>.Instance);

    private static IRequestContext Context(RequestIdentity identity)
    {
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(identity);
        context.TenantId.Returns("tenant-1");
        context.CorrelationId.Returns("corr-1");
        return context;
    }

    private static async Task<Option<AccessElevation>> ElevateAsync(IAccessElevationGate gate, EncinaError denial, RequestIdentity? identity = null)
    {
        var result = await gate.TryElevateAsync(
            new EligibleContractRequest("patient-1"),
            Context(identity ?? TestIdentity.User("clin-1", roles: ["clinician"])),
            denial,
            CancellationToken.None);
        return result.Match(Right: o => o, Left: e => throw new InvalidOperationException(e.GetCode().IfNone("?")));
    }

    [Fact]
    public async Task Gate_WithAnEffectiveElevation_GrantsItForTheDeniedRequest()
    {
        var store = new InMemoryBreakTheGlassStore();
        var elevation = Elevation("clin-1");
        (await store.AddAsync(elevation)).IsRight.ShouldBeTrue();

        var granted = await ElevateAsync(Gate(Options(), store), EncinaErrors.Forbidden());

        var held = granted.IfNoneUnsafe((AccessElevation?)null);
        held.ShouldNotBeNull();
        held.ElevationId.ShouldBe(elevation.ElevationId);
        held.Scope.ShouldBe(Scope);
        held.ExpiresAtUtc.ShouldBe(elevation.ExpiresAtUtc);
    }

    [Fact]
    public async Task Gate_WithoutAnElevation_KeepsTheDenial()
    {
        var granted = await ElevateAsync(Gate(Options(), new InMemoryBreakTheGlassStore()), EncinaErrors.Forbidden());

        granted.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_WhenTheFeatureIsDisabled_KeepsTheDenial()
    {
        var store = new InMemoryBreakTheGlassStore();
        (await store.AddAsync(Elevation("clin-1"))).IsRight.ShouldBeTrue();
        var options = Options();
        options.Enabled = false;

        var granted = await ElevateAsync(Gate(options, store), EncinaErrors.Forbidden());

        granted.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_ForADenialThatIsNotOverridable_KeepsTheDenial()
    {
        var store = new InMemoryBreakTheGlassStore();
        (await store.AddAsync(Elevation("clin-1"))).IsRight.ShouldBeTrue();

        var granted = await ElevateAsync(Gate(Options(), store), EncinaErrors.Create("encina.authorization.not_a_real_code", "denied"));

        granted.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Gate_ForAServiceIdentity_KeepsTheDenial()
    {
        var store = new InMemoryBreakTheGlassStore();
        (await store.AddAsync(Elevation("service:worker"))).IsRight.ShouldBeTrue();

        var granted = await ElevateAsync(Gate(Options(), store), EncinaErrors.Forbidden(), TestIdentity.Service("worker"));

        granted.IsNone.ShouldBeTrue();
    }
}
