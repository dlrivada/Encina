using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>Unit tests for <see cref="BreakTheGlassRequestValidator"/> and <see cref="RoleBreakTheGlassAuthorizer"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class ValidatorAndAuthorizerTests
{
    private static BreakTheGlassRequestValidator Validator(Action<BreakTheGlassOptions>? configure = null) =>
        new(BreakTheGlassTestKit.Wrap(BreakTheGlassTestKit.Options(configure)));

    private static BreakTheGlassRequest Valid(
        string subject = "patient-1",
        string scope = BreakTheGlassTestKit.Scope,
        string? justification = null,
        TimeSpan? duration = null) =>
        new(subject, scope, justification ?? BreakTheGlassTestKit.Justification, duration ?? TimeSpan.FromHours(1));

    private static string? Reason(EncinaError error) => error.GetDetails().GetValueOrDefault("reason") as string;

    private static EncinaError LeftOf(Either<EncinaError, BreakTheGlassRequest> result) =>
        result.Match(Right: _ => throw new InvalidOperationException("expected Left"), Left: e => e);

    [Fact]
    public void Validate_ValidRequest_ReturnsTrimmedRequest()
    {
        var result = Validator().Validate(Valid(subject: "  patient-1 ", scope: " clinical-record ", justification: "   " + BreakTheGlassTestKit.Justification + "  "));

        var request = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        request.SubjectId.ShouldBe("patient-1");
        request.Scope.ShouldBe("clinical-record");
        request.Justification.ShouldBe(BreakTheGlassTestKit.Justification);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankSubject_Fails(string subject)
    {
        Reason(LeftOf(Validator().Validate(Valid(subject: subject)))).ShouldBe("subject_required");
    }

    [Fact]
    public void Validate_UndeclaredScope_FailsWithScopeUnknown()
    {
        LeftOf(Validator().Validate(Valid(scope: "other"))).GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.ScopeUnknownCode);
    }

    [Fact]
    public void Validate_ScopeIsCaseSensitive()
    {
        LeftOf(Validator().Validate(Valid(scope: "Clinical-Record"))).GetCode().IfNone(string.Empty).ShouldBe(BreakTheGlassErrors.ScopeUnknownCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too short")]
    [InlineData("                              ")]
    public void Validate_ShortOrBlankJustification_Fails(string justification)
    {
        Reason(LeftOf(Validator().Validate(Valid(justification: justification)))).ShouldBe("justification_too_short");
    }

    [Fact]
    public void Validate_JustificationTrimmedBeforeLengthCheck()
    {
        var padded = new string(' ', 30) + "short" + new string(' ', 30);

        Reason(LeftOf(Validator().Validate(Valid(justification: padded)))).ShouldBe("justification_too_short");
    }

    [Fact]
    public void Validate_JustificationLengthBoundaries()
    {
        Validator().Validate(Valid(justification: new string('a', 20))).IsRight.ShouldBeTrue();
        Validator().Validate(Valid(justification: new string('a', 2000))).IsRight.ShouldBeTrue();
        Reason(LeftOf(Validator().Validate(Valid(justification: new string('a', 2001))))).ShouldBe("justification_too_long");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveDuration_Fails(int minutes)
    {
        Reason(LeftOf(Validator().Validate(Valid(duration: TimeSpan.FromMinutes(minutes))))).ShouldBe("duration_not_positive");
    }

    [Fact]
    public void Validate_DurationAboveMax_Fails_AndMaxIsAllowed()
    {
        Validator().Validate(Valid(duration: TimeSpan.FromHours(4))).IsRight.ShouldBeTrue();
        Reason(LeftOf(Validator().Validate(Valid(duration: TimeSpan.FromHours(4) + TimeSpan.FromTicks(1))))).ShouldBe("duration_too_long");
    }

    [Fact]
    public void Validate_ScopeLimitTightensGlobalLimit()
    {
        var validator = Validator(o => o.Scopes[BreakTheGlassTestKit.Scope].MaxDuration = TimeSpan.FromMinutes(30));

        validator.Validate(Valid(duration: TimeSpan.FromMinutes(30))).IsRight.ShouldBeTrue();
        Reason(LeftOf(validator.Validate(Valid(duration: TimeSpan.FromMinutes(31))))).ShouldBe("duration_too_long");
    }

    [Fact]
    public void Validate_ScopeLimitAboveGlobalLimit_DoesNotRaiseIt()
    {
        var validator = Validator(o => o.Scopes[BreakTheGlassTestKit.Scope].MaxDuration = TimeSpan.FromHours(10));

        Reason(LeftOf(validator.Validate(Valid(duration: TimeSpan.FromHours(5))))).ShouldBe("duration_too_long");
    }

    [Theory]
    [InlineData(25)]
    [InlineData(0)]
    public void Validate_GlobalMaxOutsideRange_Fails(int hours)
    {
        var validator = Validator(o => o.MaxDuration = TimeSpan.FromHours(hours));

        Reason(LeftOf(validator.Validate(Valid()))).ShouldBe("max_duration_invalid");
    }

    [Fact]
    public void Validate_GlobalMaxOfTwentyFourHours_IsAccepted()
    {
        Validator(o => o.MaxDuration = TimeSpan.FromHours(24)).Validate(Valid(duration: TimeSpan.FromHours(24))).IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullRequest_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Validator().Validate(null!));
    }

    // ── Authorizer ──────────────────────────────────────────────────────

    private static RoleBreakTheGlassAuthorizer Authorizer(Action<BreakTheGlassOptions>? configure = null) =>
        new(BreakTheGlassTestKit.Wrap(BreakTheGlassTestKit.Options(configure)));

    private static string CodeOf<T>(Either<EncinaError, T> either) =>
        either.Match(Right: _ => "ok", Left: e => e.GetCode().IfNone(string.Empty));

    [Fact]
    public async Task AuthorizeOpen_UserWithOpenerRole_Allowed()
    {
        CodeOf(await Authorizer().AuthorizeOpenAsync(BreakTheGlassTestKit.Clinician(), BreakTheGlassTestKit.Scope)).ShouldBe("ok");
    }

    [Fact]
    public async Task AuthorizeOpen_UserWithOpenerPermission_Allowed()
    {
        var authorizer = Authorizer(o => o.Scopes[BreakTheGlassTestKit.Scope].OpenerPermissions.Add("btg:open"));

        CodeOf(await authorizer.AuthorizeOpenAsync(TestIdentity.User("u", permissions: ["btg:open"]), BreakTheGlassTestKit.Scope)).ShouldBe("ok");
    }

    [Fact]
    public async Task AuthorizeOpen_UserWithoutRole_RefusedAsForbiddenNotEligible()
    {
        var result = await Authorizer().AuthorizeOpenAsync(TestIdentity.User("u", roles: ["receptionist"]), BreakTheGlassTestKit.Scope);

        CodeOf(result).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        result.Match(Right: _ => string.Empty, Left: e => e.GetDetails()["reason"] as string).ShouldBe("break_glass_not_eligible");
    }

    [Fact]
    public async Task AuthorizeOpen_ServiceIdentityWithRole_Refused()
    {
        var service = TestIdentity.Service("worker", roles: ["clinician"]);

        CodeOf(await Authorizer().AuthorizeOpenAsync(service, BreakTheGlassTestKit.Scope)).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task AuthorizeOpen_AnonymousAndUndeclaredScope_Refused()
    {
        CodeOf(await Authorizer().AuthorizeOpenAsync(TestIdentity.Anonymous, BreakTheGlassTestKit.Scope)).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        CodeOf(await Authorizer().AuthorizeOpenAsync(BreakTheGlassTestKit.Clinician(), "other")).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task AuthorizeOpen_ScopeWithNoOpener_RefusesEveryone()
    {
        var authorizer = Authorizer(o => o.DeclareScope("empty", _ => { }));

        CodeOf(await authorizer.AuthorizeOpenAsync(BreakTheGlassTestKit.Clinician(), "empty")).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task AuthorizeReview_ReviewerRoleAndOtherUser_Allowed()
    {
        var elevation = BreakTheGlassTestKit.Elevation(DateTimeOffset.UnixEpoch);

        CodeOf(await Authorizer().AuthorizeReviewAsync(BreakTheGlassTestKit.Reviewer(), elevation)).ShouldBe("ok");
    }

    [Fact]
    public async Task AuthorizeReview_WithoutReviewerRole_Refused()
    {
        var elevation = BreakTheGlassTestKit.Elevation(DateTimeOffset.UnixEpoch);
        var result = await Authorizer().AuthorizeReviewAsync(BreakTheGlassTestKit.Clinician("other"), elevation);

        CodeOf(result).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        result.Match(Right: _ => string.Empty, Left: e => e.GetDetails()["reason"] as string).ShouldBe("break_glass_reviewer_not_eligible");
    }

    [Fact]
    public async Task AuthorizeReview_ServiceIdentity_Refused()
    {
        var elevation = BreakTheGlassTestKit.Elevation(DateTimeOffset.UnixEpoch);

        CodeOf(await Authorizer().AuthorizeReviewAsync(TestIdentity.Service("w", roles: ["reviewer"]), elevation)).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task AuthorizeReview_OpenerReviewingOwnElevation_RefusedAsSelfReview()
    {
        var elevation = BreakTheGlassTestKit.Elevation(DateTimeOffset.UnixEpoch, userId: "same");
        var identity = TestIdentity.User("same", roles: ["reviewer"]);

        CodeOf(await Authorizer().AuthorizeReviewAsync(identity, elevation)).ShouldBe(BreakTheGlassErrors.SelfReviewCode);
    }
}
