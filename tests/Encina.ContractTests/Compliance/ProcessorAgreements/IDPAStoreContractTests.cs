#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

#region Abstract Base Class

/// <summary>
/// Abstract contract tests for <see cref="IDPAStore"/> verifying all implementations
/// behave consistently regardless of backing store technology.
/// </summary>
[Trait("Category", "Contract")]
public abstract class DPAStoreContractTestsBase
{
    protected abstract IDPAStore CreateStore();

    #region AddAsync Contract

    [Fact]
    public async Task Contract_AddThenGetByProcessor_ReturnsSameAgreement()
    {
        var store = CreateStore();
        var dpa = CreateActiveDPA("dpa-1", "processor-1");

        var addResult = await store.AddAsync(dpa);
        addResult.IsRight.ShouldBeTrue("Add should succeed");

        var getResult = await store.GetByProcessorIdAsync("processor-1");
        getResult.IsRight.ShouldBeTrue("GetByProcessor should succeed");

        var list = getResult.Match(l => l, _ => (IReadOnlyList<DataProcessingAgreement>)[]);
        list.Count.ShouldBe(1);
        list[0].Id.ShouldBe("dpa-1");
        list[0].ProcessorId.ShouldBe("processor-1");
    }

    [Fact]
    public async Task Contract_AddDuplicate_ReturnsError()
    {
        var store = CreateStore();
        var dpa = CreateActiveDPA("dpa-dup", "processor-dup");

        var first = await store.AddAsync(dpa);
        first.IsRight.ShouldBeTrue("First add should succeed");

        var second = await store.AddAsync(dpa);
        second.IsLeft.ShouldBeTrue("Duplicate add should return error");
    }

    #endregion

    #region GetByIdAsync Contract

    [Fact]
    public async Task Contract_GetById_ReturnsCorrectAgreement()
    {
        var store = CreateStore();
        var dpa = CreateActiveDPA("dpa-byid", "processor-byid");
        await store.AddAsync(dpa);

        var result = await store.GetByIdAsync("dpa-byid");
        result.IsRight.ShouldBeTrue("GetById should succeed");

        var option = result.Match(o => o, _ => Option<DataProcessingAgreement>.None);
        option.IsSome.ShouldBeTrue("Agreement should be found");
        var retrieved = (DataProcessingAgreement)option;
        retrieved.Id.ShouldBe("dpa-byid");
    }

    [Fact]
    public async Task Contract_GetById_NonExistent_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetByIdAsync("non-existent-dpa");
        result.IsRight.ShouldBeTrue("Get non-existent should succeed");

        var option = result.Match(o => o, _ => Option<DataProcessingAgreement>.None);
        option.IsNone.ShouldBeTrue("Non-existent agreement should return None");
    }

    #endregion

    #region GetActiveByProcessorIdAsync Contract

    [Fact]
    public async Task Contract_GetActiveByProcessorId_ReturnsActiveOnly()
    {
        var store = CreateStore();
        var active = CreateActiveDPA("dpa-active", "processor-act");
        var expired = CreateDPA("dpa-expired", "processor-act", DPAStatus.Expired);
        await store.AddAsync(active);
        await store.AddAsync(expired);

        var result = await store.GetActiveByProcessorIdAsync("processor-act");
        result.IsRight.ShouldBeTrue("GetActive should succeed");

        var option = result.Match(o => o, _ => Option<DataProcessingAgreement>.None);
        option.IsSome.ShouldBeTrue("Active DPA should be found");
        var retrieved = (DataProcessingAgreement)option;
        retrieved.Id.ShouldBe("dpa-active");
        retrieved.Status.ShouldBe(DPAStatus.Active);
    }

    [Fact]
    public async Task Contract_GetActiveByProcessorId_NoActive_ReturnsNone()
    {
        var store = CreateStore();
        var expired = CreateDPA("dpa-exp-only", "processor-noact", DPAStatus.Expired);
        await store.AddAsync(expired);

        var result = await store.GetActiveByProcessorIdAsync("processor-noact");
        result.IsRight.ShouldBeTrue();

        var option = result.Match(o => o, _ => Option<DataProcessingAgreement>.None);
        option.IsNone.ShouldBeTrue("No active DPA should return None");
    }

    #endregion

    #region GetByStatusAsync Contract

    [Fact]
    public async Task Contract_GetByStatus_FiltersCorrectly()
    {
        var store = CreateStore();
        await store.AddAsync(CreateActiveDPA("dpa-s1", "proc-status1"));
        await store.AddAsync(CreateDPA("dpa-s2", "proc-status2", DPAStatus.Expired));
        await store.AddAsync(CreateDPA("dpa-s3", "proc-status3", DPAStatus.Terminated));
        await store.AddAsync(CreateActiveDPA("dpa-s4", "proc-status4"));

        var activeResult = await store.GetByStatusAsync(DPAStatus.Active);
        activeResult.IsRight.ShouldBeTrue();
        var activeList = activeResult.Match(l => l, _ => (IReadOnlyList<DataProcessingAgreement>)[]);
        activeList.Count.ShouldBeGreaterThanOrEqualTo(2);
        activeList.ShouldAllBe(a => a.Status == DPAStatus.Active);

        var expiredResult = await store.GetByStatusAsync(DPAStatus.Expired);
        expiredResult.IsRight.ShouldBeTrue();
        var expiredList = expiredResult.Match(l => l, _ => (IReadOnlyList<DataProcessingAgreement>)[]);
        expiredList.Count.ShouldBeGreaterThanOrEqualTo(1);
        expiredList.ShouldAllBe(a => a.Status == DPAStatus.Expired);
    }

    [Fact]
    public async Task Contract_GetByStatus_NoMatches_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetByStatusAsync(DPAStatus.PendingRenewal);
        result.IsRight.ShouldBeTrue();

        var list = result.Match(l => l, _ => (IReadOnlyList<DataProcessingAgreement>)[]);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region GetExpiringAsync Contract

    [Fact]
    public async Task Contract_GetExpiring_FiltersActiveByThreshold()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;

        var expiringSoon = CreateActiveDPA("dpa-exp-soon", "proc-exp1") with
        {
            ExpiresAtUtc = now.AddDays(10)
        };
        var expiringLater = CreateActiveDPA("dpa-exp-later", "proc-exp2") with
        {
            ExpiresAtUtc = now.AddDays(60)
        };
        var alreadyExpiredStatus = CreateDPA("dpa-already-exp", "proc-exp3", DPAStatus.Expired) with
        {
            ExpiresAtUtc = now.AddDays(-5)
        };

        await store.AddAsync(expiringSoon);
        await store.AddAsync(expiringLater);
        await store.AddAsync(alreadyExpiredStatus);

        var threshold = now.AddDays(30);
        var result = await store.GetExpiringAsync(threshold);
        result.IsRight.ShouldBeTrue("GetExpiring should succeed");

        var list = result.Match(l => l, _ => (IReadOnlyList<DataProcessingAgreement>)[]);
        list.ShouldContain(a => a.Id == "dpa-exp-soon");
        list.ShouldNotContain(a => a.Id == "dpa-exp-later");
        list.ShouldNotContain(a => a.Id == "dpa-already-exp");
    }

    [Fact]
    public async Task Contract_GetExpiring_NoExpiring_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetExpiringAsync(DateTimeOffset.UtcNow.AddDays(30));
        result.IsRight.ShouldBeTrue();

        var list = result.Match(l => l, _ => (IReadOnlyList<DataProcessingAgreement>)[]);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region UpdateAsync Contract

    [Fact]
    public async Task Contract_UpdateAsync_ModifiesStored()
    {
        var store = CreateStore();
        var dpa = CreateActiveDPA("dpa-upd", "proc-upd");
        await store.AddAsync(dpa);

        var updated = dpa with
        {
            Status = DPAStatus.Terminated,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var updateResult = await store.UpdateAsync(updated);
        updateResult.IsRight.ShouldBeTrue("Update should succeed");

        var getResult = await store.GetByIdAsync("dpa-upd");
        var option = getResult.Match(o => o, _ => Option<DataProcessingAgreement>.None);
        option.IsSome.ShouldBeTrue();
        var retrieved = (DataProcessingAgreement)option;
        retrieved.Status.ShouldBe(DPAStatus.Terminated);
    }

    [Fact]
    public async Task Contract_UpdateAsync_NonExistent_ReturnsError()
    {
        var store = CreateStore();
        var dpa = CreateActiveDPA("dpa-ghost", "proc-ghost");

        var result = await store.UpdateAsync(dpa);
        result.IsLeft.ShouldBeTrue("Update non-existent should return error");
    }

    #endregion

    #region Helpers

    private static readonly DPAMandatoryTerms FullyCompliantTerms = new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    protected static DataProcessingAgreement CreateActiveDPA(string id, string processorId) =>
        CreateDPA(id, processorId, DPAStatus.Active);

    protected static DataProcessingAgreement CreateDPA(
        string id, string processorId, DPAStatus status) => new()
        {
            Id = id,
            ProcessorId = processorId,
            Status = status,
            SignedAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
            MandatoryTerms = FullyCompliantTerms,
            HasSCCs = true,
            ProcessingPurposes = ["Payment processing", "Analytics"],
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };

    #endregion
}

#endregion
