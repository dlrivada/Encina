using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Model;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="BreachAggregate"/> to verify null, empty, and whitespace
/// parameter handling across all factory and instance methods.
/// </summary>
public class BreachAggregateGuardTests
{
    #region Detect Guards — nature

    [Fact]
    public void Detect_NullNature_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), null!, BreachSeverity.High, "rule", 100, "desc",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("nature");
    }

    [Fact]
    public void Detect_EmptyNature_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "", BreachSeverity.High, "rule", 100, "desc",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("nature");
    }

    [Fact]
    public void Detect_WhitespaceNature_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "   ", BreachSeverity.High, "rule", 100, "desc",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("nature");
    }

    #endregion

    #region Detect Guards — detectedByRule

    [Fact]
    public void Detect_NullDetectedByRule_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "data leak", BreachSeverity.High, null!, 100, "desc",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("detectedByRule");
    }

    [Fact]
    public void Detect_EmptyDetectedByRule_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "data leak", BreachSeverity.High, "", 100, "desc",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("detectedByRule");
    }

    [Fact]
    public void Detect_WhitespaceDetectedByRule_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "data leak", BreachSeverity.High, "   ", 100, "desc",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("detectedByRule");
    }

    #endregion

    #region Detect Guards — description

    [Fact]
    public void Detect_NullDescription_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "data leak", BreachSeverity.High, "rule", 100, null!,
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void Detect_EmptyDescription_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "data leak", BreachSeverity.High, "rule", 100, "",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void Detect_WhitespaceDescription_ThrowsArgumentException()
    {
        var act = () => BreachAggregate.Detect(
            Guid.NewGuid(), "data leak", BreachSeverity.High, "rule", 100, "   ",
            null, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    #endregion

    #region Assess Guards — assessmentSummary

    [Fact]
    public void Assess_NullAssessmentSummary_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, null!, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessmentSummary");
    }

    [Fact]
    public void Assess_EmptyAssessmentSummary_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, "", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessmentSummary");
    }

    [Fact]
    public void Assess_WhitespaceAssessmentSummary_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, "   ", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessmentSummary");
    }

    #endregion

    #region Assess Guards — assessedByUserId

    [Fact]
    public void Assess_NullAssessedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, "summary", null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessedByUserId");
    }

    [Fact]
    public void Assess_EmptyAssessedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, "summary", "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessedByUserId");
    }

    [Fact]
    public void Assess_WhitespaceAssessedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, "summary", "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessedByUserId");
    }

    #endregion

    #region ReportToDPA Guards — authorityName

    [Fact]
    public void ReportToDPA_NullAuthorityName_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            null!, "contact@dpa.eu", "report summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("authorityName");
    }

    [Fact]
    public void ReportToDPA_EmptyAuthorityName_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "", "contact@dpa.eu", "report summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("authorityName");
    }

    [Fact]
    public void ReportToDPA_WhitespaceAuthorityName_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "   ", "contact@dpa.eu", "report summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("authorityName");
    }

    #endregion

    #region ReportToDPA Guards — authorityContactInfo

    [Fact]
    public void ReportToDPA_NullAuthorityContactInfo_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", null!, "report summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("authorityContactInfo");
    }

    [Fact]
    public void ReportToDPA_EmptyAuthorityContactInfo_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "", "report summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("authorityContactInfo");
    }

    [Fact]
    public void ReportToDPA_WhitespaceAuthorityContactInfo_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "   ", "report summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("authorityContactInfo");
    }

    #endregion

    #region ReportToDPA Guards — reportSummary

    [Fact]
    public void ReportToDPA_NullReportSummary_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact@dpa.eu", null!, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportSummary");
    }

    [Fact]
    public void ReportToDPA_EmptyReportSummary_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact@dpa.eu", "", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportSummary");
    }

    [Fact]
    public void ReportToDPA_WhitespaceReportSummary_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact@dpa.eu", "   ", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportSummary");
    }

    #endregion

    #region ReportToDPA Guards — reportedByUserId

    [Fact]
    public void ReportToDPA_NullReportedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact@dpa.eu", "report summary", null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportedByUserId");
    }

    [Fact]
    public void ReportToDPA_EmptyReportedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact@dpa.eu", "report summary", "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportedByUserId");
    }

    [Fact]
    public void ReportToDPA_WhitespaceReportedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact@dpa.eu", "report summary", "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportedByUserId");
    }

    #endregion

    #region NotifySubjects Guards — communicationMethod

    [Fact]
    public void NotifySubjects_NullCommunicationMethod_ThrowsArgumentException()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, null!, SubjectNotificationExemption.None, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("communicationMethod");
    }

    [Fact]
    public void NotifySubjects_EmptyCommunicationMethod_ThrowsArgumentException()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, "", SubjectNotificationExemption.None, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("communicationMethod");
    }

    [Fact]
    public void NotifySubjects_WhitespaceCommunicationMethod_ThrowsArgumentException()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, "   ", SubjectNotificationExemption.None, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("communicationMethod");
    }

    #endregion

    #region NotifySubjects Guards — notifiedByUserId

    [Fact]
    public void NotifySubjects_NullNotifiedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, "email", SubjectNotificationExemption.None, null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("notifiedByUserId");
    }

    [Fact]
    public void NotifySubjects_EmptyNotifiedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, "email", SubjectNotificationExemption.None, "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("notifiedByUserId");
    }

    [Fact]
    public void NotifySubjects_WhitespaceNotifiedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, "email", SubjectNotificationExemption.None, "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("notifiedByUserId");
    }

    #endregion

    #region AddPhasedReport Guards — reportContent

    [Fact]
    public void AddPhasedReport_NullReportContent_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.AddPhasedReport(
            null!, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportContent");
    }

    [Fact]
    public void AddPhasedReport_EmptyReportContent_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.AddPhasedReport(
            "", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportContent");
    }

    [Fact]
    public void AddPhasedReport_WhitespaceReportContent_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.AddPhasedReport(
            "   ", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reportContent");
    }

    #endregion

    #region AddPhasedReport Guards — submittedByUserId

    [Fact]
    public void AddPhasedReport_NullSubmittedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.AddPhasedReport(
            "additional findings", null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("submittedByUserId");
    }

    [Fact]
    public void AddPhasedReport_EmptySubmittedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.AddPhasedReport(
            "additional findings", "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("submittedByUserId");
    }

    [Fact]
    public void AddPhasedReport_WhitespaceSubmittedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.AddPhasedReport(
            "additional findings", "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("submittedByUserId");
    }

    #endregion

    #region Contain Guards — containmentMeasures

    [Fact]
    public void Contain_NullContainmentMeasures_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Contain(
            null!, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("containmentMeasures");
    }

    [Fact]
    public void Contain_EmptyContainmentMeasures_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Contain(
            "", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("containmentMeasures");
    }

    [Fact]
    public void Contain_WhitespaceContainmentMeasures_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Contain(
            "   ", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("containmentMeasures");
    }

    #endregion

    #region Contain Guards — containedByUserId

    [Fact]
    public void Contain_NullContainedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Contain(
            "revoked access tokens", null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("containedByUserId");
    }

    [Fact]
    public void Contain_EmptyContainedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Contain(
            "revoked access tokens", "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("containedByUserId");
    }

    [Fact]
    public void Contain_WhitespaceContainedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Contain(
            "revoked access tokens", "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("containedByUserId");
    }

    #endregion

    #region Close Guards — resolutionSummary

    [Fact]
    public void Close_NullResolutionSummary_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Close(
            null!, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("resolutionSummary");
    }

    [Fact]
    public void Close_EmptyResolutionSummary_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Close(
            "", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("resolutionSummary");
    }

    [Fact]
    public void Close_WhitespaceResolutionSummary_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Close(
            "   ", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("resolutionSummary");
    }

    #endregion

    #region Close Guards — closedByUserId

    [Fact]
    public void Close_NullClosedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Close(
            "root cause identified and mitigated", null!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("closedByUserId");
    }

    [Fact]
    public void Close_EmptyClosedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Close(
            "root cause identified and mitigated", "", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("closedByUserId");
    }

    [Fact]
    public void Close_WhitespaceClosedByUserId_ThrowsArgumentException()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();

        var act = () => aggregate.Close(
            "root cause identified and mitigated", "   ", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("closedByUserId");
    }

    #endregion

    #region Helpers

    private static BreachAggregate CreateDetectedAggregate()
    {
        return BreachAggregate.Detect(
            Guid.NewGuid(), "unauthorized access", BreachSeverity.High,
            "anomaly-detection-rule", 100, "Unauthorized access to user database",
            "user-1", DateTimeOffset.UtcNow);
    }

    private static BreachAggregate CreateInvestigatingAggregate()
    {
        var aggregate = CreateDetectedAggregate();
        aggregate.Assess(
            BreachSeverity.Critical, 500, "Full assessment completed",
            "user-1", DateTimeOffset.UtcNow);
        return aggregate;
    }

    private static BreachAggregate CreateAuthorityNotifiedAggregate()
    {
        var aggregate = CreateInvestigatingAggregate();
        aggregate.ReportToDPA(
            "AEPD", "contact@aepd.es", "Initial report submitted",
            "user-1", DateTimeOffset.UtcNow);
        return aggregate;
    }

    private static BreachAggregate CreateSubjectsNotifiedAggregate()
    {
        var aggregate = CreateAuthorityNotifiedAggregate();
        aggregate.NotifySubjects(
            50, "email", SubjectNotificationExemption.None,
            "user-1", DateTimeOffset.UtcNow);
        return aggregate;
    }

    #endregion
}
