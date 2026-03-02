using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="InMemoryRetentionAuditStore"/>.
/// </summary>
public class InMemoryRetentionAuditStoreTests
{
    private readonly ILogger<InMemoryRetentionAuditStore> _logger;
    private readonly InMemoryRetentionAuditStore _store;

    public InMemoryRetentionAuditStoreTests()
    {
        _logger = Substitute.For<ILogger<InMemoryRetentionAuditStore>>();
        _store = new InMemoryRetentionAuditStore(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryRetentionAuditStore(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RecordAsync Tests

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldReturnRight()
    {
        // Arrange
        var entry = CreateEntry("entity-1", "PolicyCreated");

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldIncrementCount()
    {
        // Arrange
        var entry = CreateEntry("entity-1", "PolicyCreated");

        // Act
        await _store.RecordAsync(entry);

        // Assert
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ShouldThrow()
    {
        // Act
        var act = async () => await _store.RecordAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RecordAsync_MultipleEntriesForSameEntity_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        await _store.RecordAsync(CreateEntry("entity-1", "EnforcementExecuted"));
        await _store.RecordAsync(CreateEntry("entity-1", "RecordDeleted"));

        // Assert
        _store.Count.Should().Be(3);
    }

    [Fact]
    public async Task RecordAsync_EntriesForDifferentEntities_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        await _store.RecordAsync(CreateEntry("entity-2", "RecordTracked"));

        // Assert
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task RecordAsync_SameEntryTwice_ShouldOverwrite()
    {
        // Arrange — RecordAsync uses entry.Id as key, so same Id is overwritten
        var entry = CreateEntry("entity-1", "RecordTracked");

        // Act
        await _store.RecordAsync(entry);
        await _store.RecordAsync(entry);

        // Assert — overwritten, not duplicated
        _store.Count.Should().Be(1);
    }

    #endregion

    #region GetByEntityIdAsync Tests

    [Fact]
    public async Task GetByEntityIdAsync_ExistingEntity_ShouldReturnMatchingEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        await _store.RecordAsync(CreateEntry("entity-1", "RecordDeleted"));
        await _store.RecordAsync(CreateEntry("entity-2", "RecordTracked"));

        // Act
        var result = await _store.GetByEntityIdAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(e => e.EntityId == "entity-1");
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

    [Fact]
    public async Task GetByEntityIdAsync_ShouldReturnInDescendingOccurredAtUtcOrder()
    {
        // Arrange
        var time1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var time3 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Record out of order
        await _store.RecordAsync(CreateEntry("entity-1", "RecordDeleted", occurredAtUtc: time3));
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked", occurredAtUtc: time1));
        await _store.RecordAsync(CreateEntry("entity-1", "EnforcementExecuted", occurredAtUtc: time2));

        // Act
        var result = await _store.GetByEntityIdAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(3);
        list[0].OccurredAtUtc.Should().Be(time3);
        list[1].OccurredAtUtc.Should().Be(time2);
        list[2].OccurredAtUtc.Should().Be(time1);
    }

    [Fact]
    public async Task GetByEntityIdAsync_MultipleEntities_ShouldIsolateResults()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        await _store.RecordAsync(CreateEntry("entity-1", "RecordDeleted"));
        await _store.RecordAsync(CreateEntry("entity-2", "RecordTracked"));

        // Act
        var result1 = await _store.GetByEntityIdAsync("entity-1");
        var result2 = await _store.GetByEntityIdAsync("entity-2");

        // Assert
        result1.RightAsEnumerable().First().Should().HaveCount(2);
        result2.RightAsEnumerable().First().Should().HaveCount(1);
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
    public async Task GetAllAsync_WithEntries_ShouldReturnAll()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        await _store.RecordAsync(CreateEntry("entity-2", "RecordTracked"));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnInDescendingOccurredAtUtcOrder()
    {
        // Arrange
        var time1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var time3 = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);

        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked", occurredAtUtc: time1));
        await _store.RecordAsync(CreateEntry("entity-2", "RecordTracked", occurredAtUtc: time2));
        await _store.RecordAsync(CreateEntry("entity-3", "RecordTracked", occurredAtUtc: time3));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(3);
        list[0].OccurredAtUtc.Should().Be(time2);
        list[1].OccurredAtUtc.Should().Be(time3);
        list[2].OccurredAtUtc.Should().Be(time1);
    }

    #endregion

    #region Testing Helpers Tests

    [Fact]
    public void Count_EmptyStore_ShouldReturnZero()
    {
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Count_AfterRecording_ShouldReflectStoredEntries()
    {
        // Arrange & Act
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        await _store.RecordAsync(CreateEntry("entity-2", "RecordTracked"));

        // Assert
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetAllEntries_ShouldReturnAllEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        await _store.RecordAsync(CreateEntry("entity-2", "RecordTracked"));

        // Act
        var entries = _store.GetAllEntries();

        // Assert
        entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllEntries_ShouldReturnInDescendingOccurredAtUtcOrder()
    {
        // Arrange
        var time1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked", occurredAtUtc: time1));
        await _store.RecordAsync(CreateEntry("entity-2", "LegalHoldApplied", occurredAtUtc: time2));

        // Act
        var entries = _store.GetAllEntries();

        // Assert
        entries[0].OccurredAtUtc.Should().Be(time2);
        entries[1].OccurredAtUtc.Should().Be(time1);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));
        _store.Count.Should().Be(1);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    [Fact]
    public async Task Clear_AfterClear_GetAllEntries_ShouldReturnEmpty()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("entity-1", "RecordTracked"));

        // Act
        _store.Clear();
        var entries = _store.GetAllEntries();

        // Assert
        entries.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private static RetentionAuditEntry CreateEntry(
        string? entityId,
        string action,
        string? dataCategory = null,
        string? detail = null,
        DateTimeOffset? occurredAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Action = action,
            EntityId = entityId,
            DataCategory = dataCategory,
            Detail = detail,
            OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow
        };

    #endregion
}
