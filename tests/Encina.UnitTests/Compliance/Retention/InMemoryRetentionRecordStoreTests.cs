using Encina.Compliance.Retention;
using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="InMemoryRetentionRecordStore"/>.
/// </summary>
public class InMemoryRetentionRecordStoreTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<InMemoryRetentionRecordStore> _logger;
    private readonly InMemoryRetentionRecordStore _store;

    public InMemoryRetentionRecordStoreTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<InMemoryRetentionRecordStore>>();
        _store = new InMemoryRetentionRecordStore(_timeProvider, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryRetentionRecordStore(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryRetentionRecordStore(_timeProvider, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRecord_ShouldReturnRight()
    {
        // Arrange
        var record = CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365);

        // Act
        var result = await _store.CreateAsync(record);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRecord_ShouldIncrementCount()
    {
        // Arrange
        var record = CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365);

        // Act
        await _store.CreateAsync(record);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ShouldReturnLeft()
    {
        // Arrange
        var record = CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365);
        await _store.CreateAsync(record);

        // Act
        var result = await _store.CreateAsync(record);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.RecordAlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ShouldNotIncrementCount()
    {
        // Arrange
        var record = CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365);
        await _store.CreateAsync(record);

        // Act
        await _store.CreateAsync(record);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_NullRecord_ShouldThrow()
    {
        // Act
        var act = async () => await _store.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_MultipleRecords_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));
        await _store.CreateAsync(CreateRecord("entity-2", "session-logs", daysUntilExpiry: 30));
        await _store.CreateAsync(CreateRecord("entity-3", "marketing-consent", daysUntilExpiry: 90));

        // Assert
        _store.Count.Should().Be(3);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingRecord_ShouldReturnSome()
    {
        // Arrange
        var record = CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365);
        await _store.CreateAsync(record);

        // Act
        var result = await _store.GetByIdAsync(record.Id);

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<RetentionRecord>)result;
        option.IsSome.Should().BeTrue();
        var found = (RetentionRecord)option;
        found.Id.Should().Be(record.Id);
        found.EntityId.Should().Be("entity-1");
        found.DataCategory.Should().Be("financial-records");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingRecord_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetByIdAsync("non-existing-id");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<RetentionRecord>)result;
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

    #region GetByEntityIdAsync Tests

    [Fact]
    public async Task GetByEntityIdAsync_ExistingEntity_ShouldReturnMatchingRecords()
    {
        // Arrange
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));
        await _store.CreateAsync(CreateRecord("entity-1", "session-logs", daysUntilExpiry: 30));
        await _store.CreateAsync(CreateRecord("entity-2", "financial-records", daysUntilExpiry: 365));

        // Act
        var result = await _store.GetByEntityIdAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(r => r.EntityId == "entity-1");
    }

    [Fact]
    public async Task GetByEntityIdAsync_NonExistingEntity_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetByEntityIdAsync("non-existing-entity");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByEntityIdAsync_InvalidEntityId_ShouldThrow(string? entityId)
    {
        // Act
        var act = async () => await _store.GetByEntityIdAsync(entityId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetExpiredRecordsAsync Tests

    [Fact]
    public async Task GetExpiredRecordsAsync_ActiveRecordPastExpiry_ShouldReturn()
    {
        // Arrange — record expires in 10 days from now
        var record = CreateRecord("entity-1", "session-logs", daysUntilExpiry: 10);
        await _store.CreateAsync(record);

        // Advance past expiry
        _timeProvider.Advance(TimeSpan.FromDays(11));

        // Act
        var result = await _store.GetExpiredRecordsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(1);
        list[0].EntityId.Should().Be("entity-1");
    }

    [Fact]
    public async Task GetExpiredRecordsAsync_ActiveRecordBeforeExpiry_ShouldNotReturn()
    {
        // Arrange — record expires in 30 days from now
        var record = CreateRecord("entity-1", "session-logs", daysUntilExpiry: 30);
        await _store.CreateAsync(record);

        // Advance less than expiry
        _timeProvider.Advance(TimeSpan.FromDays(29));

        // Act
        var result = await _store.GetExpiredRecordsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiredRecordsAsync_DeletedRecord_ShouldNotReturn()
    {
        // Arrange — record already expired and deleted
        var now = _timeProvider.GetUtcNow();
        var record = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = "entity-deleted",
            DataCategory = "session-logs",
            CreatedAtUtc = now.AddDays(-60),
            ExpiresAtUtc = now.AddDays(-30),
            Status = RetentionStatus.Deleted
        };
        await _store.CreateAsync(record);

        // Act
        var result = await _store.GetExpiredRecordsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiredRecordsAsync_UnderLegalHoldRecord_ShouldNotReturn()
    {
        // Arrange — record has expired but is under legal hold
        var now = _timeProvider.GetUtcNow();
        var record = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = "entity-held",
            DataCategory = "financial-records",
            CreatedAtUtc = now.AddDays(-400),
            ExpiresAtUtc = now.AddDays(-30),
            Status = RetentionStatus.UnderLegalHold
        };
        await _store.CreateAsync(record);

        // Act
        var result = await _store.GetExpiredRecordsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiredRecordsAsync_MixedStatuses_ShouldReturnOnlyActiveExpired()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();

        // Active and expired
        var activeExpired = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = "entity-active-expired",
            DataCategory = "session-logs",
            CreatedAtUtc = now.AddDays(-40),
            ExpiresAtUtc = now.AddDays(-10),
            Status = RetentionStatus.Active
        };

        // Active, not yet expired
        var activeNotExpired = CreateRecord("entity-active-live", "session-logs", daysUntilExpiry: 20);

        // Deleted and expired
        var deletedExpired = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = "entity-deleted",
            DataCategory = "session-logs",
            CreatedAtUtc = now.AddDays(-60),
            ExpiresAtUtc = now.AddDays(-30),
            Status = RetentionStatus.Deleted
        };

        await _store.CreateAsync(activeExpired);
        await _store.CreateAsync(activeNotExpired);
        await _store.CreateAsync(deletedExpired);

        // Act
        var result = await _store.GetExpiredRecordsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(1);
        list[0].EntityId.Should().Be("entity-active-expired");
    }

    [Fact]
    public async Task GetExpiredRecordsAsync_EmptyStore_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetExpiredRecordsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    #endregion

    #region GetExpiringWithinAsync Tests

    [Fact]
    public async Task GetExpiringWithinAsync_RecordExpiringWithinWindow_ShouldReturn()
    {
        // Arrange — record expires in 5 days
        var record = CreateRecord("entity-1", "session-logs", daysUntilExpiry: 5);
        await _store.CreateAsync(record);

        // Act — ask for records expiring within 7 days
        var result = await _store.GetExpiringWithinAsync(TimeSpan.FromDays(7));

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(1);
        list[0].EntityId.Should().Be("entity-1");
    }

    [Fact]
    public async Task GetExpiringWithinAsync_RecordExpiringBeyondWindow_ShouldNotReturn()
    {
        // Arrange — record expires in 30 days
        var record = CreateRecord("entity-1", "session-logs", daysUntilExpiry: 30);
        await _store.CreateAsync(record);

        // Act — ask for records expiring within 7 days
        var result = await _store.GetExpiringWithinAsync(TimeSpan.FromDays(7));

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiringWithinAsync_AlreadyExpiredRecord_ShouldNotReturn()
    {
        // Arrange — record already expired (ExpiresAtUtc < now)
        var now = _timeProvider.GetUtcNow();
        var record = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = "entity-expired",
            DataCategory = "session-logs",
            CreatedAtUtc = now.AddDays(-20),
            ExpiresAtUtc = now.AddDays(-5),
            Status = RetentionStatus.Active
        };
        await _store.CreateAsync(record);

        // Act
        var result = await _store.GetExpiringWithinAsync(TimeSpan.FromDays(7));

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiringWithinAsync_DeletedRecord_ShouldNotReturn()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = "entity-deleted",
            DataCategory = "session-logs",
            CreatedAtUtc = now.AddDays(-10),
            ExpiresAtUtc = now.AddDays(3),
            Status = RetentionStatus.Deleted
        };
        await _store.CreateAsync(record);

        // Act
        var result = await _store.GetExpiringWithinAsync(TimeSpan.FromDays(7));

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiringWithinAsync_MultipleRecords_ShouldReturnOnlyWithinWindow()
    {
        // Arrange
        await _store.CreateAsync(CreateRecord("entity-soon", "session-logs", daysUntilExpiry: 3));
        await _store.CreateAsync(CreateRecord("entity-later", "session-logs", daysUntilExpiry: 14));
        await _store.CreateAsync(CreateRecord("entity-far", "financial-records", daysUntilExpiry: 365));

        // Act — ask for records expiring within 7 days
        var result = await _store.GetExpiringWithinAsync(TimeSpan.FromDays(7));

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(1);
        list[0].EntityId.Should().Be("entity-soon");
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Fact]
    public async Task UpdateStatusAsync_ExistingRecord_ShouldUpdateStatus()
    {
        // Arrange
        var record = CreateRecord("entity-1", "session-logs", daysUntilExpiry: -5);
        await _store.CreateAsync(record);

        // Act
        var result = await _store.UpdateStatusAsync(record.Id, RetentionStatus.Expired);

        // Assert
        result.IsRight.Should().BeTrue();
        var updated = await GetRecord(record.Id);
        updated.Status.Should().Be(RetentionStatus.Expired);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToDeleted_ShouldSetDeletedAtUtc()
    {
        // Arrange
        var record = CreateRecord("entity-1", "session-logs", daysUntilExpiry: -5);
        await _store.CreateAsync(record);
        var expectedDeletedAt = _timeProvider.GetUtcNow();

        // Act
        await _store.UpdateStatusAsync(record.Id, RetentionStatus.Deleted);

        // Assert
        var updated = await GetRecord(record.Id);
        updated.Status.Should().Be(RetentionStatus.Deleted);
        updated.DeletedAtUtc.Should().Be(expectedDeletedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToNonDeleted_ShouldNotSetDeletedAtUtc()
    {
        // Arrange
        var record = CreateRecord("entity-1", "session-logs", daysUntilExpiry: -5);
        await _store.CreateAsync(record);

        // Act
        await _store.UpdateStatusAsync(record.Id, RetentionStatus.Expired);

        // Assert
        var updated = await GetRecord(record.Id);
        updated.DeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingRecord_ShouldReturnLeft()
    {
        // Act
        var result = await _store.UpdateStatusAsync("non-existing-id", RetentionStatus.Deleted);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.RecordNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateStatusAsync_InvalidId_ShouldThrow(string? id)
    {
        // Act
        var act = async () => await _store.UpdateStatusAsync(id!, RetentionStatus.Deleted);

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
    public async Task GetAllAsync_WithRecords_ShouldReturnAll()
    {
        // Arrange
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));
        await _store.CreateAsync(CreateRecord("entity-2", "session-logs", daysUntilExpiry: 30));

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
    public void Count_EmptyStore_ShouldReturnZero()
    {
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Count_AfterCreating_ShouldReflectStoredRecords()
    {
        // Arrange & Act
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));
        await _store.CreateAsync(CreateRecord("entity-2", "session-logs", daysUntilExpiry: 30));

        // Assert
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetAllRecords_ShouldReturnSnapshot()
    {
        // Arrange
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));
        await _store.CreateAsync(CreateRecord("entity-2", "session-logs", daysUntilExpiry: 30));

        // Act
        var records = _store.GetAllRecords();

        // Assert
        records.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllRecords_ShouldReturnReadOnlySnapshot_NotLiveView()
    {
        // Arrange
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));
        var snapshot = _store.GetAllRecords();

        // Act — add another record after snapshot
        await _store.CreateAsync(CreateRecord("entity-2", "session-logs", daysUntilExpiry: 30));

        // Assert — snapshot is unchanged
        snapshot.Should().HaveCount(1);
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllRecords()
    {
        // Arrange
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));
        _store.Count.Should().Be(1);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Clear_AfterClear_GetAllRecords_ShouldReturnEmpty()
    {
        // Arrange
        await _store.CreateAsync(CreateRecord("entity-1", "financial-records", daysUntilExpiry: 365));

        // Act
        _store.Clear();
        var records = _store.GetAllRecords();

        // Assert
        records.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private RetentionRecord CreateRecord(
        string entityId,
        string dataCategory,
        int daysUntilExpiry) =>
        RetentionRecord.Create(
            entityId: entityId,
            dataCategory: dataCategory,
            createdAtUtc: _timeProvider.GetUtcNow().AddDays(-10),
            expiresAtUtc: _timeProvider.GetUtcNow().AddDays(daysUntilExpiry));

    private async Task<RetentionRecord> GetRecord(string id)
    {
        var result = await _store.GetByIdAsync(id);
        var option = (Option<RetentionRecord>)result;
        return (RetentionRecord)option;
    }

    #endregion
}
