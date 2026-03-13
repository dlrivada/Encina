#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="InMemoryProcessorAuditStore"/>.
/// </summary>
public class InMemoryProcessorAuditStoreTests
{
    private readonly ILogger<InMemoryProcessorAuditStore> _logger;
    private readonly InMemoryProcessorAuditStore _store;

    public InMemoryProcessorAuditStoreTests()
    {
        _logger = NullLogger<InMemoryProcessorAuditStore>.Instance;
        _store = new InMemoryProcessorAuditStore(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new InMemoryProcessorAuditStore(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RecordAsync Tests

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldReturnRight()
    {
        // Arrange
        var entry = CreateEntry("proc-1", "Registered");

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ShouldThrowArgumentNullException()
    {
        var act = async () => await _store.RecordAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("entry");
    }

    [Fact]
    public async Task RecordAsync_MultipleEntriesForSameProcessor_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.RecordAsync(CreateEntry("proc-1", "Registered"));
        await _store.RecordAsync(CreateEntry("proc-1", "DPASigned"));
        await _store.RecordAsync(CreateEntry("proc-1", "SubProcessorAdded"));

        // Assert
        _store.Count.Should().Be(3);
        var result = await _store.GetAuditTrailAsync("proc-1");
        _ = result.Match(
            Right: entries => entries.Should().HaveCount(3),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetAuditTrailAsync Tests

    [Fact]
    public async Task GetAuditTrailAsync_ExistingProcessor_ShouldReturnEntries()
    {
        // Arrange
        var entry1 = CreateEntry("proc-1", "Registered", occurredAtUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var entry2 = CreateEntry("proc-1", "DPASigned", occurredAtUtc: new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        // Act
        var result = await _store.GetAuditTrailAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(2);
                entries[0].Action.Should().Be("Registered");
                entries[1].Action.Should().Be("DPASigned");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAuditTrailAsync_NonExistentProcessor_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAuditTrailAsync("non-existent");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: entries => entries.Should().BeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAuditTrailAsync_NullProcessorId_ShouldThrowArgumentNullException()
    {
        var act = async () => await _store.GetAuditTrailAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("processorId");
    }

    [Fact]
    public async Task GetAuditTrailAsync_ShouldReturnEntriesOrderedByTimestamp()
    {
        // Arrange - insert out of order
        var entry2 = CreateEntry("proc-1", "DPASigned", occurredAtUtc: new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        var entry1 = CreateEntry("proc-1", "Registered", occurredAtUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await _store.RecordAsync(entry2);
        await _store.RecordAsync(entry1);

        // Act
        var result = await _store.GetAuditTrailAsync("proc-1");

        // Assert
        result.Match(
            Right: entries =>
            {
                entries[0].OccurredAtUtc.Should().BeBefore(entries[1].OccurredAtUtc);
                entries[0].Action.Should().Be("Registered");
                entries[1].Action.Should().Be("DPASigned");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Clear and Count Tests

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateEntry("proc-1", "Registered"));
        await _store.RecordAsync(CreateEntry("proc-2", "DPASigned"));
        _store.Count.Should().Be(2);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    #endregion

    #region Helpers

    private static ProcessorAgreementAuditEntry CreateEntry(
        string processorId,
        string action,
        string? dpaId = null,
        DateTimeOffset? occurredAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = processorId,
            DPAId = dpaId,
            Action = action,
            Detail = $"Test detail for {action}",
            PerformedByUserId = "test-user",
            OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow
        };

    #endregion
}
