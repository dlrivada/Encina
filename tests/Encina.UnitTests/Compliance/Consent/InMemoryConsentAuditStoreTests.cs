using Encina.Compliance.Consent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="InMemoryConsentAuditStore"/>.
/// </summary>
public class InMemoryConsentAuditStoreTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<InMemoryConsentAuditStore> _logger;
    private readonly InMemoryConsentAuditStore _store;

    public InMemoryConsentAuditStoreTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 23, 12, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<InMemoryConsentAuditStore>>();
        _store = new InMemoryConsentAuditStore(_timeProvider, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new InMemoryConsentAuditStore(null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new InMemoryConsentAuditStore(_timeProvider, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RecordAsync Tests

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldSucceed()
    {
        // Arrange
        var entry = CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Granted);

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_DuplicateId_ShouldOverwrite()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entry1 = CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Granted, id);
        var entry2 = CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Withdrawn, id);

        // Act
        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Assert
        _store.Count.Should().Be(1);
        var entries = _store.GetAllEntries();
        entries.Should().HaveCount(1);
        entries[0].Action.Should().Be(ConsentAuditAction.Withdrawn);
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ShouldThrow()
    {
        var act = async () => await _store.RecordAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetAuditTrailAsync Tests

    [Fact]
    public async Task GetAuditTrailAsync_BySubjectId_ShouldReturnOnlyMatchingEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Granted));
        await _store.RecordAsync(CreateEntry("user-1", ConsentPurposes.Analytics, ConsentAuditAction.Granted));
        await _store.RecordAsync(CreateEntry("user-2", ConsentPurposes.Marketing, ConsentAuditAction.Granted));

        // Act
        var result = await _store.GetAuditTrailAsync("user-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var entries = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(e => e.SubjectId == "user-1");
    }

    [Fact]
    public async Task GetAuditTrailAsync_BySubjectIdAndPurpose_ShouldFilterBoth()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Granted));
        await _store.RecordAsync(CreateEntry("user-1", ConsentPurposes.Analytics, ConsentAuditAction.Granted));

        // Act
        var result = await _store.GetAuditTrailAsync("user-1", ConsentPurposes.Marketing);

        // Assert
        result.IsRight.Should().BeTrue();
        var entries = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        entries.Should().HaveCount(1);
        entries[0].Purpose.Should().Be(ConsentPurposes.Marketing);
    }

    [Fact]
    public async Task GetAuditTrailAsync_ShouldOrderByOccurredAtDescending()
    {
        // Arrange
        var entry1 = CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Granted);
        _timeProvider.Advance(TimeSpan.FromHours(1));
        var entry2 = CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Withdrawn);

        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Act
        var result = await _store.GetAuditTrailAsync("user-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var entries = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        entries.Should().HaveCount(2);
        entries[0].OccurredAtUtc.Should().BeAfter(entries[1].OccurredAtUtc);
    }

    [Fact]
    public async Task GetAuditTrailAsync_NoEntries_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAuditTrailAsync("non-existent");

        // Assert
        result.IsRight.Should().BeTrue();
        var entries = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditTrailAsync_NullSubjectId_ShouldThrow()
    {
        var act = async () => await _store.GetAuditTrailAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAuditTrailAsync_PurposeFilter_CaseInsensitive()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("user-1", "Marketing", ConsentAuditAction.Granted));

        // Act
        var result = await _store.GetAuditTrailAsync("user-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        var entries = result.Match(Right: r => r, Left: _ => throw new InvalidOperationException());
        entries.Should().HaveCount(1);
    }

    #endregion

    #region Testing Utilities

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Granted));
        await _store.RecordAsync(CreateEntry("user-2", ConsentPurposes.Analytics, ConsentAuditAction.Granted));
        _store.Count.Should().Be(2);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
        _store.GetAllEntries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllEntries_ShouldReturnAll()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("user-1", ConsentPurposes.Marketing, ConsentAuditAction.Granted));
        await _store.RecordAsync(CreateEntry("user-2", ConsentPurposes.Analytics, ConsentAuditAction.Withdrawn));

        // Act
        var entries = _store.GetAllEntries();

        // Assert
        entries.Should().HaveCount(2);
    }

    #endregion

    #region Helpers

    private ConsentAuditEntry CreateEntry(
        string subjectId,
        string purpose,
        ConsentAuditAction action,
        Guid? id = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Action = action,
            OccurredAtUtc = _timeProvider.GetUtcNow(),
            PerformedBy = subjectId,
            Metadata = new Dictionary<string, object?>()
        };

    #endregion
}
