using Encina.Security.BreakTheGlass;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>Unit tests for the break-the-glass model, errors, attribute and options.</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class ModelAndErrorsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Elevation_ToString_OmitsUserSubjectJustificationAndNote()
    {
        var elevation = BreakTheGlassTestKit.Elevation(Now, userId: "user-secret", subjectId: "subject-secret") with
        {
            ReviewNote = "note-secret"
        };

        var text = elevation.ToString();

        text.ShouldNotContain("user-secret");
        text.ShouldNotContain("subject-secret");
        text.ShouldNotContain(BreakTheGlassTestKit.Justification);
        text.ShouldNotContain("note-secret");
        text.ShouldContain(elevation.ElevationId.ToString());
    }

    [Fact]
    public void Request_ToString_OmitsSubjectAndJustification()
    {
        var text = new BreakTheGlassRequest("subject-secret", "clinical-record", "justification-secret", TimeSpan.FromHours(1)).ToString();

        text.ShouldNotContain("subject-secret");
        text.ShouldNotContain("justification-secret");
        text.ShouldContain("clinical-record");
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(59, true)]
    [InlineData(60, false)]
    [InlineData(61, false)]
    public void IsEffectiveAt_ActiveElevation_IsTrueOnlyBeforeExpiry(int minutesAfterOpen, bool expected)
    {
        var elevation = BreakTheGlassTestKit.Elevation(Now, duration: TimeSpan.FromHours(1));

        elevation.IsEffectiveAt(Now.AddMinutes(minutesAfterOpen)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(BreakTheGlassStatus.Expired)]
    [InlineData(BreakTheGlassStatus.Revoked)]
    public void IsEffectiveAt_ClosedElevation_IsFalse(BreakTheGlassStatus status)
    {
        (BreakTheGlassTestKit.Elevation(Now) with { Status = status }).IsEffectiveAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void ReviewQuery_Defaults()
    {
        var query = new BreakTheGlassReviewQuery();

        query.PageNumber.ShouldBe(1);
        query.PageSize.ShouldBe(BreakTheGlassReviewQuery.DefaultPageSize);
        BreakTheGlassReviewQuery.MaxPageSize.ShouldBe(500);
    }

    [Fact]
    public void Enums_KeepPersistedValues()
    {
        ((int)BreakTheGlassStatus.Active).ShouldBe(0);
        ((int)BreakTheGlassStatus.Expired).ShouldBe(1);
        ((int)BreakTheGlassStatus.Revoked).ShouldBe(2);
        ((int)BreakTheGlassReviewOutcome.Justified).ShouldBe(0);
        ((int)BreakTheGlassReviewOutcome.Unjustified).ShouldBe(1);
    }

    [Fact]
    public void NotEligible_IsForbiddenWithReasonDetail()
    {
        var error = BreakTheGlassErrors.NotEligible();

        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        error.GetDetails()["reason"].ShouldBe("break_glass_not_eligible");
    }

    [Fact]
    public void ReviewerNotEligible_IsForbiddenWithReasonDetail()
    {
        var error = BreakTheGlassErrors.ReviewerNotEligible();

        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.AuthorizationForbidden);
        error.GetDetails()["reason"].ShouldBe("break_glass_reviewer_not_eligible");
    }

    [Fact]
    public void ErrorFactories_UseDocumentedCodes()
    {
        Code(BreakTheGlassErrors.ValidationFailed("x")).ShouldBe("break_glass.validation_failed");
        Code(BreakTheGlassErrors.ScopeUnknown()).ShouldBe("break_glass.scope_unknown");
        Code(BreakTheGlassErrors.AlreadyActive()).ShouldBe("break_glass.already_active");
        Code(BreakTheGlassErrors.NotFound()).ShouldBe("break_glass.not_found");
        Code(BreakTheGlassErrors.NotActive()).ShouldBe("break_glass.not_active");
        Code(BreakTheGlassErrors.AlreadyReviewed()).ShouldBe("break_glass.already_reviewed");
        Code(BreakTheGlassErrors.SelfReview()).ShouldBe("break_glass.self_review");
        Code(BreakTheGlassErrors.AuditFailed()).ShouldBe("break_glass.audit_failed");
        Code(BreakTheGlassErrors.StoreFailed()).ShouldBe("break_glass.store_failed");
        Code(BreakTheGlassErrors.StoreFailed(new InvalidOperationException("boom"))).ShouldBe("break_glass.store_failed");
        Code(BreakTheGlassErrors.TenantRequired()).ShouldBe("break_glass.tenant_required");
    }

    [Fact]
    public void ErrorMessages_AreFixedStringsWithoutIds()
    {
        BreakTheGlassErrors.AlreadyActive().Message.ShouldBe("An elevation for this user, subject and scope is already active.");
        BreakTheGlassErrors.NotFound().Message.ShouldBe("The break-the-glass elevation was not found.");
    }

    [Fact]
    public void Attribute_StoresScopeAndSubjectProperty()
    {
        var attribute = new BreakTheGlassEligibleAttribute("clinical-record") { SubjectIdProperty = "PatientId" };

        attribute.Scope.ShouldBe("clinical-record");
        attribute.SubjectIdProperty.ShouldBe("PatientId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Attribute_BlankScope_Throws(string? scope)
    {
        Should.Throw<ArgumentException>(() => new BreakTheGlassEligibleAttribute(scope!));
    }

    [Fact]
    public void Options_Defaults_MatchDesignChoiceSeven()
    {
        var options = new BreakTheGlassOptions();

        options.Enabled.ShouldBeTrue();
        options.MaxDuration.ShouldBe(TimeSpan.FromHours(4));
        options.MinimumJustificationLength.ShouldBe(20);
        options.PublishNotifications.ShouldBeTrue();
        options.RequireTenant.ShouldBeFalse();
        BreakTheGlassOptions.MaxAllowedDuration.ShouldBe(TimeSpan.FromHours(24));
        BreakTheGlassOptions.MaxJustificationLength.ShouldBe(2000);
        options.Scopes.ShouldBeEmpty();
        options.ReviewerRoles.ShouldBeEmpty();
    }

    [Fact]
    public void DeclareScope_AddsScopeAndIsFluent()
    {
        var options = new BreakTheGlassOptions();

        var returned = options.DeclareScope("a", scope => scope.OpenerPermissions.Add("p"));

        returned.ShouldBeSameAs(options);
        options.Scopes["a"].OpenerPermissions.ShouldContain("p");
    }

    [Fact]
    public void DeclareScope_Guards()
    {
        var options = new BreakTheGlassOptions();

        Should.Throw<ArgumentException>(() => options.DeclareScope(" ", _ => { }));
        Should.Throw<ArgumentNullException>(() => options.DeclareScope("a", null!));
    }

    [Fact]
    public void ReadAuditNames_AreStable()
    {
        BreakTheGlassReadAudit.Purpose.ShouldBe("break-the-glass");
        BreakTheGlassReadAudit.ElevationIdMetadataKey.ShouldBe("elevation_id");
        BreakTheGlassReadAudit.DeclaredPurposeMetadataKey.ShouldBe("declared_purpose");
    }

    [Fact]
    public void Notifications_CarryNoSubjectOrJustification()
    {
        var properties = typeof(BreakTheGlassOpenedNotification).GetProperties().Select(p => p.Name)
            .Concat(typeof(BreakTheGlassReviewedNotification).GetProperties().Select(p => p.Name))
            .ToList();

        properties.ShouldNotContain("SubjectId");
        properties.ShouldNotContain("Justification");
        properties.ShouldNotContain("UserId");
    }

    private static string Code(EncinaError error) => error.GetCode().IfNone(string.Empty);
}
