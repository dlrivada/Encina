using Encina.Compliance.NIS2.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.NIS2;

public sealed class NIS2IncidentTests
{
    private static readonly DateTimeOffset TestDetectedAt = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Create Factory

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange & Act
        var incident = NIS2Incident.Create(
            description: "Unauthorized access to critical systems",
            severity: NIS2IncidentSeverity.High,
            detectedAtUtc: TestDetectedAt,
            isSignificant: true,
            affectedServices: ["ServiceA", "ServiceB"],
            initialAssessment: "Data exfiltration suspected");

        // Assert
        incident.Id.ShouldNotBeEmpty();
        incident.Description.ShouldBe("Unauthorized access to critical systems");
        incident.Severity.ShouldBe(NIS2IncidentSeverity.High);
        incident.DetectedAtUtc.ShouldBe(TestDetectedAt);
        incident.IsSignificant.ShouldBeTrue();
        incident.AffectedServices.Count.ShouldBe(2);
        incident.InitialAssessment.ShouldBe("Data exfiltration suspected");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var incident1 = CreateTestIncident();
        var incident2 = CreateTestIncident();

        // Assert
        incident1.Id.ShouldNotBe(incident2.Id);
    }

    #endregion

    #region Timeline Deadlines

    [Fact]
    public void EarlyWarningDeadlineUtc_ShouldBe24HoursAfterDetection()
    {
        // Arrange
        var incident = CreateTestIncident();

        // Assert — Art. 23(4)(a): early warning within 24 hours
        incident.EarlyWarningDeadlineUtc.ShouldBe(TestDetectedAt.AddHours(24));
    }

    [Fact]
    public void IncidentNotificationDeadlineUtc_ShouldBe72HoursAfterDetection()
    {
        // Arrange
        var incident = CreateTestIncident();

        // Assert — Art. 23(4)(b): incident notification within 72 hours
        incident.IncidentNotificationDeadlineUtc.ShouldBe(TestDetectedAt.AddHours(72));
    }

    [Fact]
    public void FinalReportDeadlineUtc_ShouldBeNullForFreshIncident()
    {
        // Arrange — fresh incident has no IncidentNotificationAtUtc, so FinalReport deadline is null
        var incident = CreateTestIncident();

        // Assert — Art. 23(4)(d): final report deadline depends on notification submission
        incident.FinalReportDeadlineUtc.ShouldBeNull();
    }

    [Fact]
    public void FinalReportDeadlineUtc_ShouldBe1MonthAfterNotification()
    {
        // Arrange — simulate notification was submitted at 72h mark
        var notificationTime = TestDetectedAt.AddHours(72);
        var incident = CreateTestIncident() with { IncidentNotificationAtUtc = notificationTime };

        // Assert — Art. 23(4)(d): final report within 1 month of notification
        incident.FinalReportDeadlineUtc.ShouldBe(notificationTime.AddMonths(1));
    }

    [Fact]
    public void DeadlineOrder_ShouldBeMonotonicallyIncreasing()
    {
        // Arrange — set notification time so FinalReportDeadlineUtc is available
        var notificationTime = TestDetectedAt.AddHours(72);
        var incident = CreateTestIncident() with { IncidentNotificationAtUtc = notificationTime };

        // Assert
        incident.EarlyWarningDeadlineUtc.ShouldBeLessThan(incident.IncidentNotificationDeadlineUtc);
        incident.IncidentNotificationDeadlineUtc.ShouldBeLessThan(incident.FinalReportDeadlineUtc!.Value);
    }

    #endregion

    #region Notification Phase Tracking

    [Fact]
    public void Create_ShouldHaveNullNotificationTimestamps()
    {
        // Arrange & Act
        var incident = CreateTestIncident();

        // Assert — freshly created, no notifications filed yet
        incident.EarlyWarningAtUtc.ShouldBeNull();
        incident.IncidentNotificationAtUtc.ShouldBeNull();
        incident.FinalReportAtUtc.ShouldBeNull();
    }

    #endregion

    #region Helpers

    private static NIS2Incident CreateTestIncident(
        NIS2IncidentSeverity severity = NIS2IncidentSeverity.High,
        bool isSignificant = true) =>
        NIS2Incident.Create(
            "Test incident",
            severity,
            TestDetectedAt,
            isSignificant,
            ["ServiceA"],
            "Initial assessment");

    #endregion
}
