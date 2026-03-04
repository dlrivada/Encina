#pragma warning disable CA2012

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.InMemory;
using Encina.Compliance.BreachNotification.Model;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="InMemoryBreachRecordStore"/>.
/// </summary>
public class InMemoryBreachRecordStoreTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<InMemoryBreachRecordStore> _logger;
    private readonly InMemoryBreachRecordStore _sut;

    public InMemoryBreachRecordStoreTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));
        _logger = Substitute.For<ILogger<InMemoryBreachRecordStore>>();
        _sut = CreateSut();
    }

    private InMemoryBreachRecordStore CreateSut() =>
        new(_timeProvider, _logger);

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new InMemoryBreachRecordStore(null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new InMemoryBreachRecordStore(_timeProvider, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RecordBreachAsync Tests

    [Fact]
    public async Task RecordBreachAsync_ValidBreach_ReturnsRight()
    {
        // Arrange
        var breach = CreateBreachRecord();

        // Act
        var result = await _sut.RecordBreachAsync(breach);

        // Assert
        result.IsRight.Should().BeTrue();
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordBreachAsync_DuplicateId_ReturnsLeft()
    {
        // Arrange
        var breach = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach);

        // Act
        var result = await _sut.RecordBreachAsync(breach);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.Match(
            Right: _ => Assert.Fail("Expected Left"),
            Left: error => error.GetCode().Match(
                Some: code => code.Should().Be(BreachNotificationErrors.AlreadyExistsCode),
                None: () => Assert.Fail("Expected error code")));
    }

    [Fact]
    public async Task RecordBreachAsync_NullBreach_ShouldThrow()
    {
        var act = async () => await _sut.RecordBreachAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetBreachAsync Tests

    [Fact]
    public async Task GetBreachAsync_ExistingId_ReturnsSome()
    {
        // Arrange
        var breach = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach);

        // Act
        var result = await _sut.GetBreachAsync(breach.Id);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: option => option.Match(
                Some: found => found.Id.Should().Be(breach.Id),
                None: () => Assert.Fail("Expected Some")),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetBreachAsync_NonExistingId_ReturnsNone()
    {
        // Act
        var result = await _sut.GetBreachAsync("non-existent-id");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: option => option.IsNone.Should().BeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetBreachAsync_NullId_ShouldThrow()
    {
        var act = async () => await _sut.GetBreachAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetBreachAsync_WhitespaceId_ShouldThrow()
    {
        var act = async () => await _sut.GetBreachAsync("   ");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region UpdateBreachAsync Tests

    [Fact]
    public async Task UpdateBreachAsync_ExistingBreach_ReturnsRight()
    {
        // Arrange
        var breach = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach);
        var updated = breach with { Status = BreachStatus.Investigating };

        // Act
        var result = await _sut.UpdateBreachAsync(updated);

        // Assert
        result.IsRight.Should().BeTrue();

        var retrieved = await _sut.GetBreachAsync(breach.Id);
        retrieved.Match(
            Right: option => option.Match(
                Some: found => found.Status.Should().Be(BreachStatus.Investigating),
                None: () => Assert.Fail("Expected Some")),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task UpdateBreachAsync_NonExistingBreach_ReturnsLeft()
    {
        // Arrange
        var breach = CreateBreachRecord();

        // Act
        var result = await _sut.UpdateBreachAsync(breach);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.Match(
            Right: _ => Assert.Fail("Expected Left"),
            Left: error => error.GetCode().Match(
                Some: code => code.Should().Be(BreachNotificationErrors.NotFoundCode),
                None: () => Assert.Fail("Expected error code")));
    }

    [Fact]
    public async Task UpdateBreachAsync_NullBreach_ShouldThrow()
    {
        var act = async () => await _sut.UpdateBreachAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetBreachesByStatusAsync Tests

    [Fact]
    public async Task GetBreachesByStatusAsync_MatchingStatus_ReturnsMatchingBreaches()
    {
        // Arrange
        var breach1 = CreateBreachRecord();
        var breach2 = CreateBreachRecord();
        var breach3 = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach1);
        await _sut.RecordBreachAsync(breach2);
        await _sut.RecordBreachAsync(breach3);

        // Update one to Investigating
        await _sut.UpdateBreachAsync(breach2 with { Status = BreachStatus.Investigating });

        // Act
        var result = await _sut.GetBreachesByStatusAsync(BreachStatus.Detected);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches => breaches.Should().HaveCount(2),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetBreachesByStatusAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var breach = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach);

        // Act
        var result = await _sut.GetBreachesByStatusAsync(BreachStatus.Resolved);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches => breaches.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    #endregion

    #region GetOverdueBreachesAsync Tests

    [Fact]
    public async Task GetOverdueBreachesAsync_WithOverdueBreaches_ReturnsOverdue()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);

        // Advance time past the 72-hour deadline
        _timeProvider.Advance(TimeSpan.FromHours(73));

        // Act
        var result = await _sut.GetOverdueBreachesAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches =>
            {
                breaches.Should().ContainSingle();
                breaches[0].Id.Should().Be(breach.Id);
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetOverdueBreachesAsync_NoOverdue_ReturnsEmpty()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);

        // Advance time but stay within deadline
        _timeProvider.Advance(TimeSpan.FromHours(24));

        // Act
        var result = await _sut.GetOverdueBreachesAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches => breaches.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetOverdueBreachesAsync_AuthorityAlreadyNotified_ShouldNotReturn()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);

        // Notify authority, then advance past deadline
        var notified = breach with { NotifiedAuthorityAtUtc = _timeProvider.GetUtcNow() };
        await _sut.UpdateBreachAsync(notified);
        _timeProvider.Advance(TimeSpan.FromHours(73));

        // Act
        var result = await _sut.GetOverdueBreachesAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches => breaches.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetOverdueBreachesAsync_ResolvedBreach_ShouldNotReturn()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);

        // Resolve the breach, then advance past deadline
        var resolved = breach with { Status = BreachStatus.Resolved };
        await _sut.UpdateBreachAsync(resolved);
        _timeProvider.Advance(TimeSpan.FromHours(73));

        // Act
        var result = await _sut.GetOverdueBreachesAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches => breaches.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    #endregion

    #region AddPhasedReportAsync Tests

    [Fact]
    public async Task AddPhasedReportAsync_ExistingBreach_ReturnsRight()
    {
        // Arrange
        var breach = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach);
        var report = PhasedReport.Create(
            breach.Id, 1, "Additional information discovered", _timeProvider.GetUtcNow());

        // Act
        var result = await _sut.AddPhasedReportAsync(breach.Id, report);

        // Assert
        result.IsRight.Should().BeTrue();

        var retrieved = await _sut.GetBreachAsync(breach.Id);
        retrieved.Match(
            Right: option => option.Match(
                Some: found => found.PhasedReports.Should().ContainSingle()
                    .Which.ReportNumber.Should().Be(1),
                None: () => Assert.Fail("Expected Some")),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AddPhasedReportAsync_NonExistingBreach_ReturnsLeft()
    {
        // Arrange
        var report = PhasedReport.Create(
            "non-existent-breach", 1, "Report content", _timeProvider.GetUtcNow());

        // Act
        var result = await _sut.AddPhasedReportAsync("non-existent-breach", report);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.Match(
            Right: _ => Assert.Fail("Expected Left"),
            Left: error => error.GetCode().Match(
                Some: code => code.Should().Be(BreachNotificationErrors.NotFoundCode),
                None: () => Assert.Fail("Expected error code")));
    }

    [Fact]
    public async Task AddPhasedReportAsync_MultipleReports_ShouldAppend()
    {
        // Arrange
        var breach = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach);

        var report1 = PhasedReport.Create(breach.Id, 1, "Phase 1", _timeProvider.GetUtcNow());
        var report2 = PhasedReport.Create(breach.Id, 2, "Phase 2", _timeProvider.GetUtcNow());

        // Act
        await _sut.AddPhasedReportAsync(breach.Id, report1);
        await _sut.AddPhasedReportAsync(breach.Id, report2);

        // Assert
        var retrieved = await _sut.GetBreachAsync(breach.Id);
        retrieved.Match(
            Right: option => option.Match(
                Some: found => found.PhasedReports.Should().HaveCount(2),
                None: () => Assert.Fail("Expected Some")),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AddPhasedReportAsync_NullBreachId_ShouldThrow()
    {
        var report = PhasedReport.Create("breach-001", 1, "Content", DateTimeOffset.UtcNow);
        var act = async () => await _sut.AddPhasedReportAsync(null!, report);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddPhasedReportAsync_NullReport_ShouldThrow()
    {
        var act = async () => await _sut.AddPhasedReportAsync("breach-001", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllBreaches()
    {
        // Arrange
        var breach1 = CreateBreachRecord();
        var breach2 = CreateBreachRecord();
        var breach3 = CreateBreachRecord();
        await _sut.RecordBreachAsync(breach1);
        await _sut.RecordBreachAsync(breach2);
        await _sut.RecordBreachAsync(breach3);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches => breaches.Should().HaveCount(3),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: breaches => breaches.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    #endregion

    #region GetApproachingDeadlineAsync Tests

    [Fact]
    public async Task GetApproachingDeadlineAsync_WithApproaching_ReturnsDeadlineStatus()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);

        // Advance 48 hours so 24 hours remain (within 48-hour threshold)
        _timeProvider.Advance(TimeSpan.FromHours(48));

        // Act
        var result = await _sut.GetApproachingDeadlineAsync(48);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: statuses =>
            {
                statuses.Should().ContainSingle();
                var status = statuses[0];
                status.BreachId.Should().Be(breach.Id);
                status.RemainingHours.Should().BeApproximately(24.0, 0.01);
                status.IsOverdue.Should().BeFalse();
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetApproachingDeadlineAsync_NotApproaching_ReturnsEmpty()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);

        // Only 1 hour has passed, 71 hours remain -- not within 12-hour threshold
        _timeProvider.Advance(TimeSpan.FromHours(1));

        // Act
        var result = await _sut.GetApproachingDeadlineAsync(12);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: statuses => statuses.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetApproachingDeadlineAsync_OverdueBreach_ShouldInclude()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);

        // Advance past deadline
        _timeProvider.Advance(TimeSpan.FromHours(80));

        // Act
        var result = await _sut.GetApproachingDeadlineAsync(24);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: statuses =>
            {
                statuses.Should().ContainSingle();
                statuses[0].IsOverdue.Should().BeTrue();
                statuses[0].RemainingHours.Should().BeNegative();
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetApproachingDeadlineAsync_AuthorityAlreadyNotified_ShouldExclude()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow();
        var breach = CreateBreachRecord(detectedAtUtc: detectedAt);
        await _sut.RecordBreachAsync(breach);
        var notified = breach with { NotifiedAuthorityAtUtc = _timeProvider.GetUtcNow() };
        await _sut.UpdateBreachAsync(notified);

        _timeProvider.Advance(TimeSpan.FromHours(70));

        // Act
        var result = await _sut.GetApproachingDeadlineAsync(48);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: statuses => statuses.Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    #endregion

    #region Testing Utilities

    [Fact]
    public async Task Clear_ShouldRemoveAllRecords()
    {
        // Arrange
        await _sut.RecordBreachAsync(CreateBreachRecord());
        await _sut.RecordBreachAsync(CreateBreachRecord());
        _sut.Count.Should().Be(2);

        // Act
        _sut.Clear();

        // Assert
        _sut.Count.Should().Be(0);
        _sut.GetAllRecords().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRecords_ShouldReturnAll()
    {
        // Arrange
        await _sut.RecordBreachAsync(CreateBreachRecord());
        await _sut.RecordBreachAsync(CreateBreachRecord());

        // Act
        var records = _sut.GetAllRecords();

        // Assert
        records.Should().HaveCount(2);
    }

    #endregion

    #region Helpers

    private BreachRecord CreateBreachRecord(DateTimeOffset? detectedAtUtc = null) =>
        BreachRecord.Create(
            nature: "Unauthorized database access",
            approximateSubjectsAffected: 1000,
            categoriesOfDataAffected: ["email", "name", "address"],
            dpoContactDetails: "dpo@company.com",
            likelyConsequences: "Identity theft risk",
            measuresTaken: "Accounts locked, passwords reset",
            detectedAtUtc: detectedAtUtc ?? _timeProvider.GetUtcNow(),
            severity: BreachSeverity.High);

    #endregion
}
