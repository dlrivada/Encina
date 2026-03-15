using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;

namespace Encina.ContractTests.Compliance.DataSubjectRights;

/// <summary>
/// Contract tests verifying that <see cref="IDSRService"/> implementations follow the
/// expected behavioral contract for DSR request lifecycle management via event sourcing.
/// </summary>
/// <remarks>
/// Replaces the old <c>IDSRRequestStoreContractTests</c> and <c>IDSRAuditStoreContractTests</c>
/// which tested the entity-based persistence model. The event-sourced model unifies lifecycle
/// commands, handler operations, and queries in a single service interface.
/// </remarks>
public abstract class DSRServiceContractTestsBase
{
    /// <summary>
    /// Creates a new instance of the service being tested.
    /// </summary>
    protected abstract IDSRService CreateService();

    #region SubmitRequestAsync Contract

    /// <summary>
    /// Contract: SubmitRequestAsync with valid parameters should return a new Guid.
    /// </summary>
    [Fact]
    public async Task Contract_SubmitRequestAsync_ValidRequest_ShouldReturnGuid()
    {
        var service = CreateService();

        var result = await service.SubmitRequestAsync("subject-1", DataSubjectRight.Access);

        result.IsRight.ShouldBeTrue();
        var id = result.RightAsEnumerable().First();
        id.ShouldNotBe(Guid.Empty);
    }

    /// <summary>
    /// Contract: SubmitRequestAsync should support all GDPR right types.
    /// </summary>
    [Theory]
    [InlineData(DataSubjectRight.Access)]
    [InlineData(DataSubjectRight.Rectification)]
    [InlineData(DataSubjectRight.Erasure)]
    [InlineData(DataSubjectRight.Restriction)]
    [InlineData(DataSubjectRight.Portability)]
    [InlineData(DataSubjectRight.Objection)]
    public async Task Contract_SubmitRequestAsync_AllRightTypes_ShouldSucceed(DataSubjectRight rightType)
    {
        var service = CreateService();

        var result = await service.SubmitRequestAsync("subject-1", rightType);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetRequestAsync Contract

    /// <summary>
    /// Contract: GetRequestAsync for a non-existing request should return Left (error).
    /// </summary>
    [Fact]
    public async Task Contract_GetRequestAsync_NonExisting_ShouldReturnError()
    {
        var service = CreateService();

        var result = await service.GetRequestAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Lifecycle State Machine Contract

    /// <summary>
    /// Contract: VerifyIdentityAsync on a non-existing request should return error.
    /// </summary>
    [Fact]
    public async Task Contract_VerifyIdentityAsync_NonExisting_ShouldReturnError()
    {
        var service = CreateService();

        var result = await service.VerifyIdentityAsync(Guid.NewGuid(), "admin-1");

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: CompleteRequestAsync on a non-existing request should return error.
    /// </summary>
    [Fact]
    public async Task Contract_CompleteRequestAsync_NonExisting_ShouldReturnError()
    {
        var service = CreateService();

        var result = await service.CompleteRequestAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: DenyRequestAsync on a non-existing request should return error.
    /// </summary>
    [Fact]
    public async Task Contract_DenyRequestAsync_NonExisting_ShouldReturnError()
    {
        var service = CreateService();

        var result = await service.DenyRequestAsync(Guid.NewGuid(), "Reason");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handler Operations Contract

    /// <summary>
    /// Contract: HandleObjectionAsync with a valid request should succeed.
    /// </summary>
    [Fact]
    public async Task Contract_HandleObjectionAsync_ValidRequest_ShouldSucceed()
    {
        var service = CreateService();
        var request = new ObjectionRequest("subject-1", "direct-marketing", "I object");

        var result = await service.HandleObjectionAsync(request);

        result.IsRight.ShouldBeTrue();
    }

    #endregion
}
