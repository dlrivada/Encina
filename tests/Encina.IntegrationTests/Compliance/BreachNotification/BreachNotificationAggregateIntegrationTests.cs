using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Events;
using Encina.Compliance.BreachNotification.Model;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.BreachNotification;

/// <summary>
/// Integration tests for breach notification aggregates persisted via Marten against real PostgreSQL.
/// Verifies event store persistence, aggregate loading, state reconstruction, and full lifecycle transitions
/// through the GDPR Article 33/34 breach notification workflow.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class BreachNotificationAggregateIntegrationTests
{
    private readonly MartenFixture _fixture;

    public BreachNotificationAggregateIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    private MartenAggregateRepository<T> CreateRepository<T>() where T : class, IAggregate
    {
        var session = _fixture.Store!.LightweightSession();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        var logger = NullLoggerFactory.Instance.CreateLogger<MartenAggregateRepository<T>>();
        var options = Options.Create(new EncinaMartenOptions());
        return new MartenAggregateRepository<T>(
            session,
            requestContext,
            (Microsoft.Extensions.Logging.ILogger<MartenAggregateRepository<T>>)logger,
            options);
    }

    #region Aggregate Create and Load

    [Fact]
    public async Task Breach_CreateAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var detectedAt = DateTimeOffset.UtcNow;
        var aggregate = BreachAggregate.Detect(
            id, "unauthorized access", BreachSeverity.High, "UnauthorizedAccessRule",
            500, "Mass unauthorized access to customer database",
            "security-system", detectedAt, "tenant-1", "compliance");

        // Act
        var createResult = await repo.CreateAsync(aggregate);

        // Assert — creation succeeds
        createResult.IsRight.ShouldBeTrue();

        // Act — load from event store
        var loadRepo = CreateRepository<BreachAggregate>();
        var loadResult = await loadRepo.LoadAsync(id);

        // Assert — state reconstructed from events
        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.Nature.ShouldBe("unauthorized access");
            loaded.Severity.ShouldBe(BreachSeverity.High);
            loaded.EstimatedAffectedSubjects.ShouldBe(500);
            loaded.Description.ShouldBe("Mass unauthorized access to customer database");
            loaded.Status.ShouldBe(BreachStatus.Detected);
            loaded.DetectedAtUtc.ShouldBe(detectedAt);
            loaded.DeadlineUtc.ShouldBe(detectedAt.AddHours(72));
            loaded.TenantId.ShouldBe("tenant-1");
            loaded.ModuleId.ShouldBe("compliance");
        });
    }

    [Fact]
    public async Task Breach_FullLifecycle_PersistsAllStateTransitions()
    {
        // Arrange — detect breach
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var detectedAt = DateTimeOffset.UtcNow;
        var aggregate = BreachAggregate.Detect(
            id, "data exfiltration", BreachSeverity.Medium, "MassDataExfiltrationRule",
            1000, "Bulk data export detected", "security-bot", detectedAt);
        await repo.CreateAsync(aggregate);

        // Act — assess breach
        var loadRepo1 = CreateRepository<BreachAggregate>();
        var loaded1 = (await loadRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.Assess(BreachSeverity.Critical, 5000, "Scope expanded to 5K subjects", "assessor-1", DateTimeOffset.UtcNow);
        await loadRepo1.SaveAsync(loaded1);

        // Act — report to DPA
        var loadRepo2 = CreateRepository<BreachAggregate>();
        var loaded2 = (await loadRepo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.ReportToDPA("AEPD", "contact@aepd.es", "Full breach notification report", "dpo-1", DateTimeOffset.UtcNow);
        await loadRepo2.SaveAsync(loaded2);

        // Act — notify subjects
        var loadRepo3 = CreateRepository<BreachAggregate>();
        var loaded3 = (await loadRepo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded3.NotifySubjects(5000, "email", SubjectNotificationExemption.None, "comms-1", DateTimeOffset.UtcNow);
        await loadRepo3.SaveAsync(loaded3);

        // Act — contain breach
        var loadRepo4 = CreateRepository<BreachAggregate>();
        var loaded4 = (await loadRepo4.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded4.Contain("Revoked all API keys, rotated credentials", "ops-1", DateTimeOffset.UtcNow);
        await loadRepo4.SaveAsync(loaded4);

        // Act — close breach
        var loadRepo5 = CreateRepository<BreachAggregate>();
        var loaded5 = (await loadRepo5.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded5.Close("Root cause: compromised API key. Remediation: key rotation policy", "dpo-1", DateTimeOffset.UtcNow);
        await loadRepo5.SaveAsync(loaded5);

        // Assert — final state from fresh load
        var verifyRepo = CreateRepository<BreachAggregate>();
        var finalResult = await verifyRepo.LoadAsync(id);

        finalResult.IsRight.ShouldBeTrue();
        finalResult.IfRight(final =>
        {
            final.Status.ShouldBe(BreachStatus.Closed);
            final.Severity.ShouldBe(BreachSeverity.Critical);
            final.EstimatedAffectedSubjects.ShouldBe(5000);
            final.AuthorityName.ShouldBe("AEPD");
            final.SubjectCount.ShouldBe(5000);
            final.AssessedAtUtc.ShouldNotBeNull();
            final.ReportedToDPAAtUtc.ShouldNotBeNull();
            final.NotifiedSubjectsAtUtc.ShouldNotBeNull();
            final.ContainedAtUtc.ShouldNotBeNull();
            final.ClosedAtUtc.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Breach_PhasedReports_AccumulateCorrectly()
    {
        // Arrange
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var aggregate = BreachAggregate.Detect(
            id, "privilege escalation", BreachSeverity.High, "PrivilegeEscalationRule",
            200, "Admin escalation detected", "sec-system", DateTimeOffset.UtcNow);
        aggregate.Assess(BreachSeverity.High, 200, "Confirmed 200 accounts compromised", "assessor-1", DateTimeOffset.UtcNow);
        aggregate.ReportToDPA("ICO", "ico@ico.org.uk", "Initial report", "dpo-1", DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — add 3 phased reports
        for (var phase = 1; phase <= 3; phase++)
        {
            var loadRepo = CreateRepository<BreachAggregate>();
            var loaded = (await loadRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
            loaded.AddPhasedReport($"Phase {phase}: Additional details", $"submitter-{phase}", DateTimeOffset.UtcNow);
            await loadRepo.SaveAsync(loaded);
        }

        // Assert
        var verifyRepo = CreateRepository<BreachAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.PhasedReportCount.ShouldBe(3);
        final.Status.ShouldBe(BreachStatus.AuthorityNotified);
    }

    [Fact]
    public async Task Breach_DetectAndReportDirectly_SkipsAssessment()
    {
        // Art. 33(1) allows notification before assessment is complete
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var aggregate = BreachAggregate.Detect(
            id, "ransomware", BreachSeverity.Critical, "ManualDetection",
            10000, "Ransomware encryption detected across servers",
            "incident-team", DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Report directly from Detected status (no assessment)
        var loadRepo = CreateRepository<BreachAggregate>();
        var loaded = (await loadRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.ReportToDPA("CNIL", "contact@cnil.fr", "Emergency notification", "dpo-1", DateTimeOffset.UtcNow);
        await loadRepo.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<BreachAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(BreachStatus.AuthorityNotified);
        final.AssessedAtUtc.ShouldBeNull(); // Assessment was skipped
        final.AuthorityName.ShouldBe("CNIL");
    }

    [Fact]
    public async Task Breach_72HourDeadline_CalculatedFromDetection()
    {
        // Arrange
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var detectedAt = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var aggregate = BreachAggregate.Detect(
            id, "data leak", BreachSeverity.Medium, "AnomalousQueryPatternRule",
            100, "Unusual query pattern detected", null, detectedAt, "t1", "m1");
        await repo.CreateAsync(aggregate);

        // Assert
        var loadRepo = CreateRepository<BreachAggregate>();
        var loaded = (await loadRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.DetectedAtUtc.ShouldBe(detectedAt);
        loaded.DeadlineUtc.ShouldBe(detectedAt.AddHours(72)); // March 18 at 10:00 UTC
    }

    #endregion

    #region Event Stream Audit Trail

    [Fact]
    public async Task Breach_EventStream_ContainsFullAuditTrail()
    {
        // Arrange — create a breach and exercise multiple transitions
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var aggregate = BreachAggregate.Detect(
            id, "data breach", BreachSeverity.High, "TestRule",
            1000, "Test breach for audit trail", "user-1", DateTimeOffset.UtcNow, "t1", "m1");
        aggregate.Assess(BreachSeverity.Critical, 2000, "Scope increased", "assessor-1", DateTimeOffset.UtcNow);
        aggregate.ReportToDPA("DPA", "dpa@gov.eu", "Report", "dpo-1", DateTimeOffset.UtcNow);
        aggregate.NotifySubjects(2000, "email", SubjectNotificationExemption.None, "comms-1", DateTimeOffset.UtcNow);
        aggregate.Contain("Access revoked", "ops-1", DateTimeOffset.UtcNow);
        aggregate.Close("Resolved", "dpo-1", DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — query the raw event stream
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert — event stream contains all lifecycle events in order
        events.ShouldNotBeNull();
        events.Count.ShouldBe(6);

        events[0].Data.ShouldBeOfType<BreachDetected>();
        events[1].Data.ShouldBeOfType<BreachAssessed>();
        events[2].Data.ShouldBeOfType<BreachReportedToDPA>();
        events[3].Data.ShouldBeOfType<BreachNotifiedToSubjects>();
        events[4].Data.ShouldBeOfType<BreachContained>();
        events[5].Data.ShouldBeOfType<BreachClosed>();

        // Verify event data
        var detected = (BreachDetected)events[0].Data;
        detected.Nature.ShouldBe("data breach");
        detected.Severity.ShouldBe(BreachSeverity.High);
        detected.DetectedByRule.ShouldBe("TestRule");
        detected.TenantId.ShouldBe("t1");
        detected.ModuleId.ShouldBe("m1");

        var assessed = (BreachAssessed)events[1].Data;
        assessed.UpdatedSeverity.ShouldBe(BreachSeverity.Critical);
        assessed.UpdatedAffectedSubjects.ShouldBe(2000);

        var reported = (BreachReportedToDPA)events[2].Data;
        reported.AuthorityName.ShouldBe("DPA");

        var notified = (BreachNotifiedToSubjects)events[3].Data;
        notified.SubjectCount.ShouldBe(2000);
        notified.CommunicationMethod.ShouldBe("email");

        // Verify monotonically increasing versions
        for (var i = 1; i < events.Count; i++)
        {
            events[i].Version.ShouldBeGreaterThan(events[i - 1].Version);
        }

        // Verify all events have timestamps
        foreach (var evt in events)
        {
            evt.Timestamp.ShouldNotBe(default);
        }
    }

    [Fact]
    public async Task Breach_EventStream_IncludesPhasedReports()
    {
        // Arrange
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var aggregate = BreachAggregate.Detect(
            id, "breach with phases", BreachSeverity.Medium, "Rule1",
            100, "Phased reporting test", "user-1", DateTimeOffset.UtcNow);
        aggregate.Assess(BreachSeverity.Medium, 100, "Confirmed", "assessor-1", DateTimeOffset.UtcNow);
        aggregate.ReportToDPA("DPA", "dpa@gov.eu", "Initial report", "dpo-1", DateTimeOffset.UtcNow);
        aggregate.AddPhasedReport("Phase 1: root cause identified", "analyst-1", DateTimeOffset.UtcNow);
        aggregate.AddPhasedReport("Phase 2: remediation plan", "analyst-1", DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert — 5 events: Detected + Assessed + ReportedToDPA + 2 PhasedReportAdded
        events.Count.ShouldBe(5);
        events[3].Data.ShouldBeOfType<BreachPhasedReportAdded>();
        events[4].Data.ShouldBeOfType<BreachPhasedReportAdded>();

        var phase1 = (BreachPhasedReportAdded)events[3].Data;
        phase1.PhaseNumber.ShouldBe(1);
        phase1.ReportContent.ShouldBe("Phase 1: root cause identified");

        var phase2 = (BreachPhasedReportAdded)events[4].Data;
        phase2.PhaseNumber.ShouldBe(2);
    }

    #endregion

    #region Sequential Modifications (Optimistic Concurrency)

    [Fact]
    public async Task Breach_SequentialModifications_BothPersistCorrectly()
    {
        // Arrange
        var repo = CreateRepository<BreachAggregate>();
        var id = Guid.NewGuid();
        var aggregate = BreachAggregate.Detect(
            id, "test breach", BreachSeverity.Low, "TestRule",
            50, "Sequential mod test", "user-1", DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // First modification: assess
        var repo1 = CreateRepository<BreachAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded1.Assess(BreachSeverity.Medium, 100, "Severity upgraded", "assessor-1", DateTimeOffset.UtcNow);
        var result1 = await repo1.SaveAsync(loaded1);
        result1.IsRight.ShouldBeTrue();

        // Second modification: report to DPA (loads fresh state)
        var repo2 = CreateRepository<BreachAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.Status.ShouldBe(BreachStatus.Investigating);
        loaded2.ReportToDPA("BfDI", "bfdi@bfdi.bund.de", "Report", "dpo-1", DateTimeOffset.UtcNow);
        var result2 = await repo2.SaveAsync(loaded2);
        result2.IsRight.ShouldBeTrue();

        // Verify — both modifications applied
        var verifyRepo = CreateRepository<BreachAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.Status.ShouldBe(BreachStatus.AuthorityNotified);
        final.Severity.ShouldBe(BreachSeverity.Medium);
        final.AuthorityName.ShouldBe("BfDI");
    }

    #endregion

    #region Load Non-Existent Aggregate

    [Fact]
    public async Task LoadNonExistentBreach_ReturnsLeft()
    {
        // Arrange
        var repo = CreateRepository<BreachAggregate>();

        // Act
        var result = await repo.LoadAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
