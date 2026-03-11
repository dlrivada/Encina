#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="InMemoryDPIAAuditStore"/>.
/// </summary>
public class InMemoryDPIAAuditStoreTests
{
    private readonly InMemoryDPIAAuditStore _sut = new(NullLogger<InMemoryDPIAAuditStore>.Instance);

    private static DPIAAuditEntry CreateEntry(
        Guid? assessmentId = null,
        string action = "TestAction",
        DateTimeOffset? occurredAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        AssessmentId = assessmentId ?? Guid.NewGuid(),
        Action = action,
        PerformedBy = "tester",
        OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow,
        Details = "Test details",
    };

    #region RecordAuditEntryAsync Tests

    [Fact]
    public async Task RecordAuditEntryAsync_ValidEntry_ReturnsUnit()
    {
        var entry = CreateEntry();

        var result = await _sut.RecordAuditEntryAsync(entry);

        result.IsRight.Should().BeTrue();
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAuditEntryAsync_MultipleEntries_SameAssessment_Stores()
    {
        var assessmentId = Guid.NewGuid();

        await _sut.RecordAuditEntryAsync(CreateEntry(assessmentId, "Action1"));
        await _sut.RecordAuditEntryAsync(CreateEntry(assessmentId, "Action2"));

        _sut.Count.Should().Be(2);
    }

    #endregion

    #region GetAuditTrailAsync Tests

    [Fact]
    public async Task GetAuditTrailAsync_WithEntries_ReturnsOrderedByTime()
    {
        var assessmentId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow;

        await _sut.RecordAuditEntryAsync(CreateEntry(assessmentId, "Second", baseTime.AddMinutes(2)));
        await _sut.RecordAuditEntryAsync(CreateEntry(assessmentId, "First", baseTime.AddMinutes(1)));

        var result = await _sut.GetAuditTrailAsync(assessmentId);

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: trail =>
            {
                trail.Should().HaveCount(2);
                trail[0].Action.Should().Be("First");
                trail[1].Action.Should().Be("Second");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAuditTrailAsync_NoEntries_ReturnsEmptyList()
    {
        var result = await _sut.GetAuditTrailAsync(Guid.NewGuid());

        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: trail => trail.Should().BeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_RemovesAllEntries()
    {
        await _sut.RecordAuditEntryAsync(CreateEntry());
        await _sut.RecordAuditEntryAsync(CreateEntry());

        _sut.Clear();

        _sut.Count.Should().Be(0);
    }

    #endregion
}
