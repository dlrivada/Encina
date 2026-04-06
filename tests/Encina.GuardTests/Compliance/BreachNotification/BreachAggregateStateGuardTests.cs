using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Model;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="BreachAggregate"/> state transition guards
/// that throw <see cref="InvalidOperationException"/> when called from an invalid status.
/// </summary>
public sealed class BreachAggregateStateGuardTests
{
    #region Assess — Invalid State Guards

    [Fact]
    public void Assess_FromInvestigating_ThrowsInvalidOperationException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, "second assessment", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Assess_FromClosed_ThrowsInvalidOperationException()
    {
        var aggregate = CreateClosedAggregate();

        var act = () => aggregate.Assess(
            BreachSeverity.Critical, 500, "assessment attempt", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region ReportToDPA — Invalid State Guards

    [Fact]
    public void ReportToDPA_FromClosed_ThrowsInvalidOperationException()
    {
        var aggregate = CreateClosedAggregate();

        var act = () => aggregate.ReportToDPA(
            "AEPD", "contact@aepd.es", "report", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region NotifySubjects — Invalid State Guards

    [Fact]
    public void NotifySubjects_FromDetected_ThrowsInvalidOperationException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, "email", SubjectNotificationExemption.None, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void NotifySubjects_FromClosed_ThrowsInvalidOperationException()
    {
        var aggregate = CreateClosedAggregate();

        var act = () => aggregate.NotifySubjects(
            50, "email", SubjectNotificationExemption.None, "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region AddPhasedReport — Invalid State Guards

    [Fact]
    public void AddPhasedReport_FromClosed_ThrowsInvalidOperationException()
    {
        var aggregate = CreateClosedAggregate();

        var act = () => aggregate.AddPhasedReport(
            "additional findings", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Contain — Invalid State Guards

    [Fact]
    public void Contain_FromClosed_ThrowsInvalidOperationException()
    {
        var aggregate = CreateClosedAggregate();

        var act = () => aggregate.Contain(
            "revoked access tokens", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Close — Invalid State Guards

    [Fact]
    public void Close_FromDetected_ThrowsInvalidOperationException()
    {
        var aggregate = CreateDetectedAggregate();

        var act = () => aggregate.Close(
            "resolution summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Close_FromInvestigating_ThrowsInvalidOperationException()
    {
        var aggregate = CreateInvestigatingAggregate();

        var act = () => aggregate.Close(
            "resolution summary", "user-1", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
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

    private static BreachAggregate CreateClosedAggregate()
    {
        var aggregate = CreateSubjectsNotifiedAggregate();
        aggregate.Close(
            "Root cause identified and mitigated", "user-1", DateTimeOffset.UtcNow);
        return aggregate;
    }

    #endregion
}
