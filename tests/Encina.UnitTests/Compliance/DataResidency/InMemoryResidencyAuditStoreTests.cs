using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.InMemory;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataResidency;

public class InMemoryResidencyAuditStoreTests
{
    private readonly InMemoryResidencyAuditStore _store;

    public InMemoryResidencyAuditStoreTests()
    {
        _store = new InMemoryResidencyAuditStore(NullLogger<InMemoryResidencyAuditStore>.Instance);
    }

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldSucceed()
    {
        // Arrange
        var entry = ResidencyAuditEntry.Create(
            dataCategory: "personal-data",
            sourceRegion: "DE",
            action: ResidencyAction.PolicyCheck,
            outcome: ResidencyOutcome.Allowed);

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnEntriesForEntity()
    {
        // Arrange
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed, entityId: "entity-1"));
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "FR", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed, entityId: "entity-2"));
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "IT", ResidencyAction.PolicyCheck, ResidencyOutcome.Blocked, entityId: "entity-1"));

        // Act
        var result = await _store.GetByEntityAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Should().HaveCount(2),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnEntriesInRange()
    {
        // Arrange
        var entry1 = ResidencyAuditEntry.Create("data", "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed);
        var entry2 = ResidencyAuditEntry.Create("data", "FR", ResidencyAction.CrossBorderTransfer, ResidencyOutcome.Blocked);
        await _store.RecordAsync(entry1);
        await _store.RecordAsync(entry2);

        var from = DateTimeOffset.UtcNow.AddMinutes(-1);
        var to = DateTimeOffset.UtcNow.AddMinutes(1);

        // Act
        var result = await _store.GetByDateRangeAsync(from, to);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Should().HaveCount(2),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByDateRangeAsync_OutOfRange_ShouldReturnEmpty()
    {
        // Arrange
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed));

        var from = DateTimeOffset.UtcNow.AddHours(1);
        var to = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        var result = await _store.GetByDateRangeAsync(from, to);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Should().BeEmpty(),
            Left: _ => { });
    }

    [Fact]
    public async Task GetViolationsAsync_ShouldReturnOnlyBlockedEntries()
    {
        // Arrange
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed));
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "CN", ResidencyAction.CrossBorderTransfer, ResidencyOutcome.Blocked));
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "US", ResidencyAction.Violation, ResidencyOutcome.Blocked));
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "FR", ResidencyAction.PolicyCheck, ResidencyOutcome.Warning));

        // Act
        var result = await _store.GetViolationsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Should().HaveCount(2),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByEntityAsync_NonExistingEntity_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetByEntityAsync("unknown");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Should().BeEmpty(),
            Left: _ => { });
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "DE", ResidencyAction.PolicyCheck, ResidencyOutcome.Allowed));
        await _store.RecordAsync(ResidencyAuditEntry.Create("data", "FR", ResidencyAction.PolicyCheck, ResidencyOutcome.Blocked));

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }
}
