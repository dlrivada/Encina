using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.ProcessorAgreements;

/// <summary>
/// Integration tests for ProcessorAggregate and DPAAggregate persistence via Marten.
/// Validates full aggregate lifecycle, event stream reconstruction, and cross-aggregate isolation
/// against real PostgreSQL in Docker.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ProcessorAgreementAggregateIntegrationTests
{
    private readonly MartenFixture _fixture;

    public ProcessorAgreementAggregateIntegrationTests(MartenFixture fixture)
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

    private static DPAMandatoryTerms FullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    #region Processor Aggregate Persistence

    [Fact]
    public async Task Processor_RegisterAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<ProcessorAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var aggregate = ProcessorAggregate.Register(
            id, "Stripe", "US", "dpo@stripe.com", null, 0,
            SubProcessorAuthorizationType.Specific, now, "t1", "m1");

        // Act
        await repo.CreateAsync(aggregate);

        // Assert — load from a fresh repository
        var repo2 = CreateRepository<ProcessorAggregate>();
        var result = await repo2.LoadAsync(id);

        result.IsRight.ShouldBeTrue();
        result.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.Name.ShouldBe("Stripe");
            loaded.Country.ShouldBe("US");
            loaded.ContactEmail.ShouldBe("dpo@stripe.com");
            loaded.ParentProcessorId.ShouldBeNull();
            loaded.Depth.ShouldBe(0);
            loaded.AuthorizationType.ShouldBe(SubProcessorAuthorizationType.Specific);
            loaded.IsRemoved.ShouldBeFalse();
            loaded.TenantId.ShouldBe("t1");
            loaded.ModuleId.ShouldBe("m1");
        });
    }

    [Fact]
    public async Task Processor_FullLifecycle_RegisterUpdateAddSubProcessorRemove()
    {
        // Arrange — register
        var repo = CreateRepository<ProcessorAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = ProcessorAggregate.Register(
            id, "AWS", "US", "aws-dpo@amazon.com", null, 0,
            SubProcessorAuthorizationType.General, now);
        await repo.CreateAsync(aggregate);

        // Update identity
        var repo2 = CreateRepository<ProcessorAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded.Update("Amazon Web Services", "US", "dpo@aws.com",
            SubProcessorAuthorizationType.General, now.AddHours(1));
        await repo2.SaveAsync(loaded);

        // Add sub-processor
        var repo3 = CreateRepository<ProcessorAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        var subId = Guid.NewGuid();
        loaded2.AddSubProcessor(subId, "AWS Lambda", 1, now.AddHours(2));
        await repo3.SaveAsync(loaded2);

        // Remove
        var repo4 = CreateRepository<ProcessorAggregate>();
        var loaded3 = (await repo4.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded3.Remove("Contract terminated", now.AddDays(1));
        await repo4.SaveAsync(loaded3);

        // Verify final state
        var verifyRepo = CreateRepository<ProcessorAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.Name.ShouldBe("Amazon Web Services");
        final.ContactEmail.ShouldBe("dpo@aws.com");
        final.IsRemoved.ShouldBeTrue();
    }

    [Fact]
    public async Task Processor_SubProcessorHierarchy_PersistsCorrectly()
    {
        // Arrange — register parent
        var repo = CreateRepository<ProcessorAggregate>();
        var parentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var parent = ProcessorAggregate.Register(
            parentId, "Acme Corp", "DE", null, null, 0,
            SubProcessorAuthorizationType.General, now);
        await repo.CreateAsync(parent);

        // Register sub-processor as separate aggregate
        var subRepo = CreateRepository<ProcessorAggregate>();
        var subId = Guid.NewGuid();
        var sub = ProcessorAggregate.Register(
            subId, "Acme Payments", "IE", "dpo@payments.acme.com", parentId, 1,
            SubProcessorAuthorizationType.Specific, now);
        await subRepo.CreateAsync(sub);

        // Add sub-processor reference to parent
        var parentRepo2 = CreateRepository<ProcessorAggregate>();
        var loadedParent = (await parentRepo2.LoadAsync(parentId)).Match(a => a, _ => throw new InvalidOperationException());
        loadedParent.AddSubProcessor(subId, "Acme Payments", 1, now.AddMinutes(1));
        await parentRepo2.SaveAsync(loadedParent);

        // Verify sub-processor loaded correctly
        var verifyRepo = CreateRepository<ProcessorAggregate>();
        var loadedSub = (await verifyRepo.LoadAsync(subId)).Match(a => a, _ => throw new InvalidOperationException());
        loadedSub.ParentProcessorId.ShouldBe(parentId);
        loadedSub.Depth.ShouldBe(1);
        loadedSub.Name.ShouldBe("Acme Payments");
    }

    #endregion

    #region DPA Aggregate Persistence

    [Fact]
    public async Task DPA_ExecuteAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<DPAAggregate>();
        var id = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddYears(2);

        var aggregate = DPAAggregate.Execute(
            id, processorId, FullyCompliantTerms(), true,
            ["data-analytics", "payment-processing"],
            now, expiresAt, now, "t1", "m1");

        // Act
        await repo.CreateAsync(aggregate);

        // Assert
        var repo2 = CreateRepository<DPAAggregate>();
        var result = await repo2.LoadAsync(id);

        result.IsRight.ShouldBeTrue();
        result.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.ProcessorId.ShouldBe(processorId);
            loaded.Status.ShouldBe(DPAStatus.Active);
            loaded.MandatoryTerms.IsFullyCompliant.ShouldBeTrue();
            loaded.HasSCCs.ShouldBeTrue();
            loaded.ProcessingPurposes.Count.ShouldBe(2);
            loaded.ProcessingPurposes.ShouldContain("data-analytics");
            loaded.ProcessingPurposes.ShouldContain("payment-processing");
            loaded.ExpiresAtUtc.ShouldBe(expiresAt);
            loaded.TenantId.ShouldBe("t1");
            loaded.ModuleId.ShouldBe("m1");
        });
    }

    [Fact]
    public async Task DPA_FullLifecycle_ExecuteAmendAuditRenewTerminate()
    {
        // Arrange — execute DPA
        var repo = CreateRepository<DPAAggregate>();
        var id = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddYears(1);

        var aggregate = DPAAggregate.Execute(
            id, processorId, FullyCompliantTerms(), false,
            ["email-marketing"], now, expiresAt, now);
        await repo.CreateAsync(aggregate);

        // Amend — add SCCs
        var repo2 = CreateRepository<DPAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded.Amend(FullyCompliantTerms(), true, ["email-marketing", "analytics"],
            "Added SCCs for cross-border transfer", now.AddDays(30));
        await repo2.SaveAsync(loaded);

        // Audit
        var repo3 = CreateRepository<DPAAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.Audit("auditor-1", "All terms compliant, SCCs verified", now.AddDays(90));
        await repo3.SaveAsync(loaded2);

        // Mark pending renewal
        var repo4 = CreateRepository<DPAAggregate>();
        var loaded3 = (await repo4.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded3.MarkPendingRenewal(now.AddMonths(11));
        await repo4.SaveAsync(loaded3);

        // Renew
        var repo5 = CreateRepository<DPAAggregate>();
        var loaded4 = (await repo5.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded4.Status.ShouldBe(DPAStatus.PendingRenewal);
        var newExpiry = now.AddYears(2);
        loaded4.Renew(newExpiry, now.AddMonths(11).AddDays(5));
        await repo5.SaveAsync(loaded4);

        // Terminate
        var repo6 = CreateRepository<DPAAggregate>();
        var loaded5 = (await repo6.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded5.Terminate("Processor non-compliant", now.AddYears(2));
        await repo6.SaveAsync(loaded5);

        // Verify final state
        var verifyRepo = CreateRepository<DPAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.Status.ShouldBe(DPAStatus.Terminated);
        final.HasSCCs.ShouldBeTrue();
        final.ProcessingPurposes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DPA_Expiration_TransitionsFromActive()
    {
        // Arrange
        var repo = CreateRepository<DPAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPAAggregate.Execute(
            id, Guid.NewGuid(), FullyCompliantTerms(), false,
            ["processing"], now, now.AddDays(-1), now.AddDays(-2));
        await repo.CreateAsync(aggregate);

        // Expire
        var repo2 = CreateRepository<DPAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded.MarkExpired(now);
        await repo2.SaveAsync(loaded);

        // Verify
        var verifyRepo = CreateRepository<DPAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.Status.ShouldBe(DPAStatus.Expired);
    }

    [Fact]
    public async Task DPA_PartialTerms_PersistsCorrectly()
    {
        // Arrange — create DPA with partial mandatory terms
        var repo = CreateRepository<DPAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var partialTerms = new DPAMandatoryTerms
        {
            ProcessOnDocumentedInstructions = true,
            ConfidentialityObligations = true,
            SecurityMeasures = true,
            SubProcessorRequirements = false,
            DataSubjectRightsAssistance = true,
            ComplianceAssistance = false,
            DataDeletionOrReturn = true,
            AuditRights = false
        };

        var aggregate = DPAAggregate.Execute(
            id, Guid.NewGuid(), partialTerms, false, ["processing"], now, null, now);
        await repo.CreateAsync(aggregate);

        // Verify
        var repo2 = CreateRepository<DPAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded.MandatoryTerms.IsFullyCompliant.ShouldBeFalse();
        loaded.MandatoryTerms.SubProcessorRequirements.ShouldBeFalse();
        loaded.MandatoryTerms.ComplianceAssistance.ShouldBeFalse();
        loaded.MandatoryTerms.AuditRights.ShouldBeFalse();
    }

    #endregion

    #region Event Stream Audit Trail

    [Fact]
    public async Task DPA_EventStream_ContainsFullAuditTrail()
    {
        // Arrange
        var repo = CreateRepository<DPAAggregate>();
        var id = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var aggregate = DPAAggregate.Execute(
            id, processorId, FullyCompliantTerms(), true,
            ["analytics"], now, now.AddYears(1), now, "t1", "m1");
        aggregate.Audit("auditor-1", "Initial compliance check passed", now.AddDays(30));
        aggregate.Amend(FullyCompliantTerms(), true, ["analytics", "reporting"],
            "Added reporting purpose", now.AddDays(60));
        aggregate.MarkPendingRenewal(now.AddMonths(11));
        aggregate.Renew(now.AddYears(2), now.AddMonths(11).AddDays(5));
        aggregate.Terminate("Contract renegotiation", now.AddYears(2));
        await repo.CreateAsync(aggregate);

        // Act
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert
        events.ShouldNotBeNull();
        events.Count.ShouldBe(6);

        events[0].Data.ShouldBeOfType<DPAExecuted>();
        events[1].Data.ShouldBeOfType<DPAAudited>();
        events[2].Data.ShouldBeOfType<DPAAmended>();
        events[3].Data.ShouldBeOfType<DPAMarkedPendingRenewal>();
        events[4].Data.ShouldBeOfType<DPARenewed>();
        events[5].Data.ShouldBeOfType<DPATerminated>();

        // Verify event data
        var executed = (DPAExecuted)events[0].Data;
        executed.ProcessorId.ShouldBe(processorId);
        executed.HasSCCs.ShouldBeTrue();
        executed.TenantId.ShouldBe("t1");
        executed.ModuleId.ShouldBe("m1");

        var audited = (DPAAudited)events[1].Data;
        audited.AuditorId.ShouldBe("auditor-1");

        var terminated = (DPATerminated)events[5].Data;
        terminated.Reason.ShouldBe("Contract renegotiation");

        // Verify monotonically increasing versions
        for (var i = 1; i < events.Count; i++)
        {
            events[i].Version.ShouldBeGreaterThan(events[i - 1].Version);
        }
    }

    [Fact]
    public async Task Processor_EventStream_ContainsFullAuditTrail()
    {
        // Arrange
        var repo = CreateRepository<ProcessorAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var subId = Guid.NewGuid();

        var aggregate = ProcessorAggregate.Register(
            id, "Stripe", "US", "dpo@stripe.com", null, 0,
            SubProcessorAuthorizationType.General, now, "t1", "m1");
        aggregate.Update("Stripe Inc.", "US", "privacy@stripe.com",
            SubProcessorAuthorizationType.General, now.AddDays(30));
        aggregate.AddSubProcessor(subId, "Stripe Treasury", 1, now.AddDays(60));
        aggregate.RemoveSubProcessor(subId, "Service discontinued", now.AddDays(90));
        aggregate.Remove("Contract ended", now.AddDays(120));
        await repo.CreateAsync(aggregate);

        // Act
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert
        events.Count.ShouldBe(5);
        events[0].Data.ShouldBeOfType<ProcessorRegistered>();
        events[1].Data.ShouldBeOfType<ProcessorUpdated>();
        events[2].Data.ShouldBeOfType<SubProcessorAdded>();
        events[3].Data.ShouldBeOfType<SubProcessorRemoved>();
        events[4].Data.ShouldBeOfType<ProcessorRemoved>();

        var registered = (ProcessorRegistered)events[0].Data;
        registered.Name.ShouldBe("Stripe");
        registered.TenantId.ShouldBe("t1");

        var removed = (ProcessorRemoved)events[4].Data;
        removed.Reason.ShouldBe("Contract ended");
    }

    #endregion

    #region Cross-Aggregate Isolation

    [Fact]
    public async Task ProcessorAndDPA_AreIsolated()
    {
        // Arrange — create both aggregate types
        var processorRepo = CreateRepository<ProcessorAggregate>();
        var dpaRepo = CreateRepository<DPAAggregate>();
        var processorId = Guid.NewGuid();
        var dpaId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var processor = ProcessorAggregate.Register(
            processorId, "TestProc", "DE", null, null, 0,
            SubProcessorAuthorizationType.Specific, now);
        await processorRepo.CreateAsync(processor);

        var dpa = DPAAggregate.Execute(
            dpaId, processorId, FullyCompliantTerms(), false,
            ["processing"], now, null, now);
        await dpaRepo.CreateAsync(dpa);

        // Act — load each
        var pRepo = CreateRepository<ProcessorAggregate>();
        var pResult = await pRepo.LoadAsync(processorId);
        var dRepo = CreateRepository<DPAAggregate>();
        var dResult = await dRepo.LoadAsync(dpaId);

        // Assert — both exist independently
        pResult.IsRight.ShouldBeTrue();
        dResult.IsRight.ShouldBeTrue();

        // Loading DPA ID as Processor should fail
        var wrongRepo = CreateRepository<ProcessorAggregate>();
        var wrongResult = await wrongRepo.LoadAsync(dpaId);
        wrongResult.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadNonExistentAggregate_ReturnsLeft()
    {
        var repo = CreateRepository<DPAAggregate>();
        var result = await repo.LoadAsync(Guid.NewGuid());
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Concurrent Aggregate Operations

    [Fact]
    public async Task DPA_SequentialAmendAndAudit_BothPersist()
    {
        // Arrange
        var repo = CreateRepository<DPAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPAAggregate.Execute(
            id, Guid.NewGuid(), FullyCompliantTerms(), false,
            ["processing"], now, now.AddYears(1), now);
        await repo.CreateAsync(aggregate);

        // First: amend
        var repo1 = CreateRepository<DPAAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded1.Amend(FullyCompliantTerms(), true, ["processing", "analytics"],
            "Added SCCs", now.AddDays(30));
        var result1 = await repo1.SaveAsync(loaded1);
        result1.IsRight.ShouldBeTrue();

        // Second: audit (loads fresh state with amendment)
        var repo2 = CreateRepository<DPAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.HasSCCs.ShouldBeTrue();
        loaded2.Audit("auditor-1", "Post-amendment compliance review", now.AddDays(60));
        var result2 = await repo2.SaveAsync(loaded2);
        result2.IsRight.ShouldBeTrue();

        // Verify both applied
        var verifyRepo = CreateRepository<DPAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.HasSCCs.ShouldBeTrue();
        final.ProcessingPurposes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Processor_SequentialUpdateAndRemove_BothPersist()
    {
        // Arrange
        var repo = CreateRepository<ProcessorAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = ProcessorAggregate.Register(
            id, "OldName", "US", null, null, 0,
            SubProcessorAuthorizationType.Specific, now);
        await repo.CreateAsync(aggregate);

        // First: update
        var repo1 = CreateRepository<ProcessorAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded1.Update("NewName", "DE", "contact@new.com",
            SubProcessorAuthorizationType.General, now.AddDays(30));
        await repo1.SaveAsync(loaded1);

        // Second: remove (loads fresh state)
        var repo2 = CreateRepository<ProcessorAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.Name.ShouldBe("NewName");
        loaded2.Remove("Decommissioned", now.AddDays(60));
        await repo2.SaveAsync(loaded2);

        // Verify
        var verifyRepo = CreateRepository<ProcessorAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.IsRemoved.ShouldBeTrue();
        final.Name.ShouldBe("NewName");
    }

    #endregion
}
