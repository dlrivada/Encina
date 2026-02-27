using Encina.Compliance.DataSubjectRights;
using LanguageExt;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance

namespace Encina.ContractTests.Compliance.DataSubjectRights;

/// <summary>
/// Contract tests verifying that <see cref="IDSRRequestStore"/> implementations follow the
/// expected behavioral contract for DSR request lifecycle management.
/// </summary>
public abstract class DSRRequestStoreContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the store being tested.
    /// </summary>
    protected abstract IDSRRequestStore CreateStore();

    #region CreateAsync Contract

    /// <summary>
    /// Contract: CreateAsync with a valid request should succeed.
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_ValidRequest_ShouldSucceed()
    {
        var store = CreateStore();
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);

        var result = await store.CreateAsync(request);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: CreateAsync with a duplicate ID should return an error.
    /// </summary>
    [Fact]
    public async Task Contract_CreateAsync_DuplicateId_ShouldReturnError()
    {
        var store = CreateStore();
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await store.CreateAsync(request);

        var result = await store.CreateAsync(request);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetByIdAsync Contract

    /// <summary>
    /// Contract: GetByIdAsync for an existing request should return Some.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_Existing_ShouldReturnSome()
    {
        var store = CreateStore();
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await store.CreateAsync(request);

        var result = await store.GetByIdAsync("req-001");

        result.IsRight.ShouldBeTrue();
        var option = (Option<DSRRequest>)result;
        option.IsSome.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByIdAsync for a non-existing request should return None.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_NonExisting_ShouldReturnNone()
    {
        var store = CreateStore();

        var result = await store.GetByIdAsync("non-existing");

        result.IsRight.ShouldBeTrue();
        var option = (Option<DSRRequest>)result;
        option.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: GetByIdAsync should return the same data that was stored.
    /// </summary>
    [Fact]
    public async Task Contract_GetByIdAsync_ShouldPreserveRequestData()
    {
        var store = CreateStore();
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Erasure, "Delete my data");
        await store.CreateAsync(request);

        var result = await store.GetByIdAsync("req-001");

        var option = (Option<DSRRequest>)result;
        var found = (DSRRequest)option;
        found.Id.ShouldBe("req-001");
        found.SubjectId.ShouldBe("subject-1");
        found.RightType.ShouldBe(DataSubjectRight.Erasure);
        found.RequestDetails.ShouldBe("Delete my data");
        found.Status.ShouldBe(DSRRequestStatus.Received);
    }

    #endregion

    #region GetBySubjectIdAsync Contract

    /// <summary>
    /// Contract: GetBySubjectIdAsync should return all requests for the subject.
    /// </summary>
    [Fact]
    public async Task Contract_GetBySubjectIdAsync_ShouldReturnMatchingRequests()
    {
        var store = CreateStore();
        await store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await store.CreateAsync(CreateRequest("req-002", "subject-1", DataSubjectRight.Erasure));
        await store.CreateAsync(CreateRequest("req-003", "subject-2", DataSubjectRight.Access));

        var result = await store.GetBySubjectIdAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var list = result.RightAsEnumerable().First();
        list.Count.ShouldBe(2);
    }

    /// <summary>
    /// Contract: GetBySubjectIdAsync for unknown subject should return empty list.
    /// </summary>
    [Fact]
    public async Task Contract_GetBySubjectIdAsync_UnknownSubject_ShouldReturnEmpty()
    {
        var store = CreateStore();

        var result = await store.GetBySubjectIdAsync("unknown");

        result.IsRight.ShouldBeTrue();
        var list = result.RightAsEnumerable().First();
        list.Count.ShouldBe(0);
    }

    #endregion

    #region UpdateStatusAsync Contract

    /// <summary>
    /// Contract: UpdateStatusAsync should update the request status.
    /// </summary>
    [Fact]
    public async Task Contract_UpdateStatusAsync_ShouldChangeStatus()
    {
        var store = CreateStore();
        await store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));

        var result = await store.UpdateStatusAsync("req-001", DSRRequestStatus.InProgress, null);

        result.IsRight.ShouldBeTrue();
        var getResult = await store.GetByIdAsync("req-001");
        var option = (Option<DSRRequest>)getResult;
        var found = (DSRRequest)option;
        found.Status.ShouldBe(DSRRequestStatus.InProgress);
    }

    /// <summary>
    /// Contract: UpdateStatusAsync for non-existing request should return error.
    /// </summary>
    [Fact]
    public async Task Contract_UpdateStatusAsync_NonExisting_ShouldReturnError()
    {
        var store = CreateStore();

        var result = await store.UpdateStatusAsync("non-existing", DSRRequestStatus.Completed, null);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region HasActiveRestrictionAsync Contract

    /// <summary>
    /// Contract: HasActiveRestrictionAsync should detect pending restriction requests.
    /// </summary>
    [Fact]
    public async Task Contract_HasActiveRestrictionAsync_PendingRestriction_ShouldReturnTrue()
    {
        var store = CreateStore();
        await store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Restriction));

        var result = await store.HasActiveRestrictionAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var hasRestriction = (bool)result;
        hasRestriction.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: HasActiveRestrictionAsync should not detect completed restrictions.
    /// </summary>
    [Fact]
    public async Task Contract_HasActiveRestrictionAsync_CompletedRestriction_ShouldReturnFalse()
    {
        var store = CreateStore();
        await store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Restriction));
        await store.UpdateStatusAsync("req-001", DSRRequestStatus.Completed, null);

        var result = await store.HasActiveRestrictionAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var hasRestriction = (bool)result;
        hasRestriction.ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private static DSRRequest CreateRequest(
        string id,
        string subjectId,
        DataSubjectRight rightType,
        string? requestDetails = null) =>
        DSRRequest.Create(id, subjectId, rightType, DateTimeOffset.UtcNow, requestDetails);

    #endregion
}

/// <summary>
/// Contract tests for <see cref="InMemoryDSRRequestStore"/>.
/// </summary>
public sealed class InMemoryDSRRequestStoreContractTests : DSRRequestStoreContractTestsBase
{
    protected override IDSRRequestStore CreateStore() =>
        new InMemoryDSRRequestStore(TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryDSRRequestStore>.Instance);
}
