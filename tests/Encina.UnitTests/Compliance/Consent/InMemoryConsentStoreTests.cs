using Encina.Compliance.Consent;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="InMemoryConsentStore"/>.
/// </summary>
public class InMemoryConsentStoreTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<InMemoryConsentStore> _logger;
    private readonly InMemoryConsentStore _store;

    public InMemoryConsentStoreTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<InMemoryConsentStore>>();
        _store = new InMemoryConsentStore(_timeProvider, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryConsentStore(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryConsentStore(_timeProvider, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RecordConsentAsync Tests

    [Fact]
    public async Task RecordConsentAsync_ValidConsent_ShouldSucceed()
    {
        // Arrange
        var consent = CreateActiveRecord("user-1", ConsentPurposes.Marketing);

        // Act
        var result = await _store.RecordConsentAsync(consent);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordConsentAsync_NullConsent_ShouldThrow()
    {
        // Act
        var act = async () => await _store.RecordConsentAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RecordConsentAsync_SameSubjectAndPurpose_ShouldOverwrite()
    {
        // Arrange
        var consent1 = CreateActiveRecord("user-1", ConsentPurposes.Marketing, "v1");
        var consent2 = CreateActiveRecord("user-1", ConsentPurposes.Marketing, "v2");

        // Act
        await _store.RecordConsentAsync(consent1);
        await _store.RecordConsentAsync(consent2);

        // Assert
        _store.Count.Should().Be(1);
        var result = await _store.GetConsentAsync("user-1", ConsentPurposes.Marketing);
        result.IsRight.Should().BeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsSome.Should().BeTrue();
        var record = (ConsentRecord)opt;
        record.ConsentVersionId.Should().Be("v2");
    }

    #endregion

    #region GetConsentAsync Tests

    [Fact]
    public async Task GetConsentAsync_ExistingConsent_ShouldReturnRecord()
    {
        // Arrange
        var consent = CreateActiveRecord("user-1", ConsentPurposes.Analytics);
        await _store.RecordConsentAsync(consent);

        // Act
        var result = await _store.GetConsentAsync("user-1", ConsentPurposes.Analytics);

        // Assert
        result.IsRight.Should().BeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsSome.Should().BeTrue();
        var record = (ConsentRecord)opt;
        record.SubjectId.Should().Be("user-1");
        record.Purpose.Should().Be(ConsentPurposes.Analytics);
    }

    [Fact]
    public async Task GetConsentAsync_NonExistentConsent_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetConsentAsync("unknown-user", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetConsentAsync_NullSubjectId_ShouldThrow()
    {
        // Act
        var act = async () => await _store.GetConsentAsync(null!, "purpose");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetConsentAsync_NullPurpose_ShouldThrow()
    {
        // Act
        var act = async () => await _store.GetConsentAsync("user-1", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetConsentAsync_ExpiredConsent_ShouldReturnExpiredStatus()
    {
        // Arrange
        var expiresAt = _timeProvider.GetUtcNow().AddHours(1);
        var consent = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-1",
            Purpose = ConsentPurposes.Marketing,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = _timeProvider.GetUtcNow(),
            ExpiresAtUtc = expiresAt,
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };
        await _store.RecordConsentAsync(consent);

        // Advance time past expiration
        _timeProvider.Advance(TimeSpan.FromHours(2));

        // Act
        var result = await _store.GetConsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsSome.Should().BeTrue();
        var record = (ConsentRecord)opt;
        record.Status.Should().Be(ConsentStatus.Expired);
    }

    #endregion

    #region GetAllConsentsAsync Tests

    [Fact]
    public async Task GetAllConsentsAsync_MultipleConsents_ShouldReturnAll()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Analytics));
        await _store.RecordConsentAsync(CreateActiveRecord("user-2", ConsentPurposes.Marketing));

        // Act
        var result = await _store.GetAllConsentsAsync("user-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var records = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        records.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllConsentsAsync_NoConsents_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAllConsentsAsync("non-existent-user");

        // Assert
        result.IsRight.Should().BeTrue();
        var records = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        records.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllConsentsAsync_NullSubjectId_ShouldThrow()
    {
        // Act
        var act = async () => await _store.GetAllConsentsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region WithdrawConsentAsync Tests

    [Fact]
    public async Task WithdrawConsentAsync_ActiveConsent_ShouldWithdraw()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));

        // Act
        var result = await _store.WithdrawConsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();

        var consent = await _store.GetConsentAsync("user-1", ConsentPurposes.Marketing);
        var opt = (Option<ConsentRecord>)consent;
        opt.IsSome.Should().BeTrue();
        var record = (ConsentRecord)opt;
        record.Status.Should().Be(ConsentStatus.Withdrawn);
        record.WithdrawnAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task WithdrawConsentAsync_NonExistentConsent_ShouldReturnError()
    {
        // Act
        var result = await _store.WithdrawConsentAsync("unknown-user", ConsentPurposes.Marketing);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task WithdrawConsentAsync_NullSubjectId_ShouldThrow()
    {
        // Act
        var act = async () => await _store.WithdrawConsentAsync(null!, "purpose");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region HasValidConsentAsync Tests

    [Fact]
    public async Task HasValidConsentAsync_ActiveConsent_ShouldReturnTrue()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));

        // Act
        var result = await _store.HasValidConsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeTrue();
    }

    [Fact]
    public async Task HasValidConsentAsync_NoConsent_ShouldReturnFalse()
    {
        // Act
        var result = await _store.HasValidConsentAsync("unknown", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    [Fact]
    public async Task HasValidConsentAsync_ExpiredConsent_ShouldReturnFalse()
    {
        // Arrange
        var consent = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-1",
            Purpose = ConsentPurposes.Marketing,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = _timeProvider.GetUtcNow(),
            ExpiresAtUtc = _timeProvider.GetUtcNow().AddMinutes(30),
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };
        await _store.RecordConsentAsync(consent);
        _timeProvider.Advance(TimeSpan.FromHours(1));

        // Act
        var result = await _store.HasValidConsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    [Fact]
    public async Task HasValidConsentAsync_WithdrawnConsent_ShouldReturnFalse()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));
        await _store.WithdrawConsentAsync("user-1", ConsentPurposes.Marketing);

        // Act
        var result = await _store.HasValidConsentAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        ((bool)result).Should().BeFalse();
    }

    #endregion

    #region BulkRecordConsentAsync Tests

    [Fact]
    public async Task BulkRecordConsentAsync_MultipleConsents_ShouldRecordAll()
    {
        // Arrange
        var consents = new[]
        {
            CreateActiveRecord("user-1", ConsentPurposes.Marketing),
            CreateActiveRecord("user-2", ConsentPurposes.Analytics),
            CreateActiveRecord("user-3", ConsentPurposes.Personalization)
        };

        // Act
        var result = await _store.BulkRecordConsentAsync(consents);

        // Assert
        result.IsRight.Should().BeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.AllSucceeded.Should().BeTrue();
        bulkResult.SuccessCount.Should().Be(3);
        _store.Count.Should().Be(3);
    }

    [Fact]
    public async Task BulkRecordConsentAsync_EmptyCollection_ShouldReturnZeroSuccess()
    {
        // Act
        var result = await _store.BulkRecordConsentAsync(System.Array.Empty<ConsentRecord>());

        // Assert
        result.IsRight.Should().BeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.Should().Be(0);
        bulkResult.AllSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task BulkRecordConsentAsync_NullConsents_ShouldThrow()
    {
        // Act
        var act = async () => await _store.BulkRecordConsentAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region BulkWithdrawConsentAsync Tests

    [Fact]
    public async Task BulkWithdrawConsentAsync_MultipleActivePurposes_ShouldWithdrawAll()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Analytics));

        // Act
        var result = await _store.BulkWithdrawConsentAsync(
            "user-1",
            [ConsentPurposes.Marketing, ConsentPurposes.Analytics]);

        // Assert
        result.IsRight.Should().BeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.AllSucceeded.Should().BeTrue();
        bulkResult.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task BulkWithdrawConsentAsync_SomeMissing_ShouldReturnPartialResult()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));

        // Act
        var result = await _store.BulkWithdrawConsentAsync(
            "user-1",
            [ConsentPurposes.Marketing, ConsentPurposes.Analytics]);

        // Assert
        result.IsRight.Should().BeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.Should().Be(1);
        bulkResult.FailureCount.Should().Be(1);
        bulkResult.HasFailures.Should().BeTrue();
    }

    [Fact]
    public async Task BulkWithdrawConsentAsync_NullSubjectId_ShouldThrow()
    {
        // Act
        var act = async () => await _store.BulkWithdrawConsentAsync(null!, ["purpose"]);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Testing Utilities

    [Fact]
    public async Task Clear_ShouldRemoveAllRecords()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));
        await _store.RecordConsentAsync(CreateActiveRecord("user-2", ConsentPurposes.Analytics));
        _store.Count.Should().Be(2);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
        _store.GetAllRecords().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRecords_ShouldReturnAllStoredRecords()
    {
        // Arrange
        await _store.RecordConsentAsync(CreateActiveRecord("user-1", ConsentPurposes.Marketing));
        await _store.RecordConsentAsync(CreateActiveRecord("user-2", ConsentPurposes.Analytics));

        // Act
        var records = _store.GetAllRecords();

        // Assert
        records.Should().HaveCount(2);
    }

    #endregion

    #region Helpers

    private ConsentRecord CreateActiveRecord(
        string subjectId,
        string purpose,
        string versionId = "v1") => new()
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = versionId,
            GivenAtUtc = _timeProvider.GetUtcNow(),
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        };

    #endregion
}
