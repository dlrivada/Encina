using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="InMemoryDSRAuditStore"/> to verify null and invalid parameter handling.
/// </summary>
public class InMemoryDSRAuditStoreGuardTests
{
    private readonly InMemoryDSRAuditStore _store;

    public InMemoryDSRAuditStoreGuardTests()
    {
        _store = new InMemoryDSRAuditStore(
            NullLogger<InMemoryDSRAuditStore>.Instance);
    }

    #region Constructor Guard Tests

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryDSRAuditStore(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region RecordAsync Guard Tests

    /// <summary>
    /// Verifies that RecordAsync throws <see cref="ArgumentNullException"/> for null entry.
    /// </summary>
    [Fact]
    public async Task RecordAsync_NullEntry_ThrowsArgumentNullException()
    {
        var act = async () => await _store.RecordAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetAuditTrailAsync Guard Tests

    /// <summary>
    /// Verifies that GetAuditTrailAsync throws <see cref="ArgumentException"/> for invalid dsrRequestId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAuditTrailAsync_InvalidDsrRequestId_ThrowsArgumentException(string? dsrRequestId)
    {
        var act = async () => await _store.GetAuditTrailAsync(dsrRequestId!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dsrRequestId");
    }

    #endregion
}
