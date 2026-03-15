using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Integration tests for cross-border transfer aggregates persisted via Marten against real PostgreSQL.
/// Verifies event store persistence, aggregate loading, state reconstruction, and lifecycle transitions.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CrossBorderTransferAggregateIntegrationTests
{
    private readonly MartenFixture _fixture;

    public CrossBorderTransferAggregateIntegrationTests(MartenFixture fixture)
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

    #region TIA Aggregate Persistence

    [Fact]
    public async Task TIA_CreateAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<TIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = TIAAggregate.Create(id, "DE", "US", "health-data", "analyst-1", "tenant-1", "compliance");

        // Act
        var createResult = await repo.CreateAsync(aggregate);

        // Assert — creation succeeds
        createResult.IsRight.ShouldBeTrue();

        // Act — load from event store
        var loadRepo = CreateRepository<TIAAggregate>();
        var loadResult = await loadRepo.LoadAsync(id);

        // Assert — state reconstructed from events
        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.SourceCountryCode.ShouldBe("DE");
            loaded.DestinationCountryCode.ShouldBe("US");
            loaded.DataCategory.ShouldBe("health-data");
            loaded.Status.ShouldBe(TIAStatus.Draft);
            loaded.TenantId.ShouldBe("tenant-1");
            loaded.ModuleId.ShouldBe("compliance");
        });
    }

    [Fact]
    public async Task TIA_FullLifecycle_PersistsAllStateTransitions()
    {
        // Arrange — create TIA
        var repo = CreateRepository<TIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = TIAAggregate.Create(id, "DE", "CN", "financial-data", "analyst-1");
        await repo.CreateAsync(aggregate);

        // Act — assess risk
        var loadRepo1 = CreateRepository<TIAAggregate>();
        var loaded1 = (await loadRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.AssessRisk(0.75, "High surveillance risk", "risk-assessor-1");
        await loadRepo1.SaveAsync(loaded1);

        // Act — add supplementary measure
        var loadRepo2 = CreateRepository<TIAAggregate>();
        var loaded2 = (await loadRepo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "End-to-end encryption");
        await loadRepo2.SaveAsync(loaded2);

        // Act — submit for DPO review
        var loadRepo3 = CreateRepository<TIAAggregate>();
        var loaded3 = (await loadRepo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded3.SubmitForDPOReview("submitter-1");
        await loadRepo3.SaveAsync(loaded3);

        // Act — approve and complete
        var loadRepo4 = CreateRepository<TIAAggregate>();
        var loaded4 = (await loadRepo4.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded4.ApproveDPOReview("dpo-1");
        loaded4.Complete();
        await loadRepo4.SaveAsync(loaded4);

        // Assert — final state from fresh load
        var verifyRepo = CreateRepository<TIAAggregate>();
        var finalResult = await verifyRepo.LoadAsync(id);

        finalResult.IsRight.ShouldBeTrue();
        finalResult.IfRight(final =>
        {
            final.Status.ShouldBe(TIAStatus.Completed);
            final.RiskScore.ShouldBe(0.75);
            final.Findings.ShouldBe("High surveillance risk");
            final.AssessorId.ShouldBe("risk-assessor-1");
            final.RequiredSupplementaryMeasures.Count.ShouldBe(1);
            final.RequiredSupplementaryMeasures[0].Type.ShouldBe(SupplementaryMeasureType.Technical);
            final.RequiredSupplementaryMeasures[0].Description.ShouldBe("End-to-end encryption");
            final.CompletedAtUtc.ShouldNotBeNull();
            final.DPOReviewedAtUtc.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task TIA_DPORejection_ReturnsToInProgress()
    {
        // Arrange — create, assess, submit for review
        var repo = CreateRepository<TIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = TIAAggregate.Create(id, "FR", "RU", "employee-data", "analyst-1");
        await repo.CreateAsync(aggregate);

        var repo2 = CreateRepository<TIAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.AssessRisk(0.9, "Extremely high risk", "assessor-1");
        loaded.SubmitForDPOReview("submitter-1");
        await repo2.SaveAsync(loaded);

        // Act — DPO rejects
        var repo3 = CreateRepository<TIAAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.RejectDPOReview("dpo-1", "Insufficient supplementary measures");
        await repo3.SaveAsync(loaded2);

        // Assert — back to InProgress, can re-assess
        var verifyRepo = CreateRepository<TIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(TIAStatus.InProgress);
        final.DPOReviewedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task TIA_Expiration_TransitionsFromCompleted()
    {
        // Arrange — create a completed TIA
        var repo = CreateRepository<TIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = TIAAggregate.Create(id, "DE", "IN", "marketing", "analyst-1");
        aggregate.AssessRisk(0.3, "Low risk", "assessor-1");
        aggregate.SubmitForDPOReview("submitter-1");
        aggregate.ApproveDPOReview("dpo-1");
        aggregate.Complete();
        await repo.CreateAsync(aggregate);

        // Act — expire
        var repo2 = CreateRepository<TIAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Expire();
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<TIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(TIAStatus.Expired);
    }

    [Fact]
    public async Task TIA_MultipleSupplementaryMeasures_AllPersisted()
    {
        // Arrange
        var repo = CreateRepository<TIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = TIAAggregate.Create(id, "DE", "BR", "customer-data", "analyst-1");
        aggregate.AssessRisk(0.6, "Medium risk", "assessor-1");
        aggregate.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption at rest");
        aggregate.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Contractual, "Data processing addendum");
        aggregate.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Organizational, "Access control policy");
        await repo.CreateAsync(aggregate);

        // Act — load and verify
        var verifyRepo = CreateRepository<TIAAggregate>();
        var loaded = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));

        // Assert
        loaded.RequiredSupplementaryMeasures.Count.ShouldBe(3);
        loaded.RequiredSupplementaryMeasures.ShouldContain(m => m.Type == SupplementaryMeasureType.Technical);
        loaded.RequiredSupplementaryMeasures.ShouldContain(m => m.Type == SupplementaryMeasureType.Contractual);
        loaded.RequiredSupplementaryMeasures.ShouldContain(m => m.Type == SupplementaryMeasureType.Organizational);
    }

    #endregion

    #region SCC Agreement Aggregate Persistence

    [Fact]
    public async Task SCC_RegisterAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<SCCAgreementAggregate>();
        var id = Guid.NewGuid();
        var executedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var expiresAt = DateTimeOffset.UtcNow.AddYears(2);
        var aggregate = SCCAgreementAggregate.Register(
            id, "processor-acme", SCCModule.ControllerToProcessor, "2021/914",
            executedAt, expiresAt, "tenant-1", "compliance");

        // Act
        await repo.CreateAsync(aggregate);

        // Assert
        var verifyRepo = CreateRepository<SCCAgreementAggregate>();
        var loaded = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Id.ShouldBe(id);
        loaded.ProcessorId.ShouldBe("processor-acme");
        loaded.Module.ShouldBe(SCCModule.ControllerToProcessor);
        loaded.SCCVersion.ShouldBe("2021/914");
        loaded.ExecutedAtUtc.ShouldBe(executedAt);
        loaded.ExpiresAtUtc.ShouldBe(expiresAt);
        loaded.IsRevoked.ShouldBeFalse();
        loaded.IsExpired.ShouldBeFalse();
        loaded.TenantId.ShouldBe("tenant-1");
        loaded.ModuleId.ShouldBe("compliance");
    }

    [Fact]
    public async Task SCC_AddSupplementaryMeasureAndRevoke_PersistsLifecycle()
    {
        // Arrange
        var repo = CreateRepository<SCCAgreementAggregate>();
        var id = Guid.NewGuid();
        var aggregate = SCCAgreementAggregate.Register(
            id, "processor-beta", SCCModule.ControllerToController, "2021/914",
            DateTimeOffset.UtcNow.AddDays(-10));
        await repo.CreateAsync(aggregate);

        // Act — add supplementary measure
        var repo2 = CreateRepository<SCCAgreementAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Pseudonymization");
        await repo2.SaveAsync(loaded);

        // Act — revoke
        var repo3 = CreateRepository<SCCAgreementAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Revoke("Processor non-compliance detected", "dpo-1");
        await repo3.SaveAsync(loaded2);

        // Assert
        var verifyRepo = CreateRepository<SCCAgreementAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsRevoked.ShouldBeTrue();
        final.RevokedAtUtc.ShouldNotBeNull();
        final.SupplementaryMeasures.Count.ShouldBe(1);
        final.SupplementaryMeasures[0].Description.ShouldBe("Pseudonymization");
    }

    [Fact]
    public async Task SCC_Expire_TransitionsCorrectly()
    {
        // Arrange
        var repo = CreateRepository<SCCAgreementAggregate>();
        var id = Guid.NewGuid();
        var aggregate = SCCAgreementAggregate.Register(
            id, "processor-gamma", SCCModule.ControllerToProcessor, "2021/914",
            DateTimeOffset.UtcNow.AddYears(-2), DateTimeOffset.UtcNow.AddDays(-1));
        await repo.CreateAsync(aggregate);

        // Act
        var repo2 = CreateRepository<SCCAgreementAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Expire();
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<SCCAgreementAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsExpired.ShouldBeTrue();
        final.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    #endregion

    #region Approved Transfer Aggregate Persistence

    [Fact]
    public async Task Transfer_ApproveAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<ApprovedTransferAggregate>();
        var id = Guid.NewGuid();
        var sccId = Guid.NewGuid();
        var tiaId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddYears(1);
        var aggregate = ApprovedTransferAggregate.Approve(
            id, "DE", "US", "health-data", TransferBasis.SCCs,
            sccId, tiaId, "approver-1", expiresAt, "tenant-1", "compliance");

        // Act
        await repo.CreateAsync(aggregate);

        // Assert
        var verifyRepo = CreateRepository<ApprovedTransferAggregate>();
        var loaded = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Id.ShouldBe(id);
        loaded.SourceCountryCode.ShouldBe("DE");
        loaded.DestinationCountryCode.ShouldBe("US");
        loaded.DataCategory.ShouldBe("health-data");
        loaded.Basis.ShouldBe(TransferBasis.SCCs);
        loaded.SCCAgreementId.ShouldBe(sccId);
        loaded.TIAId.ShouldBe(tiaId);
        loaded.ApprovedBy.ShouldBe("approver-1");
        loaded.ExpiresAtUtc.ShouldBe(expiresAt);
        loaded.IsRevoked.ShouldBeFalse();
        loaded.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public async Task Transfer_RevokeLifecycle_PersistsRevocation()
    {
        // Arrange
        var repo = CreateRepository<ApprovedTransferAggregate>();
        var id = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            id, "FR", "IN", "marketing", TransferBasis.AdequacyDecision,
            approvedBy: "approver-1");
        await repo.CreateAsync(aggregate);

        // Act
        var repo2 = CreateRepository<ApprovedTransferAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Revoke("Adequacy decision invalidated", "dpo-1");
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<ApprovedTransferAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsRevoked.ShouldBeTrue();
        final.RevokedAtUtc.ShouldNotBeNull();
        final.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public async Task Transfer_RenewLifecycle_UpdatesExpiration()
    {
        // Arrange
        var repo = CreateRepository<ApprovedTransferAggregate>();
        var id = Guid.NewGuid();
        var originalExpiry = DateTimeOffset.UtcNow.AddMonths(6);
        var aggregate = ApprovedTransferAggregate.Approve(
            id, "DE", "JP", "analytics", TransferBasis.SCCs,
            approvedBy: "approver-1", expiresAtUtc: originalExpiry);
        await repo.CreateAsync(aggregate);

        // Act — renew with new expiry
        var newExpiry = DateTimeOffset.UtcNow.AddYears(2);
        var repo2 = CreateRepository<ApprovedTransferAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Renew(newExpiry, "renewer-1");
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<ApprovedTransferAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.ExpiresAtUtc.ShouldBe(newExpiry);
        final.IsValid(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public async Task Transfer_ExpireAndRenew_ResetsExpiredFlag()
    {
        // Arrange
        var repo = CreateRepository<ApprovedTransferAggregate>();
        var id = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            id, "DE", "KR", "customer-data", TransferBasis.BindingCorporateRules,
            approvedBy: "approver-1", expiresAtUtc: DateTimeOffset.UtcNow.AddDays(-1));
        await repo.CreateAsync(aggregate);

        // Act — expire then renew
        var repo2 = CreateRepository<ApprovedTransferAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Expire();
        loaded.Renew(DateTimeOffset.UtcNow.AddYears(1), "renewer-1");
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<ApprovedTransferAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsExpired.ShouldBeFalse();
        final.IsValid(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    #endregion

    #region Cross-Aggregate Isolation

    [Fact]
    public async Task MultipleAggregateTypes_AreIsolated()
    {
        // Arrange — create one of each aggregate type
        var tiaId = Guid.NewGuid();
        var sccId = Guid.NewGuid();
        var transferId = Guid.NewGuid();

        var tiaRepo = CreateRepository<TIAAggregate>();
        var sccRepo = CreateRepository<SCCAgreementAggregate>();
        var transferRepo = CreateRepository<ApprovedTransferAggregate>();

        var tia = TIAAggregate.Create(tiaId, "DE", "US", "health", "analyst-1");
        var scc = SCCAgreementAggregate.Register(sccId, "proc-1", SCCModule.ControllerToProcessor, "2021/914", DateTimeOffset.UtcNow);
        var transfer = ApprovedTransferAggregate.Approve(transferId, "DE", "US", "health", TransferBasis.SCCs, sccId, tiaId, "approver-1");

        await tiaRepo.CreateAsync(tia);
        await sccRepo.CreateAsync(scc);
        await transferRepo.CreateAsync(transfer);

        // Act — load each from fresh repositories
        var verifyTia = CreateRepository<TIAAggregate>();
        var verifyScc = CreateRepository<SCCAgreementAggregate>();
        var verifyTransfer = CreateRepository<ApprovedTransferAggregate>();

        var tiaResult = await verifyTia.LoadAsync(tiaId);
        var sccResult = await verifyScc.LoadAsync(sccId);
        var transferResult = await verifyTransfer.LoadAsync(transferId);

        // Assert — each loaded independently
        tiaResult.IsRight.ShouldBeTrue();
        sccResult.IsRight.ShouldBeTrue();
        transferResult.IsRight.ShouldBeTrue();

        tiaResult.IfRight(a => a.SourceCountryCode.ShouldBe("DE"));
        sccResult.IfRight(a => a.ProcessorId.ShouldBe("proc-1"));
        transferResult.IfRight(a => a.Basis.ShouldBe(TransferBasis.SCCs));
    }

    [Fact]
    public async Task LoadNonExistentAggregate_ReturnsLeft()
    {
        // Arrange
        var repo = CreateRepository<TIAAggregate>();

        // Act
        var result = await repo.LoadAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
