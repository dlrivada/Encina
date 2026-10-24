#pragma warning disable CA2012 // NSubstitute stubbing of ValueTask members

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>Unit tests for <see cref="DefaultBreakTheGlassService"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class DefaultBreakTheGlassServiceTests
{
    private const string SecretSubject = "patient-secret-77";

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private sealed class Harness
    {
        public FakeTimeProvider Time { get; } = new(Now);

        public InMemoryBreakTheGlassStore InMemory { get; } = new();

        public IBreakTheGlassStore Store { get; set; }

        public IOperationAuditStore AuditStore { get; set; }

        public InMemoryOperationAuditStore Audit { get; } = new();

        public IEncina Encina { get; } = Substitute.For<IEncina>();

        public BreakTheGlassOptions Options { get; } = BreakTheGlassTestKit.Options();

        public Harness()
        {
            Store = InMemory;
            AuditStore = Audit;
            Encina.Publish(Arg.Any<BreakTheGlassOpenedNotification>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
            Encina.Publish(Arg.Any<BreakTheGlassReviewedNotification>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        }

        public DefaultBreakTheGlassService Sut(RequestIdentity? identity, string? tenant = "tenant-1")
        {
            var context = identity is null ? null : BreakTheGlassTestKit.Context(identity, tenant);
            return new DefaultBreakTheGlassService(
                Store,
                AuditStore,
                BreakTheGlassTestKit.Accessor(context),
                new RoleBreakTheGlassAuthorizer(BreakTheGlassTestKit.Wrap(Options)),
                new BreakTheGlassRequestValidator(BreakTheGlassTestKit.Wrap(Options)),
                Encina,
                BreakTheGlassTestKit.Wrap(Options),
                Time);
        }

        public async Task<BreakTheGlassElevation> OpenAsync(string user = "clin-1", string tenant = "tenant-1")
        {
            var result = await Sut(BreakTheGlassTestKit.Clinician(user), tenant)
                .OpenAsync(new BreakTheGlassRequest(SecretSubject, BreakTheGlassTestKit.Scope, BreakTheGlassTestKit.Justification, TimeSpan.FromHours(1)), CancellationToken.None);
            return Ok(result);
        }
    }

    private static T Ok<T>(Either<EncinaError, T> result) =>
        result.Match(Right: r => r, Left: e => throw new InvalidOperationException("unexpected Left " + e.GetCode().IfNone("?")));

    private static string Code<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => "ok", Left: e => e.GetCode().IfNone(string.Empty));

    private static EncinaError ErrorOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => throw new InvalidOperationException("expected Left"), Left: e => e);

    private static BreakTheGlassRequest Request(string justification = BreakTheGlassTestKit.Justification, TimeSpan? duration = null) =>
        new(SecretSubject, BreakTheGlassTestKit.Scope, justification, duration ?? TimeSpan.FromHours(1));

    private static Either<EncinaError, Unit> Failure() => Left<EncinaError, Unit>(EncinaError.New("boom"));

    // ── OpenAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task Open_ConcurrentSameKey_OneSucceedsAndTheOtherIsAlreadyActive()
    {
        var h = new Harness();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            Task.Run(async () => await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None))));

        results.Count(r => r.IsRight).ShouldBe(1);
        results.Where(r => r.IsLeft).Select(Code).ShouldAllBe(code => code == BreakTheGlassErrors.AlreadyActiveCode);
        var queue = Ok(await h.Sut(BreakTheGlassTestKit.Reviewer("rev-1")).GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None));
        queue.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Open_StoreReportsAlreadyActive_IsAuthoritative_AndAuditsOpenFailed()
    {
        var h = new Harness();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(None)));
        store.AddAsync(default!, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Left<EncinaError, Unit>(BreakTheGlassErrors.AlreadyActive())));
        h.Store = store;

        var result = await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None);

        Code(result).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
        h.Audit.Count.ShouldBe(2);
        await h.Encina.DidNotReceive().Publish(Arg.Any<BreakTheGlassOpenedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Open_NoRequestContext_IsUnauthenticated()
    {
        var h = new Harness();

        Code(await h.Sut(null).OpenAsync(Request(), CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task Open_AnonymousIdentity_IsUnauthenticated()
    {
        var h = new Harness();

        Code(await h.Sut(TestIdentity.Anonymous).OpenAsync(Request(), CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task Open_AuthorizerRefusal_IsForbiddenNotEligible_AndWritesNothing()
    {
        var h = new Harness();

        var result = await h.Sut(TestIdentity.User("rec-1", roles: ["receptionist"])).OpenAsync(Request(), CancellationToken.None);

        Code(result).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        ErrorOf(result).GetDetails()["reason"].ShouldBe("break_glass_not_eligible");
        h.Audit.Count.ShouldBe(0);
        Ok(await h.InMemory.GetExpiredUnrecordedAsync(Now.AddYears(1), 10)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Open_ValidationFailure_ReturnsValidationFailed_AndWritesNothing()
    {
        var h = new Harness();

        var result = await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(justification: "too short"), CancellationToken.None);

        Code(result).ShouldBe(BreakTheGlassErrors.ValidationFailedCode);
        h.Audit.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Open_TenantRequiredAndMissing_ReturnsTenantRequired()
    {
        var h = new Harness();
        h.Options.RequireTenant = true;

        Code(await h.Sut(BreakTheGlassTestKit.Clinician(), tenant: null).OpenAsync(Request(), CancellationToken.None)).ShouldBe(BreakTheGlassErrors.TenantRequiredCode);
    }

    [Fact]
    public async Task Open_TenantRequiredAndPresent_Succeeds()
    {
        var h = new Harness();
        h.Options.RequireTenant = true;

        (await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None)).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Open_SecondOpenForSameKey_IsAlreadyActive()
    {
        var h = new Harness();
        await h.OpenAsync();

        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None)).ShouldBe(BreakTheGlassErrors.AlreadyActiveCode);
        h.Audit.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Open_SameKeyAfterExpiry_IsAllowed()
    {
        var h = new Harness();
        await h.OpenAsync();
        h.Time.Advance(TimeSpan.FromHours(2));

        (await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None)).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Open_FindEffectiveFails_ReturnsStoreFailed()
    {
        var h = new Harness();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Left<EncinaError, Option<BreakTheGlassElevation>>(EncinaError.New("boom"))));
        h.Store = store;

        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None)).ShouldBe(BreakTheGlassErrors.StoreFailedCode);
        await store.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Open_AuditFails_NoStoreWrite_AndAuditFailedReturned()
    {
        var h = new Harness();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(None)));
        h.Store = store;
        var audit = Substitute.For<IOperationAuditStore>();
        audit.RecordAsync(default!, default).ReturnsForAnyArgs(ValueTask.FromResult(Failure()));
        h.AuditStore = audit;

        var result = await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None);

        Code(result).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
        await store.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await h.Encina.DidNotReceiveWithAnyArgs().Publish<BreakTheGlassOpenedNotification>(default!, default);
    }

    [Fact]
    public async Task Open_AuditStoreThrows_FailsClosedWithAuditFailed()
    {
        var h = new Harness();
        var audit = Substitute.For<IOperationAuditStore>();
        audit.RecordAsync(default!, default).ReturnsForAnyArgs<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("db down"));
        h.AuditStore = audit;

        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None)).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
        Ok(await h.InMemory.GetExpiredUnrecordedAsync(Now.AddYears(1), 10)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Open_StoreAddFails_WritesOpenFailedAudit_AndReturnsStoreFailed()
    {
        var h = new Harness();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.FindEffectiveAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(None)));
        store.AddAsync(default!, default).ReturnsForAnyArgs(ValueTask.FromResult(Failure()));
        h.Store = store;

        var result = await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None);

        Code(result).ShouldBe(BreakTheGlassErrors.StoreFailedCode);
        h.Audit.GetAllEntries().Select(e => e.Action).ShouldBe(["break_glass.opened", "break_glass.open_failed"], ignoreOrder: true);
        h.Audit.GetAllEntries().Single(e => e.Action == "break_glass.open_failed").Outcome.ShouldBe(AuditOutcome.Error);
        await h.Encina.DidNotReceiveWithAnyArgs().Publish<BreakTheGlassOpenedNotification>(default!, default);
    }

    [Fact]
    public async Task Open_Success_PersistsAuditsAndPublishes()
    {
        var h = new Harness();

        var elevation = await h.OpenAsync();

        elevation.UserId.ShouldBe("clin-1");
        elevation.TenantId.ShouldBe("tenant-1");
        elevation.OpenedAtUtc.ShouldBe(Now);
        elevation.ExpiresAtUtc.ShouldBe(Now.AddHours(1));
        elevation.Status.ShouldBe(BreakTheGlassStatus.Active);
        Ok(await h.InMemory.GetByIdAsync("tenant-1", elevation.ElevationId)).IsSome.ShouldBeTrue();

        var entry = h.Audit.GetAllEntries().Single();
        entry.Action.ShouldBe("break_glass.opened");
        entry.EntityType.ShouldBe("BreakTheGlassElevation");
        entry.EntityId.ShouldBe(elevation.ElevationId.ToString("D"));
        entry.Outcome.ShouldBe(AuditOutcome.Success);
        entry.UserId.ShouldBe("clin-1");
        entry.TenantId.ShouldBe("tenant-1");

        await h.Encina.Received(1).Publish(
            Arg.Is<BreakTheGlassOpenedNotification>(n => n.ElevationId == elevation.ElevationId && n.Scope == BreakTheGlassTestKit.Scope && n.TenantId == "tenant-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Open_AuditEntryAndNotification_ContainNoSubjectJustificationOrUserInMetadata()
    {
        var h = new Harness();
        BreakTheGlassOpenedNotification? published = null;
        h.Encina.Publish(Arg.Do<BreakTheGlassOpenedNotification>(n => published = n), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        await h.OpenAsync();

        var entry = h.Audit.GetAllEntries().Single();
        var rendered = string.Join('|', entry.Metadata.Select(kv => $"{kv.Key}={kv.Value}")) + entry.EntityId + entry.ErrorMessage + entry.Action;
        rendered.ShouldNotContain(SecretSubject);
        rendered.ShouldNotContain(BreakTheGlassTestKit.Justification);
        rendered.ShouldNotContain("clin-1");
        published.ShouldNotBeNull();
        published.ToString().ShouldNotContain(SecretSubject);
        published.ToString().ShouldNotContain(BreakTheGlassTestKit.Justification);
    }

    [Fact]
    public async Task Open_NotificationsDisabled_DoesNotPublish()
    {
        var h = new Harness();
        h.Options.PublishNotifications = false;

        await h.OpenAsync();

        await h.Encina.DidNotReceiveWithAnyArgs().Publish<BreakTheGlassOpenedNotification>(default!, default);
    }

    [Fact]
    public async Task Open_PublishFails_ReturnsTheError()
    {
        var h = new Harness();
        h.Encina.Publish(Arg.Any<BreakTheGlassOpenedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Failure()));

        (await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(Request(), CancellationToken.None)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Open_ConvenienceOverload_OpensTheSameWay()
    {
        var h = new Harness();

        var result = await h.Sut(BreakTheGlassTestKit.Clinician()).OpenAsync(SecretSubject, BreakTheGlassTestKit.Scope, BreakTheGlassTestKit.Justification, TimeSpan.FromMinutes(30));

        Ok(result).ExpiresAtUtc.ShouldBe(Now.AddMinutes(30));
    }

    // ── RevokeAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_ByOpener_ClosesAndAudits()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        h.Time.Advance(TimeSpan.FromMinutes(5));

        var result = await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(elevation.ElevationId, "emergency_over", CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var stored = Ok(await h.InMemory.GetByIdAsync("tenant-1", elevation.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!;
        stored.Status.ShouldBe(BreakTheGlassStatus.Revoked);
        stored.CloseReason.ShouldBe("emergency_over");
        stored.ClosedAtUtc.ShouldBe(Now.AddMinutes(5));
        h.Audit.GetAllEntries().ShouldContain(e => e.Action == "break_glass.revoked" && e.EntityId == elevation.ElevationId.ToString("D"));
    }

    [Fact]
    public async Task Revoke_ByReviewer_IsAllowed()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();

        (await h.Sut(BreakTheGlassTestKit.Reviewer()).RevokeAsync(elevation.ElevationId, "policy", CancellationToken.None)).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Revoke_ByAnotherClinician_IsForbidden()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();

        Code(await h.Sut(BreakTheGlassTestKit.Clinician("clin-2")).RevokeAsync(elevation.ElevationId, "x", CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task Revoke_Unknown_IsNotFound_BlankReason_IsValidation_Unauthenticated()
    {
        var h = new Harness();

        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(Guid.NewGuid(), "x", CancellationToken.None)).ShouldBe(BreakTheGlassErrors.NotFoundCode);
        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(Guid.NewGuid(), "  ", CancellationToken.None)).ShouldBe(BreakTheGlassErrors.ValidationFailedCode);
        Code(await h.Sut(null).RevokeAsync(Guid.NewGuid(), "x", CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task Revoke_AlreadyClosed_IsNotActive()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(elevation.ElevationId, "once", CancellationToken.None);

        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(elevation.ElevationId, "twice", CancellationToken.None)).ShouldBe(BreakTheGlassErrors.NotActiveCode);
    }

    [Fact]
    public async Task Revoke_AuditFails_ElevationStaysActive()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        var audit = Substitute.For<IOperationAuditStore>();
        audit.RecordAsync(default!, default).ReturnsForAnyArgs(ValueTask.FromResult(Failure()));
        h.AuditStore = audit;

        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(elevation.ElevationId, "x", CancellationToken.None)).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
        Ok(await h.InMemory.GetByIdAsync("tenant-1", elevation.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!.Status.ShouldBe(BreakTheGlassStatus.Active);
    }

    [Fact]
    public async Task Revoke_CloseFailsInStore_ReturnsStoreFailed()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetByIdAsync(default, default, default).ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(Some(elevation))));
        store.TryCloseAsync(default, default, default, default!, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Left<EncinaError, bool>(EncinaError.New("boom"))));
        h.Store = store;

        Code(await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(elevation.ElevationId, "x", CancellationToken.None)).ShouldBe(BreakTheGlassErrors.StoreFailedCode);
    }

    // ── ReviewAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task Review_Justified_RecordsAndPublishes_AndKeepsElevationActive()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();

        var result = await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, " fine ", CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var stored = Ok(await h.InMemory.GetByIdAsync("tenant-1", elevation.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!;
        stored.ReviewOutcome.ShouldBe(BreakTheGlassReviewOutcome.Justified);
        stored.ReviewedByUserId.ShouldBe("rev-1");
        stored.ReviewNote.ShouldBe("fine");
        stored.Status.ShouldBe(BreakTheGlassStatus.Active);
        var entry = h.Audit.GetAllEntries().Single(e => e.Action == "break_glass.reviewed");
        entry.Metadata["outcome"].ShouldBe("Justified");
        await h.Encina.Received(1).Publish(
            Arg.Is<BreakTheGlassReviewedNotification>(n => n.ElevationId == elevation.ElevationId && n.Outcome == BreakTheGlassReviewOutcome.Justified),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Review_Unjustified_RevokesAnActiveElevation()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();

        (await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Unjustified, null, CancellationToken.None)).IsRight.ShouldBeTrue();

        var stored = Ok(await h.InMemory.GetByIdAsync("tenant-1", elevation.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!;
        stored.Status.ShouldBe(BreakTheGlassStatus.Revoked);
        stored.CloseReason.ShouldBe("review_unjustified");
        h.Audit.GetAllEntries().Select(e => e.Action).ShouldContain("break_glass.revoked");
    }

    [Fact]
    public async Task Review_UnjustifiedOnAlreadyClosedElevation_DoesNotRevokeAgain()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        await h.Sut(BreakTheGlassTestKit.Clinician()).RevokeAsync(elevation.ElevationId, "done", CancellationToken.None);
        var before = h.Audit.Count;

        (await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Unjustified, null, CancellationToken.None)).IsRight.ShouldBeTrue();

        h.Audit.Count.ShouldBe(before + 1);
        Ok(await h.InMemory.GetByIdAsync("tenant-1", elevation.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!.CloseReason.ShouldBe("done");
    }

    [Fact]
    public async Task Review_ByOpener_IsSelfReview()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync(user: "rev-1");

        Code(await h.Sut(BreakTheGlassTestKit.Reviewer("rev-1")).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(BreakTheGlassErrors.SelfReviewCode);
    }

    [Fact]
    public async Task Review_WithoutReviewerRole_IsForbidden()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();

        Code(await h.Sut(BreakTheGlassTestKit.Clinician("clin-2")).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task Review_Twice_IsAlreadyReviewed()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None);

        Code(await h.Sut(BreakTheGlassTestKit.Reviewer("rev-2")).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(BreakTheGlassErrors.AlreadyReviewedCode);
    }

    [Fact]
    public async Task Review_RetryOfInterruptedUnjustifiedRuling_FinishesTheRevoke()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        // A first Unjustified ruling was recorded, but the revoke that followed failed.
        await h.Store.TryRecordReviewAsync(
            elevation.TenantId, elevation.ElevationId, BreakTheGlassReviewOutcome.Unjustified, "rev-1", null, DateTimeOffset.UnixEpoch, CancellationToken.None);

        var retried = await h.Sut(BreakTheGlassTestKit.Reviewer("rev-2")).ReviewAsync(
            elevation.ElevationId, BreakTheGlassReviewOutcome.Unjustified, null, CancellationToken.None);

        retried.IsRight.ShouldBeTrue();
        var stored = await h.Store.GetByIdAsync(elevation.TenantId, elevation.ElevationId, CancellationToken.None);
        stored.IsRight.ShouldBeTrue();
        var row = stored.IfLeft(default(Option<BreakTheGlassElevation>)).IfNoneUnsafe((BreakTheGlassElevation?)null);
        row!.Status.ShouldBe(BreakTheGlassStatus.Revoked);
    }

    [Fact]
    public async Task Review_RaceLostAtTheStore_IsAlreadyReviewed()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetByIdAsync(default, default, default).ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(Some(elevation))));
        store.TryRecordReviewAsync(default, default, default, default!, default, default, default)
            .ReturnsForAnyArgs(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        h.Store = store;

        Code(await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(BreakTheGlassErrors.AlreadyReviewedCode);
    }

    [Fact]
    public async Task Review_UnknownElevation_Unauthenticated_AndStoreFailure()
    {
        var h = new Harness();

        Code(await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(BreakTheGlassErrors.NotFoundCode);
        Code(await h.Sut(null).ReviewAsync(Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);

        var store = Substitute.For<IBreakTheGlassStore>();
        store.GetByIdAsync(default, default, default).ReturnsForAnyArgs(ValueTask.FromResult(Left<EncinaError, Option<BreakTheGlassElevation>>(EncinaError.New("boom"))));
        h.Store = store;
        (await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Review_AuditFails_NothingRecorded()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        var audit = Substitute.For<IOperationAuditStore>();
        audit.RecordAsync(default!, default).ReturnsForAnyArgs(ValueTask.FromResult(Failure()));
        h.AuditStore = audit;

        Code(await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(BreakTheGlassErrors.AuditFailedCode);
        Ok(await h.InMemory.GetByIdAsync("tenant-1", elevation.ElevationId)).IfNoneUnsafe((BreakTheGlassElevation?)null)!.ReviewOutcome.ShouldBeNull();
    }

    [Fact]
    public async Task Review_NotificationsDisabled_DoesNotPublish()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();
        h.Options.PublishNotifications = false;

        await h.Sut(BreakTheGlassTestKit.Reviewer()).ReviewAsync(elevation.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None);

        await h.Encina.DidNotReceiveWithAnyArgs().Publish<BreakTheGlassReviewedNotification>(default!, default);
    }

    [Fact]
    public async Task RequireTenant_MissingTenant_IsRefusedByEveryOperation()
    {
        var h = new Harness();
        h.Options.RequireTenant = true;
        var id = Guid.NewGuid();
        var clinician = h.Sut(BreakTheGlassTestKit.Clinician(), tenant: null);
        var reviewer = h.Sut(BreakTheGlassTestKit.Reviewer(), tenant: null);

        Code(await clinician.OpenAsync(Request(), CancellationToken.None)).ShouldBe(BreakTheGlassErrors.TenantRequiredCode);
        Code(await clinician.RevokeAsync(id, "reason", CancellationToken.None)).ShouldBe(BreakTheGlassErrors.TenantRequiredCode);
        Code(await reviewer.ReviewAsync(id, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None)).ShouldBe(BreakTheGlassErrors.TenantRequiredCode);
        Code(await reviewer.GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None)).ShouldBe(BreakTheGlassErrors.TenantRequiredCode);
        Code(await clinician.GetAsync(id, CancellationToken.None)).ShouldBe(BreakTheGlassErrors.TenantRequiredCode);
        h.Audit.Count.ShouldBe(0);
    }

    // ── Queue and Get ───────────────────────────────────────────────────

    [Fact]
    public async Task ReviewQueue_Reviewer_SeesOwnTenantOldestFirst()
    {
        var h = new Harness();
        await h.OpenAsync("clin-1");
        await h.OpenAsync("clin-9", tenant: "tenant-2");

        var page = Ok(await h.Sut(BreakTheGlassTestKit.Reviewer()).GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None));

        page.TotalCount.ShouldBe(1);
        page.Items.Single().UserId.ShouldBe("clin-1");
    }

    [Fact]
    public async Task ReviewQueue_NonReviewer_Forbidden_Unauthenticated_Unauthenticated()
    {
        var h = new Harness();

        var forbidden = await h.Sut(BreakTheGlassTestKit.Clinician()).GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None);
        Code(forbidden).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        ErrorOf(forbidden).GetDetails()["reason"].ShouldBe("break_glass_reviewer_not_eligible");
        Code(await h.Sut(null).GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        Code(await h.Sut(TestIdentity.Service("w", roles: ["reviewer"])).GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task Get_OpenerAndReviewerSee_StrangerRefused_UnauthenticatedRefused()
    {
        var h = new Harness();
        var elevation = await h.OpenAsync();

        (await h.Sut(BreakTheGlassTestKit.Clinician()).GetAsync(elevation.ElevationId, CancellationToken.None)).IsRight.ShouldBeTrue();
        (await h.Sut(BreakTheGlassTestKit.Reviewer()).GetAsync(elevation.ElevationId, CancellationToken.None)).IsRight.ShouldBeTrue();
        Code(await h.Sut(BreakTheGlassTestKit.Clinician("clin-2")).GetAsync(elevation.ElevationId, CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        Code(await h.Sut(null).GetAsync(elevation.ElevationId, CancellationToken.None)).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
    }
}
