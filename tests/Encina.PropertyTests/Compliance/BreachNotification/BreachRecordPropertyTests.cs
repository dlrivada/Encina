using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="BreachRecord"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class BreachRecordPropertyTests
{
    #region Create Factory Invariants

    /// <summary>
    /// Invariant: BreachRecord.Create always generates a 32-char hex Id (GUID without hyphens).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysGenerates32CharHexId(NonEmptyString nature, PositiveInt subjects)
    {
        var record = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: subjects.Get,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.Medium);

        return !string.IsNullOrEmpty(record.Id)
            && record.Id.Length == 32
            && !record.Id.Contains('-');
    }

    /// <summary>
    /// Invariant: BreachRecord.Create always sets Status to Detected.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysSetsStatusToDetected(NonEmptyString nature, PositiveInt subjects)
    {
        var record = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: subjects.Get,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.High);

        return record.Status == BreachStatus.Detected;
    }

    /// <summary>
    /// Invariant: NotificationDeadlineUtc is always exactly DetectedAtUtc + 72 hours.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_DeadlineIsAlwaysDetectedPlusSeventyTwoHours(NonEmptyString nature)
    {
        var detectedAt = DateTimeOffset.UtcNow;
        var record = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: 100,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: detectedAt,
            severity: BreachSeverity.Medium);

        return record.NotificationDeadlineUtc == detectedAt.AddHours(72);
    }

    /// <summary>
    /// Invariant: PhasedReports always defaults to an empty collection.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PhasedReportsDefaultsToEmpty(NonEmptyString nature)
    {
        var record = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: 100,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.Low);

        return record.PhasedReports.Count == 0;
    }

    /// <summary>
    /// Invariant: SubjectNotificationExemption always defaults to None.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_SubjectNotificationExemptionDefaultsToNone(NonEmptyString nature)
    {
        var record = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: 100,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.Medium);

        return record.SubjectNotificationExemption == SubjectNotificationExemption.None;
    }

    /// <summary>
    /// Invariant: Create with different calls produces unique Ids.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_DifferentCallsProduceUniqueIds(NonEmptyString nature)
    {
        var now = DateTimeOffset.UtcNow;

        var record1 = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: 100,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: now,
            severity: BreachSeverity.Medium);

        var record2 = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: 100,
            categoriesOfDataAffected: ["email"],
            dpoContactDetails: "dpo@test.com",
            likelyConsequences: "Risk",
            measuresTaken: "Measures",
            detectedAtUtc: now,
            severity: BreachSeverity.Medium);

        return record1.Id != record2.Id;
    }

    /// <summary>
    /// Invariant: Create preserves all input parameters.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesAllInputParameters(
        NonEmptyString nature,
        PositiveInt subjects,
        NonEmptyString dpoContact,
        NonEmptyString consequences,
        NonEmptyString measures)
    {
        var now = DateTimeOffset.UtcNow;
        string[] categories = ["names", "emails"];

        var record = BreachRecord.Create(
            nature: nature.Get,
            approximateSubjectsAffected: subjects.Get,
            categoriesOfDataAffected: categories,
            dpoContactDetails: dpoContact.Get,
            likelyConsequences: consequences.Get,
            measuresTaken: measures.Get,
            detectedAtUtc: now,
            severity: BreachSeverity.Critical);

        return record.Nature == nature.Get
            && record.ApproximateSubjectsAffected == subjects.Get
            && record.CategoriesOfDataAffected.Count == 2
            && record.DPOContactDetails == dpoContact.Get
            && record.LikelyConsequences == consequences.Get
            && record.MeasuresTaken == measures.Get
            && record.DetectedAtUtc == now
            && record.Severity == BreachSeverity.Critical;
    }

    #endregion
}
