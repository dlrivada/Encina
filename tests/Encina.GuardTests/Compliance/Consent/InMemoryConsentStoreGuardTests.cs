using Encina.Compliance.Consent;

namespace Encina.GuardTests.Compliance.Consent;

/// <summary>
/// Guard tests for <see cref="InMemoryConsentStore"/> to verify null and invalid parameter handling.
/// </summary>
public class InMemoryConsentStoreGuardTests
{
    private readonly InMemoryConsentStore _store;

    public InMemoryConsentStoreGuardTests()
    {
        _store = new InMemoryConsentStore(
            TimeProvider.System,
            NullLogger<InMemoryConsentStore>.Instance);
    }

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryConsentStore(
            null!,
            NullLogger<InMemoryConsentStore>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryConsentStore(
            TimeProvider.System,
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region RecordConsentAsync Guard Tests

    [Fact]
    public async Task RecordConsentAsync_NullConsent_ThrowsArgumentNullException()
    {
        var act = async () => await _store.RecordConsentAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("consent");
    }

    #endregion

    #region GetConsentAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConsentAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _store.GetConsentAsync(subjectId!, "marketing");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConsentAsync_InvalidPurpose_ThrowsArgumentException(string? purpose)
    {
        var act = async () => await _store.GetConsentAsync("user-1", purpose!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("purpose");
    }

    #endregion

    #region GetAllConsentsAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllConsentsAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _store.GetAllConsentsAsync(subjectId!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    #endregion

    #region WithdrawConsentAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WithdrawConsentAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _store.WithdrawConsentAsync(subjectId!, "marketing");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WithdrawConsentAsync_InvalidPurpose_ThrowsArgumentException(string? purpose)
    {
        var act = async () => await _store.WithdrawConsentAsync("user-1", purpose!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("purpose");
    }

    #endregion

    #region HasValidConsentAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasValidConsentAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _store.HasValidConsentAsync(subjectId!, "marketing");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasValidConsentAsync_InvalidPurpose_ThrowsArgumentException(string? purpose)
    {
        var act = async () => await _store.HasValidConsentAsync("user-1", purpose!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("purpose");
    }

    #endregion

    #region BulkRecordConsentAsync Guard Tests

    [Fact]
    public async Task BulkRecordConsentAsync_NullConsents_ThrowsArgumentNullException()
    {
        var act = async () => await _store.BulkRecordConsentAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("consents");
    }

    #endregion

    #region BulkWithdrawConsentAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BulkWithdrawConsentAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var act = async () => await _store.BulkWithdrawConsentAsync(subjectId!, ["marketing"]);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task BulkWithdrawConsentAsync_NullPurposes_ThrowsArgumentNullException()
    {
        var act = async () => await _store.BulkWithdrawConsentAsync("user-1", null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("purposes");
    }

    #endregion
}
