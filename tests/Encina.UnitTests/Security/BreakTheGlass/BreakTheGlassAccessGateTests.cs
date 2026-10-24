#pragma warning disable CA2012 // NSubstitute stubbing of ValueTask members

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.BreakTheGlass;

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = nameof(PatientId))]
public sealed record EligibleGateRequest(string PatientId);

public sealed record UnmarkedGateRequest(string PatientId);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = nameof(Id))]
public sealed record GuidGateRequest(Guid Id);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = nameof(Number))]
public sealed record IntGateRequest(int Number);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = "Missing")]
public sealed record MissingPropertyGateRequest(string PatientId);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope)]
public sealed record NoPropertyNamedGateRequest(string PatientId);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = nameof(Complex))]
public sealed record UnconvertibleGateRequest(object Complex);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = nameof(Id))]
public sealed record NullableGuidGateRequest(Guid? Id);

public sealed record PatientId(string Value);

public readonly record struct GuidPatientId(Guid Value);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = nameof(Patient))]
public sealed record RecordIdGateRequest(PatientId Patient);

[BreakTheGlassEligible(BreakTheGlassTestKit.Scope, SubjectIdProperty = nameof(Patient))]
public sealed record StructIdGateRequest(GuidPatientId Patient);

/// <summary>Unit tests for <see cref="BreakTheGlassAccessGate"/> (internal; reached through InternalsVisibleTo).</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassAccessGateTests
{
    private const string Subject = "patient-secret-77";

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private sealed class Harness
    {
        public FakeTimeProvider Time { get; } = new(Now);

        public InMemoryBreakTheGlassStore InMemory { get; } = new();

        public IBreakTheGlassStore Store { get; set; }

        public IOperationAuditStore AuditStore { get; set; }

        public InMemoryOperationAuditStore Audit { get; } = new();

        public ReadAuditContext ReadAudit { get; } = new();

        public BreakTheGlassRequestGrants Grants { get; } = new();

        public BreakTheGlassOptions Options { get; } = BreakTheGlassTestKit.Options();

        public Harness()
        {
            Store = InMemory;
            AuditStore = Audit;
        }

        public BreakTheGlassAccessGate Gate() => new(Store, AuditStore, ReadAudit, Grants, BreakTheGlassTestKit.Wrap(Options), Time);

        public async Task<BreakTheGlassElevation> OpenAsync(string user = "clin-1", string subject = Subject, string? tenant = "tenant-1", TimeSpan? duration = null)
        {
            var elevation = BreakTheGlassTestKit.Elevation(Now, userId: user, subjectId: subject, tenantId: tenant, duration: duration);
            await InMemory.AddAsync(elevation);
            return elevation;
        }

        public ValueTask<Either<EncinaError, Option<AccessElevation>>> TryAsync<TRequest>(
            TRequest request,
            RequestIdentity? identity = null,
            EncinaError? denial = null,
            string? tenant = "tenant-1",
            string correlationId = "corr-1")
            where TRequest : notnull =>
            Gate().TryElevateAsync(
                request,
                BreakTheGlassTestKit.Context(identity ?? BreakTheGlassTestKit.Clinician(), tenant, correlationId),
                denial ?? EncinaErrors.Forbidden(),
                CancellationToken.None);
    }

    private static Option<AccessElevation> Granted(Either<EncinaError, Option<AccessElevation>> result) =>
        result.Match(Right: o => o, Left: e => throw new InvalidOperationException("unexpected Left " + e.GetCode().IfNone("?")));

    private static string CodeOfLeft(Either<EncinaError, Option<AccessElevation>> result) =>
        result.Match(Right: _ => "ok", Left: e => e.GetCode().IfNone(string.Empty));

    private static AccessElevation Some1(Either<EncinaError, Option<AccessElevation>> result) =>
        Granted(result).IfNoneUnsafe((AccessElevation?)null)!;

    private static Either<EncinaError, Unit> Failure() => Left<EncinaError, Unit>(EncinaError.New("boom"));

    // ── Steps that keep the denial ──────────────────────────────────────

    [Theory]
    [InlineData("encina.authorization.unauthenticated")]
    [InlineData("abac.indeterminate")]
    [InlineData("abac.evaluation_failed")]
    [InlineData("abac.missing_context")]
    [InlineData("encina.authorization.abac_obligation_failed")]
    [InlineData("encina.authorization.abac_policy_not_found")]
    [InlineData("encina.authorization.not_a_real_code")]
    public async Task NonOverridableDenial_KeepsDenial_WithoutAnyStoreCall(string code)
    {
        var h = new Harness();
        var store = Substitute.For<IBreakTheGlassStore>();
        h.Store = store;

        var result = await h.TryAsync(new EligibleGateRequest(Subject), denial: EncinaErrors.Create(code, "denied"));

        Granted(result).IsNone.ShouldBeTrue();
        store.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task ServiceIdentity_KeepsDenial()
    {
        var h = new Harness();
        await h.OpenAsync(user: "service:worker");

        Granted(await h.TryAsync(new EligibleGateRequest(Subject), identity: TestIdentity.Service("worker"))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task AnonymousIdentity_KeepsDenial()
    {
        var h = new Harness();

        Granted(await h.TryAsync(new EligibleGateRequest(Subject), identity: TestIdentity.Anonymous)).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task UnmarkedRequestType_KeepsDenial()
    {
        var h = new Harness();
        await h.OpenAsync();

        Granted(await h.TryAsync(new UnmarkedGateRequest(Subject))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task FeatureDisabled_KeepsDenial()
    {
        var h = new Harness();
        await h.OpenAsync();
        h.Options.Enabled = false;

        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsNone.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankSubject_KeepsDenial(string subject)
    {
        var h = new Harness();
        await h.OpenAsync(subject: subject);

        Granted(await h.TryAsync(new EligibleGateRequest(subject))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task NoElevation_KeepsDenial()
    {
        var h = new Harness();

        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsNone.ShouldBeTrue();
        h.Audit.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ElevationOfAnotherSubjectUserOrTenant_KeepsDenial()
    {
        var h = new Harness();
        await h.OpenAsync();

        Granted(await h.TryAsync(new EligibleGateRequest("another-patient"))).IsNone.ShouldBeTrue();
        Granted(await h.TryAsync(new EligibleGateRequest(Subject), identity: BreakTheGlassTestKit.Clinician("clin-2"))).IsNone.ShouldBeTrue();
        Granted(await h.TryAsync(new EligibleGateRequest(Subject), tenant: "tenant-2")).IsNone.ShouldBeTrue();
        Granted(await h.TryAsync(new EligibleGateRequest(Subject), tenant: null)).IsNone.ShouldBeTrue();
        h.Audit.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExpiredElevation_KeepsDenial()
    {
        var h = new Harness();
        await h.OpenAsync(duration: TimeSpan.FromMinutes(10));
        h.Time.Advance(TimeSpan.FromMinutes(10));

        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsNone.ShouldBeTrue();
        h.Audit.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ElevationJustBeforeExpiry_IsGranted()
    {
        var h = new Harness();
        await h.OpenAsync(duration: TimeSpan.FromMinutes(10));
        h.Time.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1));

        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task StaleElevationReturnedByStore_IsRejectedByTheGate()
    {
        var h = new Harness();
        var stale = BreakTheGlassTestKit.Elevation(Now.AddHours(-2), userId: "clin-1", subjectId: Subject);
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(Some(stale))));
        h.Store = store;

        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsNone.ShouldBeTrue();
        h.Audit.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ClosedElevationReturnedByStore_IsRejectedByTheGate()
    {
        var h = new Harness();
        var revoked = BreakTheGlassTestKit.Elevation(Now, userId: "clin-1", subjectId: Subject) with { Status = BreakTheGlassStatus.Revoked };
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(Some(revoked))));
        h.Store = store;

        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsNone.ShouldBeTrue();
    }

    // ── Failures replace the denial ─────────────────────────────────────

    [Fact]
    public async Task StoreFailure_ReplacesDenialWithStoreFailed()
    {
        var h = new Harness();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Left<EncinaError, Option<BreakTheGlassElevation>>(EncinaError.New("boom"))));
        h.Store = store;

        CodeOfLeft(await h.TryAsync(new EligibleGateRequest(Subject))).ShouldBe(BreakTheGlassErrors.StoreFailedCode);
    }

    [Fact]
    public async Task AuditFailure_ReturnsAuditFailed_AndNothingIsGrantedOrSet()
    {
        var h = new Harness();
        await h.OpenAsync();
        var audit = Substitute.For<IOperationAuditStore>();
        audit.RecordAsync(default!, default).ReturnsForAnyArgs(ValueTask.FromResult(Failure()));
        h.AuditStore = audit;

        CodeOfLeft(await h.TryAsync(new EligibleGateRequest(Subject))).ShouldBe(BreakTheGlassErrors.AuditFailedCode);

        h.ReadAudit.Purpose.ShouldBeNull();
        h.ReadAudit.Metadata.ShouldBeEmpty();
        h.Grants.TryGet("corr-1", typeof(EligibleGateRequest), Subject, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task AuditStoreThrows_ReturnsAuditFailed()
    {
        var h = new Harness();
        await h.OpenAsync();
        var audit = Substitute.For<IOperationAuditStore>();
        audit.RecordAsync(default!, default).ReturnsForAnyArgs<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("db down"));
        h.AuditStore = audit;

        CodeOfLeft(await h.TryAsync(new EligibleGateRequest(Subject))).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
    }

    // ── Grant ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Grant_ReturnsElevation_WritesAccessAudit_AndSetsReadAudit()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();

        var granted = Some1(await h.TryAsync(new EligibleGateRequest(Subject)));

        granted.ElevationId.ShouldBe(elevation.ElevationId);
        granted.Scope.ShouldBe(BreakTheGlassTestKit.Scope);
        granted.ExpiresAtUtc.ShouldBe(elevation.ExpiresAtUtc);

        var entry = h.Audit.GetAllEntries().Single();
        entry.Action.ShouldBe("break_glass.access");
        entry.EntityType.ShouldBe("BreakTheGlassElevation");
        entry.EntityId.ShouldBe(elevation.ElevationId.ToString("D"));
        entry.Outcome.ShouldBe(AuditOutcome.Success);
        entry.CorrelationId.ShouldBe("corr-1");
        entry.UserId.ShouldBe("clin-1");
        entry.TenantId.ShouldBe("tenant-1");
        entry.Metadata["request_type"].ShouldBe(nameof(EligibleGateRequest));
        entry.Metadata["denial_code"].ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        entry.Metadata["gate"].ShouldBe("unknown");

        h.ReadAudit.Purpose.ShouldBe("break-the-glass");
        h.ReadAudit.Metadata["elevation_id"].ShouldBe(elevation.ElevationId.ToString("D"));
        h.ReadAudit.Metadata.ContainsKey("declared_purpose").ShouldBeFalse();
    }

    [Fact]
    public async Task Grant_AuditEntry_ContainsNoSubjectJustificationOrUserInMetadata()
    {
        var h = new Harness();
        await h.OpenAsync();

        await h.TryAsync(new EligibleGateRequest(Subject));

        var entry = h.Audit.GetAllEntries().Single();
        var rendered = string.Join('|', entry.Metadata.Select(kv => $"{kv.Key}={kv.Value}")) + entry.EntityId + entry.ErrorMessage;
        rendered.ShouldNotContain(Subject);
        rendered.ShouldNotContain(BreakTheGlassTestKit.Justification);
        rendered.ShouldNotContain("clin-1");
    }

    [Fact]
    public async Task Grant_DenyingStageFromDetails_IsRecordedAsGate()
    {
        var h = new Harness();
        await h.OpenAsync();
        var denial = EncinaErrors.Create("encina.authorization.abac_access_denied", "denied", details: new Dictionary<string, object?> { ["stage"] = "abac" });

        await h.TryAsync(new EligibleGateRequest(Subject), denial: denial);

        h.Audit.GetAllEntries().Single().Metadata["gate"].ShouldBe("abac");
    }

    [Fact]
    public async Task Grant_KeepsAPurposeTheApplicationDeclared()
    {
        var h = new Harness();
        await h.OpenAsync();
        h.ReadAudit.WithPurpose("Patient care review");

        await h.TryAsync(new EligibleGateRequest(Subject));

        h.ReadAudit.Purpose.ShouldBe("break-the-glass");
        h.ReadAudit.Metadata["declared_purpose"].ShouldBe("Patient care review");
    }

    [Fact]
    public async Task SecondCallInSameRequest_ReusesGrant_AndWritesNoSecondAccessEntry()
    {
        var h = new Harness();
        await h.OpenAsync();
        var first = Some1(await h.TryAsync(new EligibleGateRequest(Subject)));
        var store = Substitute.For<IBreakTheGlassStore>();
        h.Store = store;

        var second = Some1(await h.TryAsync(new EligibleGateRequest(Subject)));

        second.ShouldBe(first);
        h.Audit.Count.ShouldBe(1);
        store.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task HeldGrant_PastItsExpiry_IsNotReused()
    {
        var h = new Harness();
        await h.OpenAsync(duration: TimeSpan.FromMinutes(10));
        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsSome.ShouldBeTrue();

        h.Time.Advance(TimeSpan.FromMinutes(10));

        Granted(await h.TryAsync(new EligibleGateRequest(Subject))).IsNone.ShouldBeTrue();
        h.Audit.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SecondCallForAnotherSubjectOrRequestType_DoesNotReuseTheGrant()
    {
        var h = new Harness();
        await h.OpenAsync();
        await h.TryAsync(new EligibleGateRequest(Subject));

        Granted(await h.TryAsync(new EligibleGateRequest("other-patient"))).IsNone.ShouldBeTrue();
        Granted(await h.TryAsync(new UnmarkedGateRequest(Subject))).IsNone.ShouldBeTrue();
        h.Audit.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DifferentCorrelationId_AuditsAgain()
    {
        var h = new Harness();
        await h.OpenAsync();

        await h.TryAsync(new EligibleGateRequest(Subject), correlationId: "corr-1");
        await h.TryAsync(new EligibleGateRequest(Subject), correlationId: "corr-2");

        h.Audit.Count.ShouldBe(2);
    }

    // ── Subject conversion ──────────────────────────────────────────────

    [Fact]
    public async Task GuidSubject_IsConvertedWithDFormat()
    {
        var h = new Harness();
        var id = Guid.NewGuid();
        await h.OpenAsync(subject: id.ToString("D"));

        Granted(await h.TryAsync(new GuidGateRequest(id))).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task NullableGuidSubject_NullKeepsDenial_ValueGrants()
    {
        var h = new Harness();
        var id = Guid.NewGuid();
        await h.OpenAsync(subject: id.ToString("D"));

        Granted(await h.TryAsync(new NullableGuidGateRequest(null))).IsNone.ShouldBeTrue();
        Granted(await h.TryAsync(new NullableGuidGateRequest(id))).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task IntSubject_IsConvertedInvariantly()
    {
        var h = new Harness();
        await h.OpenAsync(subject: "1234567");

        Granted(await h.TryAsync(new IntGateRequest(1234567))).IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task RecordStronglyTypedId_IsConvertedByItsValue_NotByTheGeneratedToString()
    {
        var h = new Harness();
        await h.OpenAsync(subject: "p-1");

        Granted(await h.TryAsync(new RecordIdGateRequest(new PatientId("p-1")))).IsSome.ShouldBeTrue();
        Granted(await h.TryAsync(new RecordIdGateRequest(new PatientId("p-2")))).IsNone.ShouldBeTrue();
        Granted(await h.TryAsync(new RecordIdGateRequest(new PatientId(" ")))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task RecordStructStronglyTypedId_IsConvertedByItsValue()
    {
        var h = new Harness();
        var id = Guid.NewGuid();
        await h.OpenAsync(subject: id.ToString("D"));

        Granted(await h.TryAsync(new StructIdGateRequest(new GuidPatientId(id)))).IsSome.ShouldBeTrue();
        Granted(await h.TryAsync(new StructIdGateRequest(default))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task IdentityThatChangesBetweenReads_GivesOneConsistentDecisionAndNoException()
    {
        var h = new Harness();
        await h.OpenAsync();
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(BreakTheGlassTestKit.Clinician(), RequestIdentity.Anonymous);
        context.TenantId.Returns("tenant-1");
        context.CorrelationId.Returns("corr-1");

        var result = await h.Gate().TryElevateAsync(new EligibleGateRequest(Subject), context, EncinaErrors.Forbidden(), CancellationToken.None);

        Granted(result).IsSome.ShouldBeTrue();
        _ = context.Received(1).Identity;
        h.Audit.Count.ShouldBe(1);
    }

    [Fact]
    public async Task IdentityThatIsAnonymousOnTheOnlyRead_KeepsDenial()
    {
        var h = new Harness();
        await h.OpenAsync();
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(RequestIdentity.Anonymous, BreakTheGlassTestKit.Clinician());
        context.TenantId.Returns("tenant-1");
        context.CorrelationId.Returns("corr-1");

        var result = await h.Gate().TryElevateAsync(new EligibleGateRequest(Subject), context, EncinaErrors.Forbidden(), CancellationToken.None);

        Granted(result).IsNone.ShouldBeTrue();
        h.Audit.Count.ShouldBe(0);
    }

    [Fact]
    public async Task NullStringSubject_KeepsDenial()
    {
        var h = new Harness();

        Granted(await h.TryAsync(new EligibleGateRequest(null!))).IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task MisconfiguredRequestTypes_ThrowInsteadOfSilentlyMatching()
    {
        var h = new Harness();

        await Should.ThrowAsync<TypeInitializationException>(async () => await h.TryAsync(new MissingPropertyGateRequest(Subject)));
        await Should.ThrowAsync<TypeInitializationException>(async () => await h.TryAsync(new NoPropertyNamedGateRequest(Subject)));
        await Should.ThrowAsync<TypeInitializationException>(async () => await h.TryAsync(new UnconvertibleGateRequest(new object())));
    }

    [Fact]
    public async Task NullArguments_Throw()
    {
        var h = new Harness();
        var context = BreakTheGlassTestKit.Context(BreakTheGlassTestKit.Clinician());

        await Should.ThrowAsync<ArgumentNullException>(async () => await h.Gate().TryElevateAsync<EligibleGateRequest>(null!, context, EncinaErrors.Forbidden(), CancellationToken.None));
        await Should.ThrowAsync<ArgumentNullException>(async () => await h.Gate().TryElevateAsync(new EligibleGateRequest(Subject), null!, EncinaErrors.Forbidden(), CancellationToken.None));
    }
}
