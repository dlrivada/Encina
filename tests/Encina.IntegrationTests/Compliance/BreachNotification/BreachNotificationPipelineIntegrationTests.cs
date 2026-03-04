using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Detection.Rules;
using Encina.Compliance.BreachNotification.InMemory;
using Encina.Compliance.BreachNotification.Model;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.BreachNotification;

/// <summary>
/// Integration tests for the full Encina.Compliance.BreachNotification pipeline.
/// Tests DI registration, options configuration, built-in detection rules,
/// full breach lifecycle, and concurrent access safety.
/// No Docker containers needed - all operations use in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BreachNotificationPipelineIntegrationTests
{
    private static readonly int[] DefaultAlertHours = [48, 24, 12, 6, 1];
    #region DI Registration

    [Fact]
    public void AddEncinaBreachNotification_RegistersIBreachRecordStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IBreachRecordStore>().Should().NotBeNull();
        provider.GetService<IBreachRecordStore>().Should().BeOfType<InMemoryBreachRecordStore>();
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersIBreachAuditStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IBreachAuditStore>().Should().NotBeNull();
        provider.GetService<IBreachAuditStore>().Should().BeOfType<InMemoryBreachAuditStore>();
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersIBreachDetector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IBreachDetector>().Should().NotBeNull();
        provider.GetService<IBreachDetector>().Should().BeOfType<DefaultBreachDetector>();
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersIBreachNotifier()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IBreachNotifier>().Should().NotBeNull();
        provider.GetService<IBreachNotifier>().Should().BeOfType<DefaultBreachNotifier>();
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersIBreachHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert — IBreachHandler is Scoped, so resolve within a scope
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IBreachHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IBreachHandler>().Should().BeOfType<DefaultBreachHandler>();
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersBreachNotificationOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<BreachNotificationOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaBreachNotification_DefaultOptions_HaveCorrectValues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<BreachNotificationOptions>>().Value;
        options.EnforcementMode.Should().Be(BreachDetectionEnforcementMode.Warn);
        options.PublishNotifications.Should().BeTrue();
        options.TrackAuditTrail.Should().BeTrue();
        options.NotificationDeadlineHours.Should().Be(72);
        options.AlertAtHoursRemaining.Should().BeEquivalentTo(DefaultAlertHours);
        options.AutoNotifyOnHighSeverity.Should().BeFalse();
        options.PhasedReportingEnabled.Should().BeTrue();
        options.SubjectNotificationSeverityThreshold.Should().Be(BreachSeverity.High);
        options.EnableDeadlineMonitoring.Should().BeFalse();
        options.DeadlineCheckInterval.Should().Be(TimeSpan.FromMinutes(15));
        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaBreachNotification_CustomOptions_AreRespected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification(options =>
        {
            options.EnforcementMode = BreachDetectionEnforcementMode.Block;
            options.NotificationDeadlineHours = 48;
            options.AlertAtHoursRemaining = [24, 12, 6, 1];
            options.PublishNotifications = false;
            options.TrackAuditTrail = false;
            options.AutoNotifyOnHighSeverity = true;
            options.PhasedReportingEnabled = false;
            options.SubjectNotificationSeverityThreshold = BreachSeverity.Critical;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<BreachNotificationOptions>>().Value;
        options.EnforcementMode.Should().Be(BreachDetectionEnforcementMode.Block);
        options.NotificationDeadlineHours.Should().Be(48);
        options.PublishNotifications.Should().BeFalse();
        options.TrackAuditTrail.Should().BeFalse();
        options.AutoNotifyOnHighSeverity.Should().BeTrue();
        options.PhasedReportingEnabled.Should().BeFalse();
        options.SubjectNotificationSeverityThreshold.Should().Be(BreachSeverity.Critical);
    }

    [Fact]
    public void AddEncinaBreachNotification_WithConfigure_CallsCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var callbackInvoked = false;

        // Act
        services.AddEncinaBreachNotification(options =>
        {
            callbackInvoked = true;
            options.EnforcementMode = BreachDetectionEnforcementMode.Disabled;
        });
        var provider = services.BuildServiceProvider();

        // Force options resolution to trigger the configure callback
        var options = provider.GetRequiredService<IOptions<BreachNotificationOptions>>().Value;

        // Assert
        callbackInvoked.Should().BeTrue();
        options.EnforcementMode.Should().Be(BreachDetectionEnforcementMode.Disabled);
    }

    #endregion

    #region Built-in Detection Rules

    [Fact]
    public void AddEncinaBreachNotification_RegistersBuiltInDetectionRules()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaBreachNotification();
        var provider = services.BuildServiceProvider();

        // Assert — 4 built-in rules registered as IBreachDetectionRule enumerable
        var rules = provider.GetServices<IBreachDetectionRule>().ToList();
        rules.Should().HaveCount(4);
        rules.Should().ContainSingle(r => r is UnauthorizedAccessRule);
        rules.Should().ContainSingle(r => r is MassDataExfiltrationRule);
        rules.Should().ContainSingle(r => r is PrivilegeEscalationRule);
        rules.Should().ContainSingle(r => r is AnomalousQueryPatternRule);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task BreachHandler_FullLifecycle_DetectNotifyPhasedReportResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaBreachNotification(options =>
        {
            options.TrackAuditTrail = true;
            options.PublishNotifications = false; // No IEncina registered, disable to avoid noise
        });
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBreachHandler>();
        var recordStore = provider.GetRequiredService<IBreachRecordStore>();
        var auditStore = provider.GetRequiredService<IBreachAuditStore>();

        var securityEvent = SecurityEvent.Create(
            eventType: SecurityEventType.UnauthorizedAccess,
            source: "IntegrationTest",
            description: "Multiple failed login attempts from suspicious IP",
            occurredAtUtc: DateTimeOffset.UtcNow);

        var potentialBreach = new PotentialBreach
        {
            DetectionRuleName = "UnauthorizedAccess",
            Severity = BreachSeverity.High,
            Description = "Unauthorized access detected during integration test",
            SecurityEvent = securityEvent,
            DetectedAtUtc = securityEvent.OccurredAtUtc,
            RecommendedActions = ["Lock affected accounts", "Rotate credentials"]
        };

        // Step 1: Handle detected breach — creates formal record
        var handleResult = await handler.HandleDetectedBreachAsync(potentialBreach);
        handleResult.IsRight.Should().BeTrue("breach handling should succeed");

        var breachRecord = handleResult.Match(Right: r => r, Left: _ => null!);
        breachRecord.Should().NotBeNull();
        breachRecord.Id.Should().NotBeNullOrEmpty();
        breachRecord.Status.Should().Be(BreachStatus.Detected);
        breachRecord.Severity.Should().Be(BreachSeverity.High);
        breachRecord.NotificationDeadlineUtc.Should().Be(securityEvent.OccurredAtUtc.AddHours(72));

        var breachId = breachRecord.Id;

        // Verify the record was persisted
        var getResult = await recordStore.GetBreachAsync(breachId);
        getResult.IsRight.Should().BeTrue();
        var storedOption = getResult.Match(Right: opt => opt, Left: _ => default);
        storedOption.IsSome.Should().BeTrue("breach record should be in the store after handling");

        // Step 2: Notify supervisory authority (Article 33)
        var notifyResult = await handler.NotifyAuthorityAsync(breachId);
        notifyResult.IsRight.Should().BeTrue("authority notification should succeed");

        var notificationResult = notifyResult.Match(Right: r => r, Left: _ => null!);
        notificationResult.Should().NotBeNull();
        notificationResult.Outcome.Should().Be(NotificationOutcome.Sent);
        notificationResult.BreachId.Should().Be(breachId);

        // Verify status updated to AuthorityNotified
        var afterNotify = await recordStore.GetBreachAsync(breachId);
        var afterNotifyRecord = afterNotify
            .Match(Right: opt => opt, Left: _ => default)
            .Match(Some: r => r, None: () => null!);
        afterNotifyRecord.Status.Should().Be(BreachStatus.AuthorityNotified);
        afterNotifyRecord.NotifiedAuthorityAtUtc.Should().NotBeNull("authority notification timestamp should be set");

        // Step 3: Add a phased report (Article 33(4))
        var phasedReportResult = await handler.AddPhasedReportAsync(
            breachId,
            "Additional investigation reveals 5000 records affected. Root cause: SQL injection vulnerability.",
            userId: "security-analyst@company.com");
        phasedReportResult.IsRight.Should().BeTrue("AddPhasedReportAsync should succeed");

        var phasedReport = phasedReportResult.Match(Right: r => r, Left: _ => null!);
        phasedReport.Should().NotBeNull();
        phasedReport.BreachId.Should().Be(breachId);
        phasedReport.ReportNumber.Should().Be(1);
        phasedReport.SubmittedByUserId.Should().Be("security-analyst@company.com");

        // Verify the phased report is attached to the breach record
        var afterPhased = await recordStore.GetBreachAsync(breachId);
        var afterPhasedRecord = afterPhased
            .Match(Right: opt => opt, Left: _ => default)
            .Match(Some: r => r, None: () => null!);
        afterPhasedRecord.PhasedReports.Should().HaveCount(1);
        afterPhasedRecord.PhasedReports[0].ReportNumber.Should().Be(1);

        // Step 4: Resolve the breach
        var resolveResult = await handler.ResolveBreachAsync(
            breachId,
            "SQL injection vulnerability patched. Credentials rotated. Affected users notified.");
        resolveResult.IsRight.Should().BeTrue("breach resolution should succeed");

        // Verify the record is resolved
        var afterResolve = await recordStore.GetBreachAsync(breachId);
        var resolvedRecord = afterResolve
            .Match(Right: opt => opt, Left: _ => default)
            .Match(Some: r => r, None: () => null!);
        resolvedRecord.Status.Should().Be(BreachStatus.Resolved);
        resolvedRecord.ResolvedAtUtc.Should().NotBeNull("resolution timestamp should be set");
        resolvedRecord.ResolutionSummary.Should().Contain("SQL injection vulnerability patched");

        // Step 5: Verify audit trail was recorded throughout the lifecycle
        var auditResult = await auditStore.GetAuditTrailAsync(breachId);
        auditResult.IsRight.Should().BeTrue();
        var auditEntries = auditResult.Match(Right: entries => entries, Left: _ => []);
        auditEntries.Should().HaveCountGreaterThanOrEqualTo(4,
            "at least BreachDetected, AuthorityNotificationStarted, AuthorityNotified, PhasedReportSubmitted, BreachResolved should be recorded");

        // Verify audit entries contain expected lifecycle actions
        var auditActions = auditEntries.Select(e => e.Action).ToList();
        auditActions.Should().Contain("BreachDetected");
        auditActions.Should().Contain("AuthorityNotificationStarted");
        auditActions.Should().Contain("AuthorityNotified");
        auditActions.Should().Contain("PhasedReportSubmitted");
        auditActions.Should().Contain("BreachResolved");
    }

    [Fact]
    public async Task HandleDetectedBreach_SetsCorrect72HourDeadline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaBreachNotification(options =>
        {
            options.TrackAuditTrail = false;
            options.PublishNotifications = false;
        });
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBreachHandler>();

        var eventTime = DateTimeOffset.UtcNow;
        var securityEvent = SecurityEvent.Create(
            eventType: SecurityEventType.DataExfiltration,
            source: "IntegrationTest",
            description: "Bulk data export detected",
            occurredAtUtc: eventTime);

        var potentialBreach = new PotentialBreach
        {
            DetectionRuleName = "MassDataExfiltrationRule",
            Severity = BreachSeverity.Critical,
            Description = "Bulk data export exceeding 100MB threshold",
            SecurityEvent = securityEvent,
            DetectedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = await handler.HandleDetectedBreachAsync(potentialBreach);

        // Assert
        result.IsRight.Should().BeTrue();
        var record = result.Match(Right: r => r, Left: _ => null!);
        record.NotificationDeadlineUtc.Should().Be(eventTime.AddHours(72),
            "notification deadline should be 72 hours from the security event's occurrence time");
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task InMemoryStores_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaBreachNotification(options =>
        {
            options.TrackAuditTrail = false;
            options.PublishNotifications = false;
        });
        var provider = services.BuildServiceProvider();

        var recordStore = provider.GetRequiredService<IBreachRecordStore>();
        var recordCount = 50;

        // Act: Create many breach records concurrently
        var tasks = Enumerable.Range(0, recordCount).Select(async i =>
        {
            var record = BreachRecord.Create(
                nature: $"Concurrent breach #{i}",
                approximateSubjectsAffected: i + 1,
                categoriesOfDataAffected: ["email"],
                dpoContactDetails: "dpo@test.com",
                likelyConsequences: "Risk assessment pending",
                measuresTaken: "Investigation in progress",
                detectedAtUtc: DateTimeOffset.UtcNow,
                severity: BreachSeverity.Medium);

            var result = await recordStore.RecordBreachAsync(record);
            return (RecordId: record.Id, Result: result);
        });

        var results = await Task.WhenAll(tasks);

        // Assert: All creations succeeded
        results.Should().AllSatisfy(r =>
            r.Result.IsRight.Should().BeTrue($"record creation for '{r.RecordId}' should succeed"));

        // Assert: All records are retrievable
        foreach (var (recordId, _) in results)
        {
            var retrieved = await recordStore.GetBreachAsync(recordId);
            retrieved.IsRight.Should().BeTrue($"record '{recordId}' should be retrievable");

            var recordOption = retrieved.Match(Right: opt => opt, Left: _ => default);
            recordOption.IsSome.Should().BeTrue($"record '{recordId}' should exist in the store");
        }

        // Assert: All record IDs are unique (no overwrite/collision)
        var recordIds = results.Select(r => r.RecordId).ToList();
        recordIds.Should().OnlyHaveUniqueItems("each concurrent write should produce a distinct record");

        // Assert: Store count matches expected
        var allResult = await recordStore.GetAllAsync();
        allResult.IsRight.Should().BeTrue();
        var allRecords = allResult.Match(Right: r => r, Left: _ => []);
        allRecords.Should().HaveCount(recordCount, "all concurrently created records should be persisted");
    }

    #endregion
}
