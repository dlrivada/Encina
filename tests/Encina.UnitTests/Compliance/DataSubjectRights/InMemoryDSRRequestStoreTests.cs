using Encina.Compliance.DataSubjectRights;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="InMemoryDSRRequestStore"/>.
/// </summary>
public class InMemoryDSRRequestStoreTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<InMemoryDSRRequestStore> _logger;
    private readonly InMemoryDSRRequestStore _store;

    public InMemoryDSRRequestStoreTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<InMemoryDSRRequestStore>>();
        _store = new InMemoryDSRRequestStore(_timeProvider, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryDSRRequestStore(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryDSRRequestStore(_timeProvider, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);

        // Act
        var result = await _store.CreateAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ShouldThrow()
    {
        // Act
        var act = async () => await _store.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ShouldReturnError()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await _store.CreateAsync(request);

        // Act
        var result = await _store.CreateAsync(request);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(DSRErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task CreateAsync_MultipleRequests_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.CreateAsync(CreateRequest("req-002", "subject-1", DataSubjectRight.Erasure));
        await _store.CreateAsync(CreateRequest("req-003", "subject-2", DataSubjectRight.Portability));

        // Assert
        _store.Count.Should().Be(3);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingRequest_ShouldReturnSome()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await _store.CreateAsync(request);

        // Act
        var result = await _store.GetByIdAsync("req-001");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<DSRRequest>)result;
        option.IsSome.Should().BeTrue();
        var found = (DSRRequest)option;
        found.Id.Should().Be("req-001");
        found.SubjectId.Should().Be("subject-1");
        found.RightType.Should().Be(DataSubjectRight.Access);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingRequest_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByIdAsync("non-existing");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<DSRRequest>)result;
        option.IsNone.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_InvalidId_ShouldThrow(string? id)
    {
        // Act
        var act = async () => await _store.GetByIdAsync(id!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetBySubjectIdAsync Tests

    [Fact]
    public async Task GetBySubjectIdAsync_ExistingSubject_ShouldReturnRequests()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.CreateAsync(CreateRequest("req-002", "subject-1", DataSubjectRight.Erasure));
        await _store.CreateAsync(CreateRequest("req-003", "subject-2", DataSubjectRight.Portability));

        // Act
        var result = await _store.GetBySubjectIdAsync("subject-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(r => r.SubjectId == "subject-1");
    }

    [Fact]
    public async Task GetBySubjectIdAsync_NonExistingSubject_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetBySubjectIdAsync("non-existing");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetBySubjectIdAsync_InvalidSubjectId_ShouldThrow(string? subjectId)
    {
        // Act
        var act = async () => await _store.GetBySubjectIdAsync(subjectId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Fact]
    public async Task UpdateStatusAsync_ToCompleted_ShouldSetCompletedAtUtc()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await _store.CreateAsync(request);

        // Act
        var result = await _store.UpdateStatusAsync("req-001", DSRRequestStatus.Completed, null);

        // Assert
        result.IsRight.Should().BeTrue();
        var updated = await GetRequest("req-001");
        updated.Status.Should().Be(DSRRequestStatus.Completed);
        updated.CompletedAtUtc.Should().Be(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task UpdateStatusAsync_ToRejected_ShouldSetRejectionReasonAndCompletedAtUtc()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await _store.CreateAsync(request);

        // Act
        var result = await _store.UpdateStatusAsync("req-001", DSRRequestStatus.Rejected, "Unfounded request");

        // Assert
        result.IsRight.Should().BeTrue();
        var updated = await GetRequest("req-001");
        updated.Status.Should().Be(DSRRequestStatus.Rejected);
        updated.RejectionReason.Should().Be("Unfounded request");
        updated.CompletedAtUtc.Should().Be(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task UpdateStatusAsync_ToExtended_ShouldSetExtensionReasonAndExtendedDeadline()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await _store.CreateAsync(request);

        // Act
        var result = await _store.UpdateStatusAsync("req-001", DSRRequestStatus.Extended, "Complex request");

        // Assert
        result.IsRight.Should().BeTrue();
        var updated = await GetRequest("req-001");
        updated.Status.Should().Be(DSRRequestStatus.Extended);
        updated.ExtensionReason.Should().Be("Complex request");
        updated.ExtendedDeadlineAtUtc.Should().Be(request.DeadlineAtUtc.AddMonths(2));
    }

    [Fact]
    public async Task UpdateStatusAsync_ToIdentityVerified_ShouldSetVerifiedAtUtc()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await _store.CreateAsync(request);

        // Act
        var result = await _store.UpdateStatusAsync("req-001", DSRRequestStatus.IdentityVerified, null);

        // Assert
        result.IsRight.Should().BeTrue();
        var updated = await GetRequest("req-001");
        updated.Status.Should().Be(DSRRequestStatus.IdentityVerified);
        updated.VerifiedAtUtc.Should().Be(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task UpdateStatusAsync_ToInProgress_ShouldUpdateStatusOnly()
    {
        // Arrange
        var request = CreateRequest("req-001", "subject-1", DataSubjectRight.Access);
        await _store.CreateAsync(request);

        // Act
        var result = await _store.UpdateStatusAsync("req-001", DSRRequestStatus.InProgress, null);

        // Assert
        result.IsRight.Should().BeTrue();
        var updated = await GetRequest("req-001");
        updated.Status.Should().Be(DSRRequestStatus.InProgress);
        updated.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingRequest_ShouldReturnError()
    {
        // Act
        var result = await _store.UpdateStatusAsync("non-existing", DSRRequestStatus.Completed, null);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(DSRErrors.RequestNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateStatusAsync_InvalidId_ShouldThrow(string? id)
    {
        // Act
        var act = async () => await _store.UpdateStatusAsync(id!, DSRRequestStatus.Completed, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetPendingRequestsAsync Tests

    [Fact]
    public async Task GetPendingRequestsAsync_WithPendingRequests_ShouldReturnPendingOnly()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.CreateAsync(CreateRequest("req-002", "subject-2", DataSubjectRight.Erasure));
        await _store.CreateAsync(CreateRequest("req-003", "subject-3", DataSubjectRight.Portability));
        await _store.UpdateStatusAsync("req-002", DSRRequestStatus.Completed, null);

        // Act
        var result = await _store.GetPendingRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(r => r.Status == DSRRequestStatus.Received);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_IncludesExtendedAndVerified()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.CreateAsync(CreateRequest("req-002", "subject-2", DataSubjectRight.Erasure));
        await _store.CreateAsync(CreateRequest("req-003", "subject-3", DataSubjectRight.Portability));
        await _store.UpdateStatusAsync("req-001", DSRRequestStatus.IdentityVerified, null);
        await _store.UpdateStatusAsync("req-002", DSRRequestStatus.InProgress, null);
        await _store.UpdateStatusAsync("req-003", DSRRequestStatus.Extended, "Complex request");

        // Act
        var result = await _store.GetPendingRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_ExcludesCompletedAndRejected()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.CreateAsync(CreateRequest("req-002", "subject-2", DataSubjectRight.Erasure));
        await _store.UpdateStatusAsync("req-001", DSRRequestStatus.Completed, null);
        await _store.UpdateStatusAsync("req-002", DSRRequestStatus.Rejected, "Unfounded");

        // Act
        var result = await _store.GetPendingRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingRequestsAsync_EmptyStore_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetPendingRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    #endregion

    #region GetOverdueRequestsAsync Tests

    [Fact]
    public async Task GetOverdueRequestsAsync_PastDeadline_ShouldReturnOverdueRequests()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));

        // Advance time by 31 days (past the 30-day deadline)
        _timeProvider.Advance(TimeSpan.FromDays(31));

        // Act
        var result = await _store.GetOverdueRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(1);
        list[0].Id.Should().Be("req-001");
    }

    [Fact]
    public async Task GetOverdueRequestsAsync_BeforeDeadline_ShouldReturnEmpty()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));

        // Advance time by 29 days (before the 30-day deadline)
        _timeProvider.Advance(TimeSpan.FromDays(29));

        // Act
        var result = await _store.GetOverdueRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOverdueRequestsAsync_ExtendedRequest_ShouldUseExtendedDeadline()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.UpdateStatusAsync("req-001", DSRRequestStatus.Extended, "Complex request");

        // Advance time by 31 days — past original but within extended deadline
        _timeProvider.Advance(TimeSpan.FromDays(31));

        // Act
        var result = await _store.GetOverdueRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty(); // Extended deadline = original + 2 months
    }

    [Fact]
    public async Task GetOverdueRequestsAsync_CompletedRequest_ShouldNotBeOverdue()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.UpdateStatusAsync("req-001", DSRRequestStatus.Completed, null);

        // Advance time past deadline
        _timeProvider.Advance(TimeSpan.FromDays(31));

        // Act
        var result = await _store.GetOverdueRequestsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    #endregion

    #region HasActiveRestrictionAsync Tests

    [Fact]
    public async Task HasActiveRestrictionAsync_NoRestriction_ShouldReturnFalse()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));

        // Act
        var result = await _store.HasActiveRestrictionAsync("subject-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var hasRestriction = (bool)result;
        hasRestriction.Should().BeFalse();
    }

    [Fact]
    public async Task HasActiveRestrictionAsync_ActiveRestriction_ShouldReturnTrue()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Restriction));

        // Act
        var result = await _store.HasActiveRestrictionAsync("subject-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var hasRestriction = (bool)result;
        hasRestriction.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveRestrictionAsync_CompletedRestriction_ShouldReturnFalse()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Restriction));
        await _store.UpdateStatusAsync("req-001", DSRRequestStatus.Completed, null);

        // Act
        var result = await _store.HasActiveRestrictionAsync("subject-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var hasRestriction = (bool)result;
        hasRestriction.Should().BeFalse();
    }

    [Fact]
    public async Task HasActiveRestrictionAsync_DifferentSubject_ShouldReturnFalse()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Restriction));

        // Act
        var result = await _store.HasActiveRestrictionAsync("subject-2");

        // Assert
        result.IsRight.Should().BeTrue();
        var hasRestriction = (bool)result;
        hasRestriction.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasActiveRestrictionAsync_InvalidSubjectId_ShouldThrow(string? subjectId)
    {
        // Act
        var act = async () => await _store.HasActiveRestrictionAsync(subjectId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyStore_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithRequests_ShouldReturnAll()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.CreateAsync(CreateRequest("req-002", "subject-2", DataSubjectRight.Erasure));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
    }

    #endregion

    #region Testing Helpers Tests

    [Fact]
    public async Task GetAllRecords_ShouldReturnAllRequests()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        await _store.CreateAsync(CreateRequest("req-002", "subject-2", DataSubjectRight.Erasure));

        // Act
        var records = _store.GetAllRecords();

        // Assert
        records.Should().HaveCount(2);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllRequests()
    {
        // Arrange
        await _store.CreateAsync(CreateRequest("req-001", "subject-1", DataSubjectRight.Access));
        _store.Count.Should().Be(1);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    [Fact]
    public void Count_EmptyStore_ShouldReturnZero()
    {
        _store.Count.Should().Be(0);
    }

    #endregion

    #region Helpers

    private DSRRequest CreateRequest(
        string id,
        string subjectId,
        DataSubjectRight rightType,
        string? requestDetails = null) =>
        DSRRequest.Create(id, subjectId, rightType, _timeProvider.GetUtcNow(), requestDetails);

    private async Task<DSRRequest> GetRequest(string id)
    {
        var result = await _store.GetByIdAsync(id);
        var option = (Option<DSRRequest>)result;
        return (DSRRequest)option;
    }

    #endregion
}
