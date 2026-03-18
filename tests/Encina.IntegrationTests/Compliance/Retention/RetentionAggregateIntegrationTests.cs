using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.Retention;

/// <summary>
/// Integration tests for retention compliance aggregates persisted via Marten against real PostgreSQL.
/// Verifies event store persistence, aggregate loading, state reconstruction, and lifecycle transitions
/// for RetentionPolicyAggregate, RetentionRecordAggregate, and LegalHoldAggregate.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RetentionAggregateIntegrationTests
{
    private readonly MartenFixture _fixture;

    public RetentionAggregateIntegrationTests(MartenFixture fixture)
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

    #region RetentionPolicy Aggregate Persistence

    [Fact]
    public async Task Policy_CreateAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<RetentionPolicyAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionPolicyAggregate.Create(
            id, "customer-data", TimeSpan.FromDays(365), true,
            RetentionPolicyType.TimeBased, "GDPR compliance", "Art. 5(1)(e)",
            now, "tenant-1", "compliance");

        // Act
        var createResult = await repo.CreateAsync(aggregate);

        // Assert — creation succeeds
        createResult.IsRight.ShouldBeTrue();

        // Act — load from event store
        var loadRepo = CreateRepository<RetentionPolicyAggregate>();
        var loadResult = await loadRepo.LoadAsync(id);

        // Assert — state reconstructed from events
        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.DataCategory.ShouldBe("customer-data");
            loaded.RetentionPeriod.ShouldBe(TimeSpan.FromDays(365));
            loaded.AutoDelete.ShouldBeTrue();
            loaded.PolicyType.ShouldBe(RetentionPolicyType.TimeBased);
            loaded.Reason.ShouldBe("GDPR compliance");
            loaded.LegalBasis.ShouldBe("Art. 5(1)(e)");
            loaded.IsActive.ShouldBeTrue();
            loaded.TenantId.ShouldBe("tenant-1");
            loaded.ModuleId.ShouldBe("compliance");
        });
    }

    [Fact]
    public async Task Policy_UpdateLifecycle_PersistsAllStateTransitions()
    {
        // Arrange — create policy
        var repo = CreateRepository<RetentionPolicyAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionPolicyAggregate.Create(
            id, "financial-records", TimeSpan.FromDays(365 * 7), true,
            RetentionPolicyType.TimeBased, "Tax requirements", "Tax Code §147",
            now);
        await repo.CreateAsync(aggregate);

        // Act — update policy
        var loadRepo1 = CreateRepository<RetentionPolicyAggregate>();
        var loaded1 = (await loadRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.Update(TimeSpan.FromDays(365 * 10), false, "Extended tax retention", "Tax Code §147a", now.AddMinutes(1));
        await loadRepo1.SaveAsync(loaded1);

        // Assert — final state from fresh load
        var verifyRepo = CreateRepository<RetentionPolicyAggregate>();
        var finalResult = await verifyRepo.LoadAsync(id);

        finalResult.IsRight.ShouldBeTrue();
        finalResult.IfRight(final =>
        {
            final.DataCategory.ShouldBe("financial-records");
            final.RetentionPeriod.ShouldBe(TimeSpan.FromDays(365 * 10));
            final.AutoDelete.ShouldBeFalse();
            final.Reason.ShouldBe("Extended tax retention");
            final.LegalBasis.ShouldBe("Tax Code §147a");
            final.IsActive.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Policy_Deactivation_PersistsDeactivatedState()
    {
        // Arrange — create active policy
        var repo = CreateRepository<RetentionPolicyAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionPolicyAggregate.Create(
            id, "marketing-consent", TimeSpan.FromDays(90), true,
            RetentionPolicyType.ConsentBased, null, null, now);
        await repo.CreateAsync(aggregate);

        // Act — deactivate
        var repo2 = CreateRepository<RetentionPolicyAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Deactivate("Category no longer collected", now.AddDays(1));
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<RetentionPolicyAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsActive.ShouldBeFalse();
        final.DataCategory.ShouldBe("marketing-consent");
    }

    [Fact]
    public async Task Policy_FullLifecycle_CreateUpdateDeactivate()
    {
        // Arrange
        var repo = CreateRepository<RetentionPolicyAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionPolicyAggregate.Create(
            id, "health-records", TimeSpan.FromDays(365 * 30), true,
            RetentionPolicyType.TimeBased, "Medical law", "Health Data Act §12",
            now, "tenant-med", "health-module");
        await repo.CreateAsync(aggregate);

        // Act — update twice, then deactivate
        var repo2 = CreateRepository<RetentionPolicyAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Update(TimeSpan.FromDays(365 * 25), true, "Revised medical law", "Health Data Act §12a", now.AddMinutes(1));
        await repo2.SaveAsync(loaded);

        var repo3 = CreateRepository<RetentionPolicyAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Update(TimeSpan.FromDays(365 * 20), false, "Further revision", "Health Data Act §12b", now.AddMinutes(2));
        await repo3.SaveAsync(loaded2);

        var repo4 = CreateRepository<RetentionPolicyAggregate>();
        var loaded3 = (await repo4.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded3.Deactivate("Policy superseded by new regulation", now.AddMinutes(3));
        await repo4.SaveAsync(loaded3);

        // Assert — final state reflects entire lifecycle
        var verifyRepo = CreateRepository<RetentionPolicyAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsActive.ShouldBeFalse();
        final.RetentionPeriod.ShouldBe(TimeSpan.FromDays(365 * 20));
        final.Reason.ShouldBe("Further revision");
        final.LegalBasis.ShouldBe("Health Data Act §12b");
        final.TenantId.ShouldBe("tenant-med");
        final.ModuleId.ShouldBe("health-module");
    }

    #endregion

    #region RetentionRecord Aggregate Persistence

    [Fact]
    public async Task Record_TrackAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<RetentionRecordAggregate>();
        var id = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(365);
        var aggregate = RetentionRecordAggregate.Track(
            id, "customer-123", "customer-data", policyId,
            TimeSpan.FromDays(365), expiresAt, now, "tenant-1", "crm-module");

        // Act
        await repo.CreateAsync(aggregate);

        // Assert
        var verifyRepo = CreateRepository<RetentionRecordAggregate>();
        var loaded = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Id.ShouldBe(id);
        loaded.EntityId.ShouldBe("customer-123");
        loaded.DataCategory.ShouldBe("customer-data");
        loaded.PolicyId.ShouldBe(policyId);
        loaded.RetentionPeriod.ShouldBe(TimeSpan.FromDays(365));
        loaded.Status.ShouldBe(RetentionStatus.Active);
        loaded.ExpiresAtUtc.ShouldBe(expiresAt);
        loaded.TenantId.ShouldBe("tenant-1");
        loaded.ModuleId.ShouldBe("crm-module");
    }

    [Fact]
    public async Task Record_FullLifecycleToDeleted_PersistsAllTransitions()
    {
        // Arrange — track a record
        var repo = CreateRepository<RetentionRecordAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionRecordAggregate.Track(
            id, "order-456", "order-data", Guid.NewGuid(),
            TimeSpan.FromDays(180), now.AddDays(180), now);
        await repo.CreateAsync(aggregate);

        // Act — mark expired
        var repo2 = CreateRepository<RetentionRecordAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.MarkExpired(now.AddDays(181));
        await repo2.SaveAsync(loaded);

        // Act — mark deleted
        var repo3 = CreateRepository<RetentionRecordAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.MarkDeleted(now.AddDays(182));
        await repo3.SaveAsync(loaded2);

        // Assert — terminal state
        var verifyRepo = CreateRepository<RetentionRecordAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(RetentionStatus.Deleted);
        final.EntityId.ShouldBe("order-456");
    }

    [Fact]
    public async Task Record_FullLifecycleToAnonymized_PersistsAllTransitions()
    {
        // Arrange
        var repo = CreateRepository<RetentionRecordAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionRecordAggregate.Track(
            id, "user-789", "analytics-data", Guid.NewGuid(),
            TimeSpan.FromDays(90), now.AddDays(90), now);
        await repo.CreateAsync(aggregate);

        // Act — expire then anonymize
        var repo2 = CreateRepository<RetentionRecordAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.MarkExpired(now.AddDays(91));
        await repo2.SaveAsync(loaded);

        var repo3 = CreateRepository<RetentionRecordAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.MarkAnonymized(now.AddDays(92));
        await repo3.SaveAsync(loaded2);

        // Assert — terminal state via anonymization
        var verifyRepo = CreateRepository<RetentionRecordAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(RetentionStatus.Deleted);
        final.EntityId.ShouldBe("user-789");
    }

    [Fact]
    public async Task Record_LegalHoldSuspendsLifecycle_PersistsHoldAndRelease()
    {
        // Arrange — track and expire a record
        var repo = CreateRepository<RetentionRecordAggregate>();
        var id = Guid.NewGuid();
        var holdId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionRecordAggregate.Track(
            id, "employee-001", "employee-data", Guid.NewGuid(),
            TimeSpan.FromDays(730), now.AddDays(730), now);
        await repo.CreateAsync(aggregate);

        // Act — place legal hold on active record
        var repo2 = CreateRepository<RetentionRecordAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Hold(holdId, now.AddDays(100));
        await repo2.SaveAsync(loaded);

        // Assert — under legal hold
        var midRepo = CreateRepository<RetentionRecordAggregate>();
        var midResult = (await midRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        midResult.Status.ShouldBe(RetentionStatus.UnderLegalHold);
        midResult.LegalHoldId.ShouldBe(holdId);

        // Act — release hold
        var repo3 = CreateRepository<RetentionRecordAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Release(holdId, now.AddDays(200));
        await repo3.SaveAsync(loaded2);

        // Assert — resumed lifecycle (status depends on expiration time vs current time)
        var verifyRepo = CreateRepository<RetentionRecordAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.LegalHoldId.ShouldBeNull();
        // Status should be Active or Expired depending on current time vs ExpiresAtUtc
        final.Status.ShouldBeOneOf(RetentionStatus.Active, RetentionStatus.Expired);
    }

    [Fact]
    public async Task Record_HoldOnExpiredRecord_PersistsCorrectly()
    {
        // Arrange — track and expire
        var repo = CreateRepository<RetentionRecordAggregate>();
        var id = Guid.NewGuid();
        var holdId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = RetentionRecordAggregate.Track(
            id, "contract-999", "contract-data", Guid.NewGuid(),
            TimeSpan.FromDays(30), now.AddDays(30), now);
        await repo.CreateAsync(aggregate);

        var repo2 = CreateRepository<RetentionRecordAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.MarkExpired(now.AddDays(31));
        await repo2.SaveAsync(loaded);

        // Act — place hold on expired record
        var repo3 = CreateRepository<RetentionRecordAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Hold(holdId, now.AddDays(32));
        await repo3.SaveAsync(loaded2);

        // Assert — hold prevents deletion
        var verifyRepo = CreateRepository<RetentionRecordAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(RetentionStatus.UnderLegalHold);
        final.LegalHoldId.ShouldBe(holdId);
    }

    #endregion

    #region LegalHold Aggregate Persistence

    [Fact]
    public async Task LegalHold_PlaceAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<LegalHoldAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = LegalHoldAggregate.Place(
            id, "customer-123", "Ongoing litigation - Case #54321",
            "legal-counsel-1", now, "tenant-1", "legal-module");

        // Act
        await repo.CreateAsync(aggregate);

        // Assert
        var verifyRepo = CreateRepository<LegalHoldAggregate>();
        var loaded = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Id.ShouldBe(id);
        loaded.EntityId.ShouldBe("customer-123");
        loaded.Reason.ShouldBe("Ongoing litigation - Case #54321");
        loaded.AppliedByUserId.ShouldBe("legal-counsel-1");
        loaded.IsActive.ShouldBeTrue();
        loaded.ReleasedByUserId.ShouldBeNull();
        loaded.ReleasedAtUtc.ShouldBeNull();
        loaded.TenantId.ShouldBe("tenant-1");
        loaded.ModuleId.ShouldBe("legal-module");
    }

    [Fact]
    public async Task LegalHold_PlaceAndLift_PersistsLifecycle()
    {
        // Arrange
        var repo = CreateRepository<LegalHoldAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = LegalHoldAggregate.Place(
            id, "employee-data-42", "Regulatory investigation",
            "legal-counsel-2", now);
        await repo.CreateAsync(aggregate);

        // Act — lift the hold
        var repo2 = CreateRepository<LegalHoldAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Lift("legal-counsel-3", now.AddDays(90));
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<LegalHoldAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsActive.ShouldBeFalse();
        final.ReleasedByUserId.ShouldBe("legal-counsel-3");
        final.ReleasedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Cross-Aggregate Isolation

    [Fact]
    public async Task MultipleAggregateTypes_AreIsolated()
    {
        // Arrange — create one of each aggregate type
        var policyId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var holdId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var policyRepo = CreateRepository<RetentionPolicyAggregate>();
        var recordRepo = CreateRepository<RetentionRecordAggregate>();
        var holdRepo = CreateRepository<LegalHoldAggregate>();

        var policy = RetentionPolicyAggregate.Create(
            policyId, "iso-test-data", TimeSpan.FromDays(365), true,
            RetentionPolicyType.TimeBased, null, null, now);
        var record = RetentionRecordAggregate.Track(
            recordId, "entity-iso-1", "iso-test-data", policyId,
            TimeSpan.FromDays(365), now.AddDays(365), now);
        var hold = LegalHoldAggregate.Place(
            holdId, "entity-iso-1", "Cross-aggregate isolation test",
            "tester-1", now);

        await policyRepo.CreateAsync(policy);
        await recordRepo.CreateAsync(record);
        await holdRepo.CreateAsync(hold);

        // Act — load each from fresh repositories
        var verifyPolicy = CreateRepository<RetentionPolicyAggregate>();
        var verifyRecord = CreateRepository<RetentionRecordAggregate>();
        var verifyHold = CreateRepository<LegalHoldAggregate>();

        var policyResult = await verifyPolicy.LoadAsync(policyId);
        var recordResult = await verifyRecord.LoadAsync(recordId);
        var holdResult = await verifyHold.LoadAsync(holdId);

        // Assert — each loaded independently
        policyResult.IsRight.ShouldBeTrue();
        recordResult.IsRight.ShouldBeTrue();
        holdResult.IsRight.ShouldBeTrue();

        policyResult.IfRight(a => a.DataCategory.ShouldBe("iso-test-data"));
        recordResult.IfRight(a => a.EntityId.ShouldBe("entity-iso-1"));
        holdResult.IfRight(a => a.Reason.ShouldBe("Cross-aggregate isolation test"));
    }

    [Fact]
    public async Task LoadNonExistentAggregate_ReturnsLeft()
    {
        // Arrange
        var repo = CreateRepository<RetentionPolicyAggregate>();

        // Act
        var result = await repo.LoadAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Concurrent Aggregate Operations

    [Fact]
    public async Task ConcurrentPolicyCreation_AllSucceed()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var repo = CreateRepository<RetentionPolicyAggregate>();
            var id = Guid.NewGuid();
            var aggregate = RetentionPolicyAggregate.Create(
                id, $"concurrent-cat-{i}", TimeSpan.FromDays(30 * (i + 1)), true,
                RetentionPolicyType.TimeBased, null, null, now);
            var result = await repo.CreateAsync(aggregate);
            return (Id: id, Result: result);
        })).ToArray();

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert — all creations succeed
        foreach (var (id, result) in results)
        {
            result.IsRight.ShouldBeTrue($"Policy {id} creation failed");
        }

        // Verify all can be loaded
        foreach (var (id, _) in results)
        {
            var verifyRepo = CreateRepository<RetentionPolicyAggregate>();
            var loadResult = await verifyRepo.LoadAsync(id);
            loadResult.IsRight.ShouldBeTrue($"Policy {id} load failed");
        }
    }

    #endregion
}
