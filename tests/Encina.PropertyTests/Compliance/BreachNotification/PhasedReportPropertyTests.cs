using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="PhasedReport"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class PhasedReportPropertyTests
{
    #region Create Factory Invariants

    /// <summary>
    /// Invariant: PhasedReport.Create always generates a 32-char hex Id (GUID without hyphens).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysGenerates32CharHexId(
        NonEmptyString breachId,
        PositiveInt reportNumber,
        NonEmptyString content)
    {
        var report = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: reportNumber.Get,
            content: content.Get,
            submittedAtUtc: DateTimeOffset.UtcNow);

        return !string.IsNullOrEmpty(report.Id)
            && report.Id.Length == 32
            && !report.Id.Contains('-');
    }

    /// <summary>
    /// Invariant: PhasedReport.Create preserves BreachId.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesBreachId(NonEmptyString breachId, NonEmptyString content)
    {
        var report = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: 1,
            content: content.Get,
            submittedAtUtc: DateTimeOffset.UtcNow);

        return report.BreachId == breachId.Get;
    }

    /// <summary>
    /// Invariant: PhasedReport.Create preserves ReportNumber.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesReportNumber(NonEmptyString breachId, PositiveInt reportNumber)
    {
        var report = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: reportNumber.Get,
            content: "Report content",
            submittedAtUtc: DateTimeOffset.UtcNow);

        return report.ReportNumber == reportNumber.Get;
    }

    /// <summary>
    /// Invariant: PhasedReport.Create preserves Content.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesContent(NonEmptyString breachId, NonEmptyString content)
    {
        var report = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: 1,
            content: content.Get,
            submittedAtUtc: DateTimeOffset.UtcNow);

        return report.Content == content.Get;
    }

    /// <summary>
    /// Invariant: PhasedReport.Create preserves optional SubmittedByUserId.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesSubmittedByUserId(NonEmptyString breachId, NonEmptyString userId)
    {
        var report = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: 1,
            content: "Report content",
            submittedAtUtc: DateTimeOffset.UtcNow,
            submittedByUserId: userId.Get);

        return report.SubmittedByUserId == userId.Get;
    }

    /// <summary>
    /// Invariant: PhasedReport.Create with null SubmittedByUserId preserves null.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_NullSubmittedByUserId_PreservesNull(NonEmptyString breachId)
    {
        var report = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: 1,
            content: "Report content",
            submittedAtUtc: DateTimeOffset.UtcNow,
            submittedByUserId: null);

        return report.SubmittedByUserId is null;
    }

    /// <summary>
    /// Invariant: Create with different calls produces unique Ids.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_DifferentCallsProduceUniqueIds(NonEmptyString breachId)
    {
        var now = DateTimeOffset.UtcNow;

        var report1 = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: 1,
            content: "Same content",
            submittedAtUtc: now);

        var report2 = PhasedReport.Create(
            breachId: breachId.Get,
            reportNumber: 1,
            content: "Same content",
            submittedAtUtc: now);

        return report1.Id != report2.Id;
    }

    #endregion
}
