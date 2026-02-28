#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="InMemoryAnonymizationAuditStore"/>.
/// </summary>
public class InMemoryAnonymizationAuditStoreTests
{
    private readonly InMemoryAnonymizationAuditStore _store = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeEmpty()
    {
        // Assert
        _store.Count.Should().Be(0);
    }

    #endregion

    #region AddEntryAsync Tests

    [Fact]
    public async Task AddEntryAsync_ValidEntry_ShouldSucceed()
    {
        // Arrange
        var entry = CreateEntry();

        // Act
        var result = await _store.AddEntryAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task AddEntryAsync_MultipleEntries_ShouldIncrementCount()
    {
        // Arrange & Act
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-2"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-3"));

        // Assert
        _store.Count.Should().Be(3);
    }

    #endregion

    #region GetBySubjectIdAsync Tests

    [Fact]
    public async Task GetBySubjectIdAsync_ExistingSubject_ShouldReturnEntries()
    {
        // Arrange
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-2"));

        // Act
        var result = await _store.GetBySubjectIdAsync("subject-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Should().HaveCount(2);
        list.Should().OnlyContain(e => e.SubjectId == "subject-1");
    }

    [Fact]
    public async Task GetBySubjectIdAsync_NonExistingSubject_ShouldReturnEmpty()
    {
        // Arrange
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));

        // Act
        var result = await _store.GetBySubjectIdAsync("non-existing");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySubjectIdAsync_ShouldOnlyReturnMatchingSubject()
    {
        // Arrange
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-2"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));

        // Act
        var result = await _store.GetBySubjectIdAsync("subject-2");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Should().HaveCount(1);
        list[0].SubjectId.Should().Be("subject-2");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyStore_ShouldReturnEmpty()
    {
        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithEntries_ShouldReturnAll()
    {
        // Arrange
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-2"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-3"));

        // Act
        var result = await _store.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.Match(Right: l => l, Left: _ => []);
        list.Should().HaveCount(3);
    }

    #endregion

    #region GetAllEntries Tests

    [Fact]
    public async Task GetAllEntries_ShouldReturnSameAsGetAllAsync()
    {
        // Arrange
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-2"));

        // Act
        var syncResult = _store.GetAllEntries();
        var asyncResult = await _store.GetAllAsync();

        // Assert
        syncResult.Should().HaveCount(2);
        var asyncList = asyncResult.Match(Right: l => l, Left: _ => []);
        asyncList.Should().HaveCount(syncResult.Count);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-1"));
        await _store.AddEntryAsync(CreateEntry(subjectId: "subject-2"));
        _store.Count.Should().Be(2);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    #endregion

    #region Helpers

    private static AnonymizationAuditEntry CreateEntry(
        AnonymizationOperation operation = AnonymizationOperation.Anonymized,
        string? subjectId = "subject-1") =>
        AnonymizationAuditEntry.Create(operation, subjectId: subjectId);

    #endregion
}
