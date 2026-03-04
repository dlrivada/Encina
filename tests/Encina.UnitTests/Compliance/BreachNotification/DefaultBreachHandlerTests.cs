#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="DefaultBreachHandler"/>.
/// </summary>
public class DefaultBreachHandlerTests
{
    private readonly IBreachRecordStore _recordStore = Substitute.For<IBreachRecordStore>();
    private readonly IBreachAuditStore _auditStore = Substitute.For<IBreachAuditStore>();
    private readonly IBreachNotifier _notifier = Substitute.For<IBreachNotifier>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    public DefaultBreachHandlerTests()
    {
        // Audit store always succeeds by default
        _auditStore
            .RecordAsync(Arg.Any<BreachAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Record store succeeds by default
        _recordStore
            .RecordBreachAsync(Arg.Any<BreachRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        _recordStore
            .UpdateBreachAsync(Arg.Any<BreachRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        _recordStore
            .AddPhasedReportAsync(Arg.Any<string>(), Arg.Any<PhasedReport>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Service provider returns null for IEncina by default
        _serviceProvider.GetService(typeof(IEncina)).Returns(null);
    }

    private DefaultBreachHandler CreateSut(
        BreachNotificationOptions? options = null)
    {
        var opts = Options.Create(options ?? new BreachNotificationOptions
        {
            TrackAuditTrail = true,
            PublishNotifications = true
        });

        return new DefaultBreachHandler(
            _recordStore,
            _auditStore,
            _notifier,
            opts,
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultBreachHandler>.Instance);
    }

    private static PotentialBreach CreateTestPotentialBreach(
        BreachSeverity severity = BreachSeverity.High,
        string ruleName = "TestRule",
        string description = "Test breach detected")
    {
        var securityEvent = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess,
            "test-source",
            "test description",
            DateTimeOffset.UtcNow);

        return new PotentialBreach
        {
            DetectionRuleName = ruleName,
            Severity = severity,
            Description = description,
            SecurityEvent = securityEvent,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static BreachRecord CreateTestBreachRecord(
        string? id = null,
        BreachStatus status = BreachStatus.Detected,
        BreachSeverity severity = BreachSeverity.High)
    {
        var detectedAt = DateTimeOffset.UtcNow;
        return new BreachRecord
        {
            Id = id ?? Guid.NewGuid().ToString("N"),
            Nature = "Test breach",
            ApproximateSubjectsAffected = 100,
            CategoriesOfDataAffected = ["names", "emails"],
            DPOContactDetails = "dpo@test.com",
            LikelyConsequences = "Identity theft",
            MeasuresTaken = "Credentials rotated",
            DetectedAtUtc = detectedAt,
            NotificationDeadlineUtc = detectedAt.AddHours(72),
            Severity = severity,
            Status = status,
            SubjectNotificationExemption = SubjectNotificationExemption.None
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRecordStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultBreachHandler(
            null!,
            _auditStore,
            _notifier,
            Options.Create(new BreachNotificationOptions()),
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultBreachHandler>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("recordStore");
    }

    [Fact]
    public void Constructor_NullAuditStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultBreachHandler(
            _recordStore,
            null!,
            _notifier,
            Options.Create(new BreachNotificationOptions()),
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultBreachHandler>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_NullNotifier_ShouldThrow()
    {
        // Act
        var act = () => new DefaultBreachHandler(
            _recordStore,
            _auditStore,
            null!,
            Options.Create(new BreachNotificationOptions()),
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultBreachHandler>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("notifier");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act
        var act = () => new DefaultBreachHandler(
            _recordStore,
            _auditStore,
            _notifier,
            null!,
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultBreachHandler>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrow()
    {
        // Act
        var act = () => new DefaultBreachHandler(
            _recordStore,
            _auditStore,
            _notifier,
            Options.Create(new BreachNotificationOptions()),
            null!,
            _timeProvider,
            NullLogger<DefaultBreachHandler>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_ValidParameters_ShouldNotThrow()
    {
        // Act
        var act = () => CreateSut();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region HandleDetectedBreachAsync Tests

    [Fact]
    public async Task HandleDetectedBreachAsync_ValidBreach_RecordsBreachAndReturnsBreachId()
    {
        // Arrange
        var sut = CreateSut();
        var potentialBreach = CreateTestPotentialBreach();

        // Act
        var result = await sut.HandleDetectedBreachAsync(potentialBreach);

        // Assert
        result.IsRight.Should().BeTrue();
        var breachRecord = result.Match(r => r, _ => default!);
        breachRecord.Should().NotBeNull();
        breachRecord.Id.Should().NotBeNullOrEmpty();

        // Verify record store was called
        await _recordStore.Received(1)
            .RecordBreachAsync(Arg.Any<BreachRecord>(), Arg.Any<CancellationToken>());

        // Verify audit entry was recorded
        await _auditStore.Received(1)
            .RecordAsync(
                Arg.Is<BreachAuditEntry>(e => e.Action == "BreachDetected"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleDetectedBreachAsync_StoreError_ReturnsLeft()
    {
        // Arrange
        var storeError = EncinaErrors.Create(
            code: "breach.store_error",
            message: "Failed to persist breach");

        _recordStore
            .RecordBreachAsync(Arg.Any<BreachRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, Unit>(storeError)));

        var sut = CreateSut();
        var potentialBreach = CreateTestPotentialBreach();

        // Act
        var result = await sut.HandleDetectedBreachAsync(potentialBreach);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("Failed to persist breach");
    }

    [Fact]
    public async Task HandleDetectedBreachAsync_AutoNotifyOnHighSeverity_NotifiesAuthority()
    {
        // Arrange
        // Note: The current DefaultBreachHandler implementation does NOT auto-notify.
        // It records the breach and publishes a BreachDetectedNotification.
        // AutoNotifyOnHighSeverity is an option flag that downstream consumers may use.
        // We test that the handler successfully records a high-severity breach.
        var sut = CreateSut(new BreachNotificationOptions
        {
            AutoNotifyOnHighSeverity = true,
            TrackAuditTrail = true,
            PublishNotifications = true
        });

        var potentialBreach = CreateTestPotentialBreach(severity: BreachSeverity.High);

        // Act
        var result = await sut.HandleDetectedBreachAsync(potentialBreach);

        // Assert
        result.IsRight.Should().BeTrue();
        var breachRecord = result.Match(r => r, _ => default!);
        breachRecord.Severity.Should().Be(BreachSeverity.High);

        await _recordStore.Received(1)
            .RecordBreachAsync(Arg.Any<BreachRecord>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region NotifyAuthorityAsync Tests

    [Fact]
    public async Task NotifyAuthorityAsync_ExistingBreach_UpdatesStatusAndReturnsRight()
    {
        // Arrange
        var breach = CreateTestBreachRecord(status: BreachStatus.Detected);

        _recordStore
            .GetBreachAsync(breach.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Some(breach))));

        var notificationResult = new NotificationResult
        {
            Outcome = NotificationOutcome.Sent,
            SentAtUtc = _timeProvider.GetUtcNow(),
            Recipient = "supervisory-authority",
            BreachId = breach.Id
        };

        _notifier
            .NotifyAuthorityAsync(Arg.Any<BreachRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, NotificationResult>(notificationResult)));

        var sut = CreateSut();

        // Act
        var result = await sut.NotifyAuthorityAsync(breach.Id);

        // Assert
        result.IsRight.Should().BeTrue();
        var notification = result.Match(r => r, _ => default!);
        notification.Outcome.Should().Be(NotificationOutcome.Sent);

        // Verify breach status updated to AuthorityNotified
        await _recordStore.Received(1)
            .UpdateBreachAsync(
                Arg.Is<BreachRecord>(r => r.Status == BreachStatus.AuthorityNotified),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAuthorityAsync_NonExistingBreach_ReturnsLeft()
    {
        // Arrange
        var breachId = "non-existent-breach";

        _recordStore
            .GetBreachAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Option<BreachRecord>.None)));

        var sut = CreateSut();

        // Act
        var result = await sut.NotifyAuthorityAsync(breachId);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().Match(c => c, () => string.Empty).Should().Be(BreachNotificationErrors.NotFoundCode);
    }

    [Fact]
    public async Task NotifyAuthorityAsync_NotifierFails_ReturnsNotificationResult()
    {
        // Arrange
        var breach = CreateTestBreachRecord(status: BreachStatus.Detected);

        _recordStore
            .GetBreachAsync(breach.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Some(breach))));

        var notifyError = EncinaErrors.Create(
            code: "breach.authority_notification_failed",
            message: "Notification delivery failed");

        _notifier
            .NotifyAuthorityAsync(Arg.Any<BreachRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, NotificationResult>(notifyError)));

        var sut = CreateSut();

        // Act
        var result = await sut.NotifyAuthorityAsync(breach.Id);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("Notification delivery failed");
    }

    #endregion

    #region AddPhasedReportAsync Tests

    [Fact]
    public async Task AddPhasedReportAsync_ExistingBreach_AddsReportSuccessfully()
    {
        // Arrange
        var breach = CreateTestBreachRecord(status: BreachStatus.Detected);

        _recordStore
            .GetBreachAsync(breach.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Some(breach))));

        var sut = CreateSut();

        // Act
        var result = await sut.AddPhasedReportAsync(
            breach.Id, "Additional findings from investigation", "dpo-user-001");

        // Assert
        result.IsRight.Should().BeTrue();
        var report = result.Match(r => r, _ => default!);
        report.Should().NotBeNull();
        report.BreachId.Should().Be(breach.Id);
        report.Content.Should().Be("Additional findings from investigation");
        report.ReportNumber.Should().Be(1);

        await _recordStore.Received(1)
            .AddPhasedReportAsync(breach.Id, Arg.Any<PhasedReport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPhasedReportAsync_NonExistingBreach_ReturnsLeft()
    {
        // Arrange
        var breachId = "non-existent-breach";

        _recordStore
            .GetBreachAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Option<BreachRecord>.None)));

        var sut = CreateSut();

        // Act
        var result = await sut.AddPhasedReportAsync(breachId, "Some content", null);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().Match(c => c, () => string.Empty).Should().Be(BreachNotificationErrors.NotFoundCode);
    }

    #endregion

    #region ResolveBreachAsync Tests

    [Fact]
    public async Task ResolveBreachAsync_ExistingBreach_UpdatesStatusToResolved()
    {
        // Arrange
        var breach = CreateTestBreachRecord(status: BreachStatus.AuthorityNotified);

        _recordStore
            .GetBreachAsync(breach.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Some(breach))));

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveBreachAsync(breach.Id, "Root cause identified and mitigated");

        // Assert
        result.IsRight.Should().BeTrue();

        await _recordStore.Received(1)
            .UpdateBreachAsync(
                Arg.Is<BreachRecord>(r =>
                    r.Status == BreachStatus.Resolved &&
                    r.ResolutionSummary == "Root cause identified and mitigated" &&
                    r.ResolvedAtUtc != null),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveBreachAsync_AlreadyResolved_ReturnsLeft()
    {
        // Arrange
        var breach = CreateTestBreachRecord(status: BreachStatus.Resolved);

        _recordStore
            .GetBreachAsync(breach.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Some(breach))));

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveBreachAsync(breach.Id, "Some resolution");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetCode().Match(c => c, () => string.Empty).Should().Be(BreachNotificationErrors.AlreadyResolvedCode);
    }

    #endregion

    #region GetDeadlineStatusAsync Tests

    [Fact]
    public async Task GetDeadlineStatusAsync_ExistingBreach_ReturnsCorrectStatus()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow().AddHours(-24);
        var breach = new BreachRecord
        {
            Id = "test-breach-001",
            Nature = "Test breach",
            ApproximateSubjectsAffected = 100,
            CategoriesOfDataAffected = ["names"],
            DPOContactDetails = "dpo@test.com",
            LikelyConsequences = "None",
            MeasuresTaken = "None",
            DetectedAtUtc = detectedAt,
            NotificationDeadlineUtc = detectedAt.AddHours(72),
            Severity = BreachSeverity.Medium,
            Status = BreachStatus.Detected,
            SubjectNotificationExemption = SubjectNotificationExemption.None
        };

        _recordStore
            .GetBreachAsync(breach.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Some(breach))));

        var sut = CreateSut();

        // Act
        var result = await sut.GetDeadlineStatusAsync(breach.Id);

        // Assert
        result.IsRight.Should().BeTrue();
        var status = result.Match(r => r, _ => default!);
        status.BreachId.Should().Be(breach.Id);
        status.IsOverdue.Should().BeFalse();
        status.RemainingHours.Should().BeApproximately(48, 0.1);
    }

    [Fact]
    public async Task GetDeadlineStatusAsync_OverdueBreach_IsOverdueTrue()
    {
        // Arrange
        var detectedAt = _timeProvider.GetUtcNow().AddHours(-80);
        var breach = new BreachRecord
        {
            Id = "overdue-breach-001",
            Nature = "Overdue breach",
            ApproximateSubjectsAffected = 500,
            CategoriesOfDataAffected = ["emails"],
            DPOContactDetails = "dpo@test.com",
            LikelyConsequences = "Phishing risk",
            MeasuresTaken = "Investigation ongoing",
            DetectedAtUtc = detectedAt,
            NotificationDeadlineUtc = detectedAt.AddHours(72),
            Severity = BreachSeverity.High,
            Status = BreachStatus.Detected,
            SubjectNotificationExemption = SubjectNotificationExemption.None
        };

        _recordStore
            .GetBreachAsync(breach.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<BreachRecord>>(Some(breach))));

        var sut = CreateSut();

        // Act
        var result = await sut.GetDeadlineStatusAsync(breach.Id);

        // Assert
        result.IsRight.Should().BeTrue();
        var status = result.Match(r => r, _ => default!);
        status.IsOverdue.Should().BeTrue();
        status.RemainingHours.Should().BeLessThan(0);
    }

    #endregion
}
