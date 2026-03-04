#pragma warning disable CA2012

using Encina.Compliance.BreachNotification.InMemory;
using Encina.Compliance.BreachNotification.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="InMemoryBreachAuditStore"/>.
/// </summary>
public class InMemoryBreachAuditStoreTests
{
    private readonly ILogger<InMemoryBreachAuditStore> _logger;
    private readonly InMemoryBreachAuditStore _sut;

    public InMemoryBreachAuditStoreTests()
    {
        _logger = Substitute.For<ILogger<InMemoryBreachAuditStore>>();
        _sut = CreateSut();
    }

    private InMemoryBreachAuditStore CreateSut() =>
        new(_logger);

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new InMemoryBreachAuditStore(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RecordAsync Tests

    [Fact]
    public async Task RecordAsync_ValidEntry_ReturnsRight()
    {
        // Arrange
        var entry = BreachAuditEntry.Create("breach-001", "BreachDetected", "Auto-detected via pipeline");

        // Act
        var result = await _sut.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_MultipleEntries_ShouldStoreAll()
    {
        // Arrange
        var entry1 = BreachAuditEntry.Create("breach-001", "BreachDetected");
        var entry2 = BreachAuditEntry.Create("breach-001", "AuthorityNotified");
        var entry3 = BreachAuditEntry.Create("breach-002", "BreachDetected");

        // Act
        await _sut.RecordAsync(entry1);
        await _sut.RecordAsync(entry2);
        await _sut.RecordAsync(entry3);

        // Assert
        _sut.Count.Should().Be(3);
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ShouldThrow()
    {
        var act = async () => await _sut.RecordAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetAuditTrailAsync Tests

    [Fact]
    public async Task GetAuditTrailAsync_ExistingBreachId_ReturnsEntries()
    {
        // Arrange
        var entry1 = BreachAuditEntry.Create("breach-001", "BreachDetected");
        var entry2 = BreachAuditEntry.Create("breach-001", "AuthorityNotified");
        var entry3 = BreachAuditEntry.Create("breach-002", "BreachDetected");

        await _sut.RecordAsync(entry1);
        await _sut.RecordAsync(entry2);
        await _sut.RecordAsync(entry3);

        // Act
        var result = await _sut.GetAuditTrailAsync("breach-001");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(2);
                entries.Should().OnlyContain(e => e.BreachId == "breach-001");
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAuditTrailAsync_NonExistingBreachId_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAuditTrailAsync("non-existent-breach");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAuditTrailAsync_MultipleEntries_ReturnsChronological()
    {
        // Arrange - Create entries with different timestamps to ensure ordering
        var earlier = BreachAuditEntry.Create("breach-001", "BreachDetected", "First action");

        // Wait briefly to ensure distinct timestamps
        await Task.Delay(10);
        var later = BreachAuditEntry.Create("breach-001", "AuthorityNotified", "Second action");

        await _sut.RecordAsync(earlier);
        await _sut.RecordAsync(later);

        // Act
        var result = await _sut.GetAuditTrailAsync("breach-001");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(2);
                // Store orders by OccurredAtUtc descending
                entries[0].OccurredAtUtc.Should().BeOnOrAfter(entries[1].OccurredAtUtc);
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAuditTrailAsync_NullBreachId_ShouldThrow()
    {
        var act = async () => await _sut.GetAuditTrailAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAuditTrailAsync_WhitespaceBreachId_ShouldThrow()
    {
        var act = async () => await _sut.GetAuditTrailAsync("   ");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Testing Utilities

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _sut.RecordAsync(BreachAuditEntry.Create("breach-001", "BreachDetected"));
        await _sut.RecordAsync(BreachAuditEntry.Create("breach-002", "AuthorityNotified"));
        _sut.Count.Should().Be(2);

        // Act
        _sut.Clear();

        // Assert
        _sut.Count.Should().Be(0);
        _sut.GetAllEntries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllEntries_ShouldReturnAll()
    {
        // Arrange
        await _sut.RecordAsync(BreachAuditEntry.Create("breach-001", "BreachDetected"));
        await _sut.RecordAsync(BreachAuditEntry.Create("breach-002", "BreachDetected"));

        // Act
        var entries = _sut.GetAllEntries();

        // Assert
        entries.Should().HaveCount(2);
    }

    #endregion
}
