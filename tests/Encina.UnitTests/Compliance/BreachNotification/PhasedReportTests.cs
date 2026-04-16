#pragma warning disable CA2012

using Encina.Compliance.BreachNotification.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="PhasedReport"/>.
/// </summary>
public class PhasedReportTests
{
    #region Create Tests

    [Fact]
    public void Create_ValidParameters_ShouldSetAllProperties()
    {
        // Arrange
        var breachId = "abc123def456";
        var reportNumber = 2;
        var content = "Additional data categories identified: health records";
        var submittedAtUtc = new DateTimeOffset(2026, 3, 2, 14, 0, 0, TimeSpan.Zero);
        var submittedByUserId = "user-42";

        // Act
        var report = PhasedReport.Create(
            breachId,
            reportNumber,
            content,
            submittedAtUtc,
            submittedByUserId);

        // Assert
        report.BreachId.ShouldBe(breachId);
        report.ReportNumber.ShouldBe(reportNumber);
        report.Content.ShouldBe(content);
        report.SubmittedAtUtc.ShouldBe(submittedAtUtc);
        report.SubmittedByUserId.ShouldBe(submittedByUserId);
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Act
        var report = PhasedReport.Create(
            breachId: "breach-001",
            reportNumber: 1,
            content: "Initial report",
            submittedAtUtc: DateTimeOffset.UtcNow);

        // Assert
        report.Id.ShouldNotBeNullOrWhiteSpace();
        report.Id.Length.ShouldBe(32);
        report.Id.ShouldMatch("^[0-9a-f]{32}$");
    }

    [Fact]
    public void Create_NullSubmittedByUserId_ShouldBeNull()
    {
        // Act
        var report = PhasedReport.Create(
            breachId: "breach-001",
            reportNumber: 1,
            content: "Automated detection report",
            submittedAtUtc: DateTimeOffset.UtcNow,
            submittedByUserId: null);

        // Assert
        report.SubmittedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutSubmittedByUserId_ShouldDefaultToNull()
    {
        // Act
        var report = PhasedReport.Create(
            breachId: "breach-001",
            reportNumber: 1,
            content: "System-generated report",
            submittedAtUtc: DateTimeOffset.UtcNow);

        // Assert
        report.SubmittedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var report1 = PhasedReport.Create("breach-001", 1, "Report 1", DateTimeOffset.UtcNow);
        var report2 = PhasedReport.Create("breach-001", 2, "Report 2", DateTimeOffset.UtcNow);

        // Assert
        report1.Id.ShouldNotBe(report2.Id);
    }

    #endregion

    #region Record Immutability Tests

    [Fact]
    public void PhasedReport_WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = PhasedReport.Create("breach-001", 1, "Original content", DateTimeOffset.UtcNow);

        // Act
        var modified = original with { Content = "Updated content" };

        // Assert
        original.Content.ShouldBe("Original content");
        modified.Content.ShouldBe("Updated content");
        modified.Id.ShouldBe(original.Id);
    }

    #endregion
}
