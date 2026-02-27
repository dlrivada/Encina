using Encina.Compliance.DataSubjectRights;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="InMemoryDSRRequestStore"/> to verify null and invalid parameter handling.
/// </summary>
public class InMemoryDSRRequestStoreGuardTests
{
    private readonly InMemoryDSRRequestStore _store;

    public InMemoryDSRRequestStoreGuardTests()
    {
        _store = new InMemoryDSRRequestStore(
            TimeProvider.System,
            NullLogger<InMemoryDSRRequestStore>.Instance);
    }

    #region Constructor Guard Tests

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null timeProvider.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryDSRRequestStore(
            null!,
            NullLogger<InMemoryDSRRequestStore>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryDSRRequestStore(
            TimeProvider.System,
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region CreateAsync Guard Tests

    /// <summary>
    /// Verifies that CreateAsync throws <see cref="ArgumentNullException"/> for null request.
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = async () => await _store.CreateAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    #endregion

    #region GetByIdAsync Guard Tests

    /// <summary>
    /// Verifies that GetByIdAsync throws <see cref="ArgumentException"/> for invalid id.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_InvalidId_ThrowsArgumentException(string? id)
    {
        var act = async () => await _store.GetByIdAsync(id!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("id");
    }

    #endregion

    #region GetBySubjectIdAsync Guard Tests

    /// <summary>
    /// Verifies that GetBySubjectIdAsync throws <see cref="ArgumentException"/> for invalid subjectId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetBySubjectIdAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _store.GetBySubjectIdAsync(subjectId!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    #endregion

    #region UpdateStatusAsync Guard Tests

    /// <summary>
    /// Verifies that UpdateStatusAsync throws <see cref="ArgumentException"/> for invalid id.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateStatusAsync_InvalidId_ThrowsArgumentException(string? id)
    {
        var act = async () => await _store.UpdateStatusAsync(id!, DSRRequestStatus.Completed, null);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("id");
    }

    #endregion

    #region HasActiveRestrictionAsync Guard Tests

    /// <summary>
    /// Verifies that HasActiveRestrictionAsync throws <see cref="ArgumentException"/> for invalid subjectId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasActiveRestrictionAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _store.HasActiveRestrictionAsync(subjectId!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    #endregion
}
