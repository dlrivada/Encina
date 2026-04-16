#pragma warning disable CA2012

using Encina.Compliance.BreachNotification.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachRecord"/>.
/// </summary>
public class BreachRecordTests
{
    #region Create Tests

    [Fact]
    public void Create_ValidParameters_ShouldSetAllProperties()
    {
        // Arrange
        var nature = "Unauthorized access to customer database";
        var approximateSubjectsAffected = 5000;
        var categoriesOfDataAffected = new List<string> { "email", "name", "address" };
        var dpoContactDetails = "dpo@company.com";
        var likelyConsequences = "Identity theft risk";
        var measuresTaken = "Password reset enforced";
        var detectedAtUtc = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var severity = BreachSeverity.High;

        // Act
        var record = BreachRecord.Create(
            nature,
            approximateSubjectsAffected,
            categoriesOfDataAffected,
            dpoContactDetails,
            likelyConsequences,
            measuresTaken,
            detectedAtUtc,
            severity);

        // Assert
        record.Nature.ShouldBe(nature);
        record.ApproximateSubjectsAffected.ShouldBe(approximateSubjectsAffected);
        record.CategoriesOfDataAffected.ShouldBe(categoriesOfDataAffected);
        record.DPOContactDetails.ShouldBe(dpoContactDetails);
        record.LikelyConsequences.ShouldBe(likelyConsequences);
        record.MeasuresTaken.ShouldBe(measuresTaken);
        record.DetectedAtUtc.ShouldBe(detectedAtUtc);
        record.Severity.ShouldBe(severity);
    }

    [Fact]
    public void Create_ShouldCalculateDeadline72HoursFromDetection()
    {
        // Arrange
        var detectedAtUtc = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        var record = CreateDefaultRecord(detectedAtUtc: detectedAtUtc);

        // Assert
        record.NotificationDeadlineUtc.ShouldBe(detectedAtUtc.AddHours(72));
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Act
        var record = CreateDefaultRecord();

        // Assert
        record.Id.ShouldNotBeNullOrWhiteSpace();
        record.Id.Length.ShouldBe(32);
        record.Id.ShouldMatch("^[0-9a-f]{32}$");
    }

    [Fact]
    public void Create_ShouldSetStatusToDetected()
    {
        // Act
        var record = CreateDefaultRecord();

        // Assert
        record.Status.ShouldBe(BreachStatus.Detected);
    }

    [Fact]
    public void Create_ShouldDefaultPhasedReportsToEmptyList()
    {
        // Act
        var record = CreateDefaultRecord();

        // Assert
        record.PhasedReports.ShouldNotBeNull();
        record.PhasedReports.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldDefaultExemptionToNone()
    {
        // Act
        var record = CreateDefaultRecord();

        // Assert
        record.SubjectNotificationExemption.ShouldBe(SubjectNotificationExemption.None);
    }

    [Fact]
    public void Create_ShouldDefaultNullablePropertiesToNull()
    {
        // Act
        var record = CreateDefaultRecord();

        // Assert
        record.NotifiedAuthorityAtUtc.ShouldBeNull();
        record.NotifiedSubjectsAtUtc.ShouldBeNull();
        record.ResolvedAtUtc.ShouldBeNull();
        record.ResolutionSummary.ShouldBeNull();
        record.DelayReason.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var record1 = CreateDefaultRecord();
        var record2 = CreateDefaultRecord();

        // Assert
        record1.Id.ShouldNotBe(record2.Id);
    }

    #endregion

    #region Record Immutability Tests

    [Fact]
    public void BreachRecord_WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = CreateDefaultRecord();

        // Act
        var modified = original with { Status = BreachStatus.Investigating };

        // Assert
        original.Status.ShouldBe(BreachStatus.Detected);
        modified.Status.ShouldBe(BreachStatus.Investigating);
        modified.Id.ShouldBe(original.Id);
    }

    #endregion

    #region Helpers

    private static BreachRecord CreateDefaultRecord(
        DateTimeOffset? detectedAtUtc = null,
        BreachSeverity severity = BreachSeverity.High) =>
        BreachRecord.Create(
            nature: "Test breach",
            approximateSubjectsAffected: 100,
            categoriesOfDataAffected: ["email", "name"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Potential identity theft",
            measuresTaken: "Accounts locked",
            detectedAtUtc: detectedAtUtc ?? new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero),
            severity: severity);

    #endregion
}
