#pragma warning disable CA2012 // NSubstitute stubbing of ValueTask members

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>
/// Boundary tests of <see cref="BreakTheGlassLimits"/>: a value at its maximum is accepted, one character
/// more is refused with <c>break_glass.validation_failed</c> and no store call.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassLimitsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    // ── the constants ───────────────────────────────────────────────────

    [Fact]
    public void Limits_HaveTheDecidedValues()
    {
        BreakTheGlassLimits.TenantIdMaxLength.ShouldBe(128);
        BreakTheGlassLimits.UserIdMaxLength.ShouldBe(256);
        BreakTheGlassLimits.SubjectIdMaxLength.ShouldBe(256);
        BreakTheGlassLimits.ScopeMaxLength.ShouldBe(256);
        BreakTheGlassLimits.ReviewedByUserIdMaxLength.ShouldBe(256);
        BreakTheGlassLimits.CloseReasonMaxLength.ShouldBe(256);
        BreakTheGlassLimits.ReviewNoteMaxLength.ShouldBe(2000);
        BreakTheGlassLimits.JustificationMaxLength.ShouldBe(2000);
        BreakTheGlassLimits.JustificationMaxLength.ShouldBe(BreakTheGlassOptions.MaxJustificationLength);
    }

    // ── the request validator ───────────────────────────────────────────

    private static BreakTheGlassRequestValidator Validator(string? declaredScope = null) =>
        new(BreakTheGlassTestKit.Wrap(BreakTheGlassTestKit.Options(options =>
        {
            if (declaredScope is not null)
            {
                options.DeclareScope(declaredScope, scope => scope.OpenerRoles.Add("clinician"));
            }
        })));

    private static string? Reason(Either<EncinaError, BreakTheGlassRequest> result) =>
        result.Match(Right: _ => null, Left: error => error.GetDetails().GetValueOrDefault("reason") as string);

    [Fact]
    public void Validate_SubjectAtMax_Passes_AndOneMoreFails()
    {
        var atMax = new BreakTheGlassRequest(new string('s', BreakTheGlassLimits.SubjectIdMaxLength), BreakTheGlassTestKit.Scope, BreakTheGlassTestKit.Justification, TimeSpan.FromHours(1));
        var over = atMax with { SubjectId = new string('s', BreakTheGlassLimits.SubjectIdMaxLength + 1) };

        Validator().Validate(atMax).IsRight.ShouldBeTrue();
        Reason(Validator().Validate(over)).ShouldBe("subject_too_long");
    }

    [Fact]
    public void Validate_ScopeAtMax_Passes_AndOneMoreFails()
    {
        var maxScope = new string('c', BreakTheGlassLimits.ScopeMaxLength);
        var overScope = new string('c', BreakTheGlassLimits.ScopeMaxLength + 1);
        var request = new BreakTheGlassRequest("patient-1", maxScope, BreakTheGlassTestKit.Justification, TimeSpan.FromHours(1));

        Validator(maxScope).Validate(request).IsRight.ShouldBeTrue();
        Reason(Validator(overScope).Validate(request with { Scope = overScope })).ShouldBe("scope_too_long");
    }

    [Fact]
    public void Validate_JustificationAtMax_Passes_AndOneMoreFails()
    {
        var atMax = new BreakTheGlassRequest("patient-1", BreakTheGlassTestKit.Scope, new string('j', BreakTheGlassLimits.JustificationMaxLength), TimeSpan.FromHours(1));
        var over = atMax with { Justification = new string('j', BreakTheGlassLimits.JustificationMaxLength + 1) };

        Validator().Validate(atMax).IsRight.ShouldBeTrue();
        Reason(Validator().Validate(over)).ShouldBe("justification_too_long");
    }

    [Fact]
    public void Validate_LengthIsMeasuredAfterTrimming()
    {
        var padded = new BreakTheGlassRequest(
            " " + new string('s', BreakTheGlassLimits.SubjectIdMaxLength) + " ",
            BreakTheGlassTestKit.Scope,
            " " + new string('j', BreakTheGlassLimits.JustificationMaxLength) + " ",
            TimeSpan.FromHours(1));

        Validator().Validate(padded).IsRight.ShouldBeTrue();
    }

    // ── the service ─────────────────────────────────────────────────────

    private static DefaultBreakTheGlassService Service(IBreakTheGlassStore store, RequestIdentity identity, string? tenant = "tenant-1")
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<BreakTheGlassOpenedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        encina.Publish(Arg.Any<BreakTheGlassReviewedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        var options = BreakTheGlassTestKit.Options();
        return new DefaultBreakTheGlassService(
            store,
            new InMemoryOperationAuditStore(),
            BreakTheGlassTestKit.Accessor(BreakTheGlassTestKit.Context(identity, tenant)),
            new RoleBreakTheGlassAuthorizer(BreakTheGlassTestKit.Wrap(options)),
            new BreakTheGlassRequestValidator(BreakTheGlassTestKit.Wrap(options)),
            encina,
            BreakTheGlassTestKit.Wrap(options),
            new FakeTimeProvider(Now));
    }

    private static BreakTheGlassRequest OpenRequest() =>
        new("patient-1", BreakTheGlassTestKit.Scope, BreakTheGlassTestKit.Justification, TimeSpan.FromHours(1));

    private static string? ReasonOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => null, Left: error => error.GetDetails().GetValueOrDefault("reason") as string);

    private static string CodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => "ok", Left: error => error.GetCode().IfNone(string.Empty));

    private static void ShouldBeRefusedWithoutStoreCall<T>(Either<EncinaError, T> result, string reason, IBreakTheGlassStore store)
    {
        CodeOf(result).ShouldBe(BreakTheGlassErrors.ValidationFailedCode);
        ReasonOf(result).ShouldBe(reason);
        store.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Open_UserIdAtMax_Passes()
    {
        var user = new string('u', BreakTheGlassLimits.UserIdMaxLength);

        var result = await Service(new InMemoryBreakTheGlassStore(), BreakTheGlassTestKit.Clinician(user)).OpenAsync(OpenRequest(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Open_UserIdOverMax_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        var user = new string('u', BreakTheGlassLimits.UserIdMaxLength + 1);

        var result = await Service(store, BreakTheGlassTestKit.Clinician(user)).OpenAsync(OpenRequest(), CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "user_id_too_long", store);
    }

    [Fact]
    public async Task Open_TenantIdAtMax_Passes()
    {
        var tenant = new string('t', BreakTheGlassLimits.TenantIdMaxLength);

        var result = await Service(new InMemoryBreakTheGlassStore(), BreakTheGlassTestKit.Clinician(), tenant).OpenAsync(OpenRequest(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Open_TenantIdOverMax_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        var tenant = new string('t', BreakTheGlassLimits.TenantIdMaxLength + 1);

        var result = await Service(store, BreakTheGlassTestKit.Clinician(), tenant).OpenAsync(OpenRequest(), CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "tenant_id_too_long", store);
    }

    [Fact]
    public async Task Open_OverLongSubject_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        var request = OpenRequest() with { SubjectId = new string('s', BreakTheGlassLimits.SubjectIdMaxLength + 1) };

        var result = await Service(store, BreakTheGlassTestKit.Clinician()).OpenAsync(request, CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "subject_too_long", store);
    }

    [Fact]
    public async Task Open_OverLongJustification_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        var request = OpenRequest() with { Justification = new string('j', BreakTheGlassLimits.JustificationMaxLength + 1) };

        var result = await Service(store, BreakTheGlassTestKit.Clinician()).OpenAsync(request, CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "justification_too_long", store);
    }

    [Fact]
    public async Task Revoke_ReasonAtMax_Passes()
    {
        var store = new InMemoryBreakTheGlassStore();
        var opened = (await Service(store, BreakTheGlassTestKit.Clinician()).OpenAsync(OpenRequest(), CancellationToken.None))
            .Match(Right: elevation => elevation, Left: _ => throw new InvalidOperationException());

        var result = await Service(store, BreakTheGlassTestKit.Clinician())
            .RevokeAsync(opened.ElevationId, new string('r', BreakTheGlassLimits.CloseReasonMaxLength), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Revoke_ReasonOverMax_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();

        var result = await Service(store, BreakTheGlassTestKit.Clinician())
            .RevokeAsync(Guid.NewGuid(), new string('r', BreakTheGlassLimits.CloseReasonMaxLength + 1), CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "reason_too_long", store);
    }

    [Fact]
    public async Task Revoke_ReasonLengthIsMeasuredAfterTrimming()
    {
        var store = new InMemoryBreakTheGlassStore();
        var opened = (await Service(store, BreakTheGlassTestKit.Clinician()).OpenAsync(OpenRequest(), CancellationToken.None))
            .Match(Right: elevation => elevation, Left: _ => throw new InvalidOperationException());

        var result = await Service(store, BreakTheGlassTestKit.Clinician())
            .RevokeAsync(opened.ElevationId, " " + new string('r', BreakTheGlassLimits.CloseReasonMaxLength) + " ", CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Review_NoteAtMax_Passes()
    {
        var store = new InMemoryBreakTheGlassStore();
        var opened = (await Service(store, BreakTheGlassTestKit.Clinician()).OpenAsync(OpenRequest(), CancellationToken.None))
            .Match(Right: elevation => elevation, Left: _ => throw new InvalidOperationException());

        var result = await Service(store, BreakTheGlassTestKit.Reviewer())
            .ReviewAsync(opened.ElevationId, BreakTheGlassReviewOutcome.Justified, new string('n', BreakTheGlassLimits.ReviewNoteMaxLength), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Review_NoteOverMax_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();

        var result = await Service(store, BreakTheGlassTestKit.Reviewer())
            .ReviewAsync(Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, new string('n', BreakTheGlassLimits.ReviewNoteMaxLength + 1), CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "note_too_long", store);
    }

    [Fact]
    public async Task Review_ReviewerUserIdOverMax_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        var reviewer = BreakTheGlassTestKit.Reviewer(new string('v', BreakTheGlassLimits.ReviewedByUserIdMaxLength + 1));

        var result = await Service(store, reviewer).ReviewAsync(Guid.NewGuid(), BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "user_id_too_long", store);
    }

    [Fact]
    public async Task Review_ReviewerUserIdAtMax_ReachesTheStore()
    {
        var store = new InMemoryBreakTheGlassStore();
        var opened = (await Service(store, BreakTheGlassTestKit.Clinician()).OpenAsync(OpenRequest(), CancellationToken.None))
            .Match(Right: elevation => elevation, Left: _ => throw new InvalidOperationException());
        var reviewer = BreakTheGlassTestKit.Reviewer(new string('v', BreakTheGlassLimits.ReviewedByUserIdMaxLength));

        var result = await Service(store, reviewer).ReviewAsync(opened.ElevationId, BreakTheGlassReviewOutcome.Justified, null, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetReviewQueue_TenantIdOverMax_IsRefusedWithoutStoreCall()
    {
        var store = Substitute.For<IBreakTheGlassStore>();
        var tenant = new string('t', BreakTheGlassLimits.TenantIdMaxLength + 1);

        var result = await Service(store, BreakTheGlassTestKit.Reviewer(), tenant).GetReviewQueueAsync(new BreakTheGlassReviewQuery(), CancellationToken.None);

        ShouldBeRefusedWithoutStoreCall(result, "tenant_id_too_long", store);
    }
}
