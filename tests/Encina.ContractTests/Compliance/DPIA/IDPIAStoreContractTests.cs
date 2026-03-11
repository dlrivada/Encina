#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.DPIA;

#region Abstract Base Class

/// <summary>
/// Abstract contract tests for <see cref="IDPIAStore"/> verifying all implementations
/// behave consistently regardless of backing store technology.
/// </summary>
[Trait("Category", "Contract")]
public abstract class DPIAStoreContractTestsBase
{
    protected abstract IDPIAStore CreateStore();

    #region SaveAssessmentAsync Contract

    [Fact]
    public async Task Contract_SaveAssessment_ThenGetByRequestType_ReturnsSameAssessment()
    {
        var store = CreateStore();
        var assessment = CreateDraftAssessment("Ns.TestCommand");

        var saveResult = await store.SaveAssessmentAsync(assessment);
        saveResult.IsRight.ShouldBeTrue("Save should succeed");

        var getResult = await store.GetAssessmentAsync("Ns.TestCommand");
        getResult.IsRight.ShouldBeTrue("Get should succeed");

        var retrievedOption = getResult.Match(o => o, _ => Option<DPIAAssessment>.None);
        retrievedOption.IsSome.ShouldBeTrue();
        var retrieved = (DPIAAssessment)retrievedOption;
        retrieved.Id.ShouldBe(assessment.Id);
        retrieved.RequestTypeName.ShouldBe("Ns.TestCommand");
        retrieved.Status.ShouldBe(DPIAAssessmentStatus.Draft);
    }

    [Fact]
    public async Task Contract_SaveAssessment_ThenGetById_ReturnsSameAssessment()
    {
        var store = CreateStore();
        var assessment = CreateDraftAssessment("Ns.GetByIdCommand");

        await store.SaveAssessmentAsync(assessment);

        var getResult = await store.GetAssessmentByIdAsync(assessment.Id);
        getResult.IsRight.ShouldBeTrue("GetById should succeed");

        var retrievedOption = getResult.Match(o => o, _ => Option<DPIAAssessment>.None);
        retrievedOption.IsSome.ShouldBeTrue();
        var retrieved = (DPIAAssessment)retrievedOption;
        retrieved.Id.ShouldBe(assessment.Id);
    }

    [Fact]
    public async Task Contract_SaveAssessment_Upsert_OverwritesExisting()
    {
        var store = CreateStore();
        var original = CreateDraftAssessment("Ns.UpsertCommand");
        await store.SaveAssessmentAsync(original);

        var updated = original with { Status = DPIAAssessmentStatus.Approved };
        await store.SaveAssessmentAsync(updated);

        var getResult = await store.GetAssessmentAsync("Ns.UpsertCommand");
        var retrievedOption = getResult.Match(o => o, _ => Option<DPIAAssessment>.None);
        retrievedOption.IsSome.ShouldBeTrue();
        var retrieved = (DPIAAssessment)retrievedOption;
        retrieved.Status.ShouldBe(DPIAAssessmentStatus.Approved);
    }

    #endregion

    #region GetAssessmentAsync Contract

    [Fact]
    public async Task Contract_GetAssessment_NonExistent_ReturnsNull()
    {
        var store = CreateStore();

        var result = await store.GetAssessmentAsync("Ns.NonExistentCommand");
        result.IsRight.ShouldBeTrue("Get non-existent should succeed");

        var assessmentOption = result.Match(o => o, _ => Option<DPIAAssessment>.None);
        assessmentOption.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetAssessmentById_NonExistent_ReturnsNull()
    {
        var store = CreateStore();

        var result = await store.GetAssessmentByIdAsync(Guid.NewGuid());
        result.IsRight.ShouldBeTrue("GetById non-existent should succeed");

        var assessmentOption = result.Match(o => o, _ => Option<DPIAAssessment>.None);
        assessmentOption.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetExpiredAssessmentsAsync Contract

    [Fact]
    public async Task Contract_GetExpiredAssessments_ReturnsOnlyExpired()
    {
        var store = CreateStore();
        var now = DateTimeOffset.UtcNow;

        var expired = CreateDraftAssessment("Ns.ExpiredCommand") with
        {
            Status = DPIAAssessmentStatus.Approved,
            NextReviewAtUtc = now.AddDays(-30)
        };
        var current = CreateDraftAssessment("Ns.CurrentCommand") with
        {
            Status = DPIAAssessmentStatus.Approved,
            NextReviewAtUtc = now.AddDays(30)
        };

        await store.SaveAssessmentAsync(expired);
        await store.SaveAssessmentAsync(current);

        var result = await store.GetExpiredAssessmentsAsync(now);
        result.IsRight.ShouldBeTrue("GetExpired should succeed");

        var list = result.Match(l => l, _ => (IReadOnlyList<DPIAAssessment>)[]);
        list.ShouldContain(a => a.RequestTypeName == "Ns.ExpiredCommand");
        list.ShouldNotContain(a => a.RequestTypeName == "Ns.CurrentCommand");
    }

    [Fact]
    public async Task Contract_GetExpiredAssessments_Empty_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);
        result.IsRight.ShouldBeTrue();

        var list = result.Match(l => l, _ => (IReadOnlyList<DPIAAssessment>)[]);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region GetAllAssessmentsAsync Contract

    [Fact]
    public async Task Contract_GetAllAssessments_ReturnsAll()
    {
        var store = CreateStore();

        await store.SaveAssessmentAsync(CreateDraftAssessment("Ns.First"));
        await store.SaveAssessmentAsync(CreateDraftAssessment("Ns.Second"));

        var result = await store.GetAllAssessmentsAsync();
        result.IsRight.ShouldBeTrue();

        var list = result.Match(l => l, _ => (IReadOnlyList<DPIAAssessment>)[]);
        list.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region DeleteAssessmentAsync Contract

    [Fact]
    public async Task Contract_DeleteAssessment_ExistingId_Succeeds()
    {
        var store = CreateStore();
        var assessment = CreateDraftAssessment("Ns.DeleteMe");
        await store.SaveAssessmentAsync(assessment);

        var deleteResult = await store.DeleteAssessmentAsync(assessment.Id);
        deleteResult.IsRight.ShouldBeTrue("Delete should succeed");

        var getResult = await store.GetAssessmentByIdAsync(assessment.Id);
        var retrievedOption = getResult.Match(o => o, _ => Option<DPIAAssessment>.None);
        retrievedOption.IsNone.ShouldBeTrue("Deleted assessment should no longer be retrievable");
    }

    [Fact]
    public async Task Contract_DeleteAssessment_NonExistentId_ReturnsError()
    {
        var store = CreateStore();

        var result = await store.DeleteAssessmentAsync(Guid.NewGuid());
        result.IsLeft.ShouldBeTrue("Deleting non-existent should return error");
    }

    #endregion

    #region Helpers

    protected static DPIAAssessment CreateDraftAssessment(string requestTypeName) => new()
    {
        Id = Guid.NewGuid(),
        RequestTypeName = requestTypeName,
        Status = DPIAAssessmentStatus.Draft,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    #endregion
}

#endregion

#region InMemory Concrete Implementation

/// <summary>
/// Contract verification for the in-memory <see cref="IDPIAStore"/> implementation.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InMemoryDPIAStoreContractTests : DPIAStoreContractTestsBase
{
    protected override IDPIAStore CreateStore() =>
        new InMemoryDPIAStore(NullLogger<InMemoryDPIAStore>.Instance);
}

#endregion
