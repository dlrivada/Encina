#pragma warning disable CA1859 // Use concrete types when possible for improved performance

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying that <see cref="IBreachRecordStore"/> implementations follow the
/// expected behavioral contract for breach record lifecycle management.
/// </summary>
public abstract class BreachRecordStoreContractTestsBase
{
    private static readonly string[] DefaultCategories = ["email", "phone"];

    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract IBreachRecordStore CreateStore();

    #region Helpers

    /// <summary>
    /// Creates a <see cref="BreachRecord"/> with sensible defaults for testing.
    /// </summary>
    protected static BreachRecord CreateTestBreach(
        string? nature = null,
        int approximateSubjectsAffected = 100,
        IReadOnlyList<string>? categoriesOfDataAffected = null,
        string? dpoContactDetails = null,
        string? likelyConsequences = null,
        string? measuresTaken = null,
        DateTimeOffset? detectedAtUtc = null,
        BreachSeverity severity = BreachSeverity.High)
    {
        return BreachRecord.Create(
            nature: nature ?? "Test breach",
            approximateSubjectsAffected: approximateSubjectsAffected,
            categoriesOfDataAffected: categoriesOfDataAffected ?? DefaultCategories,
            dpoContactDetails: dpoContactDetails ?? "dpo@test.com",
            likelyConsequences: likelyConsequences ?? "Identity theft",
            measuresTaken: measuresTaken ?? "Credentials rotated",
            detectedAtUtc: detectedAtUtc ?? DateTimeOffset.UtcNow,
            severity: severity);
    }

    #endregion

    #region RecordBreachAsync Contract

    /// <summary>
    /// Contract: RecordBreachAsync with a valid breach should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_RecordBreachAsync_ValidBreach_ReturnsRight()
    {
        var store = CreateStore();
        var breach = CreateTestBreach();

        var result = await store.RecordBreachAsync(breach);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: RecordBreachAsync with a duplicate ID should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_RecordBreachAsync_DuplicateId_ReturnsLeft()
    {
        var store = CreateStore();
        var breach = CreateTestBreach();

        await store.RecordBreachAsync(breach);
        var result = await store.RecordBreachAsync(breach);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetBreachAsync Contract

    /// <summary>
    /// Contract: GetBreachAsync for an existing ID should return Some with the breach.
    /// </summary>
    [Fact]
    public async Task Contract_GetBreachAsync_ExistingId_ReturnsSome()
    {
        var store = CreateStore();
        var breach = CreateTestBreach();
        await store.RecordBreachAsync(breach);

        var result = await store.GetBreachAsync(breach.Id);

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<BreachRecord>.None);
        option.IsSome.ShouldBeTrue();
        option.Match(Some: r => r.Id.ShouldBe(breach.Id), None: () => throw new InvalidOperationException("Expected Some"));
    }

    /// <summary>
    /// Contract: GetBreachAsync for a non-existing ID should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetBreachAsync_NonExistingId_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetBreachAsync("non-existing-id");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => Option<BreachRecord>.None);
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region UpdateBreachAsync Contract

    /// <summary>
    /// Contract: UpdateBreachAsync for an existing breach should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_UpdateBreachAsync_ExistingBreach_ReturnsRight()
    {
        var store = CreateStore();
        var breach = CreateTestBreach();
        await store.RecordBreachAsync(breach);

        var updated = breach with { Status = BreachStatus.Investigating };
        var result = await store.UpdateBreachAsync(updated);

        result.IsRight.ShouldBeTrue();

        // Verify the update was persisted
        var getResult = await store.GetBreachAsync(breach.Id);
        getResult.IsRight.ShouldBeTrue();
        var option = getResult.Match(Right: o => o, Left: _ => Option<BreachRecord>.None);
        option.Match(
            Some: r => r.Status.ShouldBe(BreachStatus.Investigating),
            None: () => throw new InvalidOperationException("Expected Some"));
    }

    /// <summary>
    /// Contract: UpdateBreachAsync for a non-existing breach should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_UpdateBreachAsync_NonExistingBreach_ReturnsLeft()
    {
        var store = CreateStore();
        var breach = CreateTestBreach();

        var result = await store.UpdateBreachAsync(breach);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetBreachAsync PreservesAllFields Contract

    /// <summary>
    /// Contract: GetBreachAsync should preserve all fields of the stored breach record.
    /// </summary>
    [Fact]
    public async Task Contract_GetBreachAsync_PreservesAllFields()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;
        var breach = BreachRecord.Create(
            nature: "Mass data exfiltration via compromised API endpoint",
            approximateSubjectsAffected: 5000,
            categoriesOfDataAffected: ["names", "email addresses", "phone numbers"],
            dpoContactDetails: "dpo@company.com",
            likelyConsequences: "Identity theft, phishing attacks",
            measuresTaken: "API endpoint disabled, credentials rotated",
            detectedAtUtc: now,
            severity: BreachSeverity.High);

        await store.RecordBreachAsync(breach);

        var result = await store.GetBreachAsync(breach.Id);
        result.IsRight.ShouldBeTrue();

        var option = result.Match(Right: o => o, Left: _ => Option<BreachRecord>.None);
        option.IsSome.ShouldBeTrue();

        var found = option.Match(Some: r => r, None: () => throw new InvalidOperationException("Expected Some but got None"));
        found.Id.ShouldBe(breach.Id);
        found.Nature.ShouldBe("Mass data exfiltration via compromised API endpoint");
        found.ApproximateSubjectsAffected.ShouldBe(5000);
        found.CategoriesOfDataAffected.Count.ShouldBe(3);
        found.DPOContactDetails.ShouldBe("dpo@company.com");
        found.LikelyConsequences.ShouldBe("Identity theft, phishing attacks");
        found.MeasuresTaken.ShouldBe("API endpoint disabled, credentials rotated");
        found.Severity.ShouldBe(BreachSeverity.High);
        found.Status.ShouldBe(BreachStatus.Detected);
        found.NotificationDeadlineUtc.ShouldBe(now.AddHours(72));
        found.PhasedReports.Count.ShouldBe(0);
        found.SubjectNotificationExemption.ShouldBe(SubjectNotificationExemption.None);
    }

    #endregion

    #region GetBreachesByStatusAsync Contract

    /// <summary>
    /// Contract: GetBreachesByStatusAsync with matching status should return matching breaches.
    /// </summary>
    [Fact]
    public async Task Contract_GetBreachesByStatusAsync_MatchingStatus_ReturnsMatches()
    {
        var store = CreateStore();
        var breach1 = CreateTestBreach(nature: "Breach 1");
        var breach2 = CreateTestBreach(nature: "Breach 2");
        var breach3 = CreateTestBreach(nature: "Breach 3");
        await store.RecordBreachAsync(breach1);
        await store.RecordBreachAsync(breach2);
        await store.RecordBreachAsync(breach3);

        // Update one breach to a different status
        var updated = breach3 with { Status = BreachStatus.Investigating };
        await store.UpdateBreachAsync(updated);

        var result = await store.GetBreachesByStatusAsync(BreachStatus.Detected);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(2);
        list.ShouldAllBe(r => r.Status == BreachStatus.Detected);
    }

    /// <summary>
    /// Contract: GetBreachesByStatusAsync with no matching status should return empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetBreachesByStatusAsync_NoMatch_ReturnsEmptyList()
    {
        var store = CreateStore();
        var breach = CreateTestBreach();
        await store.RecordBreachAsync(breach);

        var result = await store.GetBreachesByStatusAsync(BreachStatus.Resolved);

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region GetOverdueBreachesAsync Contract

    /// <summary>
    /// Contract: GetOverdueBreachesAsync should return breaches with past deadline and no authority notification.
    /// </summary>
    [Fact]
    public async Task Contract_GetOverdueBreachesAsync_ReturnsOverdueBreaches()
    {
        var store = CreateStore();

        // Create a breach detected 4 days ago (deadline long passed)
        var overdueBreach = CreateTestBreach(
            nature: "Overdue breach",
            detectedAtUtc: DateTimeOffset.UtcNow.AddDays(-4));
        await store.RecordBreachAsync(overdueBreach);

        // Create a breach detected now (deadline is 72h from now, not overdue)
        var currentBreach = CreateTestBreach(nature: "Current breach");
        await store.RecordBreachAsync(currentBreach);

        var result = await store.GetOverdueBreachesAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.ShouldContain(b => b.Id == overdueBreach.Id);
        list.ShouldNotContain(b => b.Id == currentBreach.Id);
    }

    /// <summary>
    /// Contract: GetOverdueBreachesAsync should not return breaches that have already been notified.
    /// </summary>
    [Fact]
    public async Task Contract_GetOverdueBreachesAsync_NotifiedBreaches_NotReturned()
    {
        var store = CreateStore();

        var breach = CreateTestBreach(
            nature: "Notified overdue breach",
            detectedAtUtc: DateTimeOffset.UtcNow.AddDays(-4));
        await store.RecordBreachAsync(breach);

        // Mark as notified
        var notified = breach with
        {
            Status = BreachStatus.AuthorityNotified,
            NotifiedAuthorityAtUtc = DateTimeOffset.UtcNow
        };
        await store.UpdateBreachAsync(notified);

        var result = await store.GetOverdueBreachesAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.ShouldNotContain(b => b.Id == breach.Id);
    }

    #endregion

    #region GetAllAsync Contract

    /// <summary>
    /// Contract: GetAllAsync should return all stored breaches.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_ReturnsAllStoredBreaches()
    {
        var store = CreateStore();
        var breach1 = CreateTestBreach(nature: "Breach A");
        var breach2 = CreateTestBreach(nature: "Breach B");
        await store.RecordBreachAsync(breach1);
        await store.RecordBreachAsync(breach2);

        var result = await store.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(2);
    }

    /// <summary>
    /// Contract: GetAllAsync on an empty store should return an empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetAllAsync();

        result.IsRight.ShouldBeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region AddPhasedReportAsync Contract

    /// <summary>
    /// Contract: AddPhasedReportAsync for an existing breach should return Right (success).
    /// </summary>
    [Fact]
    public async Task Contract_AddPhasedReportAsync_ExistingBreach_ReturnsRight()
    {
        var store = CreateStore();
        var breach = CreateTestBreach();
        await store.RecordBreachAsync(breach);

        var report = PhasedReport.Create(
            breachId: breach.Id,
            reportNumber: 1,
            content: "Initial phased report content",
            submittedAtUtc: DateTimeOffset.UtcNow,
            submittedByUserId: "test-user");

        var result = await store.AddPhasedReportAsync(breach.Id, report);

        result.IsRight.ShouldBeTrue();

        // Verify the report was persisted
        var getResult = await store.GetBreachAsync(breach.Id);
        getResult.IsRight.ShouldBeTrue();
        var option = getResult.Match(Right: o => o, Left: _ => Option<BreachRecord>.None);
        option.Match(
            Some: r => r.PhasedReports.Count.ShouldBe(1),
            None: () => throw new InvalidOperationException("Expected Some"));
    }

    /// <summary>
    /// Contract: AddPhasedReportAsync for a non-existing breach should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_AddPhasedReportAsync_NonExistingBreach_ReturnsLeft()
    {
        var store = CreateStore();

        var report = PhasedReport.Create(
            breachId: "non-existing-breach",
            reportNumber: 1,
            content: "Report content",
            submittedAtUtc: DateTimeOffset.UtcNow);

        var result = await store.AddPhasedReportAsync("non-existing-breach", report);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
