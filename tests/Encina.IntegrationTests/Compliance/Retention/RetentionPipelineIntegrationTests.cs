using Encina.Compliance.Retention;
using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.Retention;

/// <summary>
/// Integration tests for the full Encina.Compliance.Retention pipeline.
/// Tests DI registration, full lifecycle flows (policy creation, record tracking,
/// enforcement, legal holds), options configuration, and concurrent access safety.
/// No Docker containers needed — all operations use in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class RetentionPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaRetention_RegistersIRetentionRecordStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRetentionRecordStore>().Should().NotBeNull();
        provider.GetService<IRetentionRecordStore>().Should().BeOfType<InMemoryRetentionRecordStore>();
    }

    [Fact]
    public void AddEncinaRetention_RegistersIRetentionPolicyStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRetentionPolicyStore>().Should().NotBeNull();
        provider.GetService<IRetentionPolicyStore>().Should().BeOfType<InMemoryRetentionPolicyStore>();
    }

    [Fact]
    public void AddEncinaRetention_RegistersILegalHoldStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ILegalHoldStore>().Should().NotBeNull();
        provider.GetService<ILegalHoldStore>().Should().BeOfType<InMemoryLegalHoldStore>();
    }

    [Fact]
    public void AddEncinaRetention_RegistersIRetentionAuditStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRetentionAuditStore>().Should().NotBeNull();
        provider.GetService<IRetentionAuditStore>().Should().BeOfType<InMemoryRetentionAuditStore>();
    }

    [Fact]
    public void AddEncinaRetention_RegistersIRetentionPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRetentionPolicy>().Should().NotBeNull();
        provider.GetService<IRetentionPolicy>().Should().BeOfType<DefaultRetentionPolicy>();
    }

    [Fact]
    public void AddEncinaRetention_RegistersIRetentionEnforcer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRetentionEnforcer>().Should().NotBeNull();
        provider.GetService<IRetentionEnforcer>().Should().BeOfType<DefaultRetentionEnforcer>();
    }

    [Fact]
    public void AddEncinaRetention_RegistersILegalHoldManager()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ILegalHoldManager>().Should().NotBeNull();
        provider.GetService<ILegalHoldManager>().Should().BeOfType<DefaultLegalHoldManager>();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaRetention_DefaultOptions_HaveCorrectValues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<RetentionOptions>>().Value;
        options.EnforcementMode.Should().Be(RetentionEnforcementMode.Warn);
        options.AlertBeforeExpirationDays.Should().Be(30);
        options.PublishNotifications.Should().BeTrue();
        options.TrackAuditTrail.Should().BeTrue();
        options.AddHealthCheck.Should().BeFalse();
        options.EnableAutomaticEnforcement.Should().BeTrue();
    }

    [Fact]
    public void AddEncinaRetention_CustomOptions_AreRespected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaRetention(options =>
        {
            options.EnforcementMode = RetentionEnforcementMode.Block;
            options.AlertBeforeExpirationDays = 14;
            options.PublishNotifications = false;
            options.TrackAuditTrail = false;
            options.AutoRegisterFromAttributes = false;
            options.EnableAutomaticEnforcement = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<RetentionOptions>>().Value;
        options.EnforcementMode.Should().Be(RetentionEnforcementMode.Block);
        options.AlertBeforeExpirationDays.Should().Be(14);
        options.PublishNotifications.Should().BeFalse();
        options.TrackAuditTrail.Should().BeFalse();
        options.AutoRegisterFromAttributes.Should().BeFalse();
        options.EnableAutomaticEnforcement.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaRetention_WithConfigure_CallsCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var callbackInvoked = false;

        // Act
        services.AddEncinaRetention(options =>
        {
            callbackInvoked = true;
            options.EnforcementMode = RetentionEnforcementMode.Disabled;
        });
        var provider = services.BuildServiceProvider();

        // Force options resolution to trigger the configure callback
        var options = provider.GetRequiredService<IOptions<RetentionOptions>>().Value;

        // Assert
        callbackInvoked.Should().BeTrue();
        options.EnforcementMode.Should().Be(RetentionEnforcementMode.Disabled);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task CreatePolicy_CreateRecord_EnforceRetention_RecordDeleted()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRetention(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.EnableAutomaticEnforcement = false;
            options.TrackAuditTrail = false;
        });
        var provider = services.BuildServiceProvider();

        var policyStore = provider.GetRequiredService<IRetentionPolicyStore>();
        var recordStore = provider.GetRequiredService<IRetentionRecordStore>();
        var enforcer = provider.GetRequiredService<IRetentionEnforcer>();

        // Act: Create a retention policy with a very short period so it expires immediately
        var policy = RetentionPolicy.Create(
            dataCategory: "integration-test-data",
            retentionPeriod: TimeSpan.FromMilliseconds(1),
            autoDelete: true,
            reason: "Integration test policy — expires immediately");

        var createPolicyResult = await policyStore.CreateAsync(policy);
        createPolicyResult.IsRight.Should().BeTrue("policy creation should succeed");

        // Act: Create a retention record with an expiration date already in the past
        var now = DateTimeOffset.UtcNow;
        var record = RetentionRecord.Create(
            entityId: "test-entity-001",
            dataCategory: "integration-test-data",
            createdAtUtc: now.AddDays(-2),
            expiresAtUtc: now.AddMilliseconds(-1),
            policyId: policy.Id);

        var createRecordResult = await recordStore.CreateAsync(record);
        createRecordResult.IsRight.Should().BeTrue("record creation should succeed");

        // Verify the record is Active before enforcement
        var beforeResult = await recordStore.GetByIdAsync(record.Id);
        beforeResult.IsRight.Should().BeTrue();
        var beforeRecord = beforeResult.Match(Right: opt => opt, Left: _ => default);
        beforeRecord.IsSome.Should().BeTrue("record should exist before enforcement");
        var beforeStatus = beforeRecord.Match(Some: r => r.Status, None: () => RetentionStatus.Active);
        beforeStatus.Should().Be(RetentionStatus.Active, "record should be Active before enforcement");

        // Act: Run enforcement cycle
        var enforceResult = await enforcer.EnforceRetentionAsync();
        enforceResult.IsRight.Should().BeTrue("enforcement should succeed");

        var deletionResult = enforceResult.Match(Right: r => r, Left: _ => null!);
        deletionResult.TotalRecordsEvaluated.Should().BeGreaterThan(0, "at least one record should be evaluated");
        deletionResult.RecordsDeleted.Should().BeGreaterThan(0, "at least one record should be deleted");

        // Assert: Record status should now be Deleted
        var afterResult = await recordStore.GetByIdAsync(record.Id);
        afterResult.IsRight.Should().BeTrue();
        var afterRecord = afterResult.Match(Right: opt => opt, Left: _ => default);
        afterRecord.IsSome.Should().BeTrue("record should still exist after enforcement (status updated)");
        var afterStatus = afterRecord.Match(Some: r => r.Status, None: () => RetentionStatus.Active);
        afterStatus.Should().Be(RetentionStatus.Deleted, "record should be Deleted after enforcement");
    }

    [Fact]
    public async Task CreateRecord_ApplyLegalHold_EnforceRetention_RecordRetained()
    {
        // Arrange
        // This test verifies that when a legal hold is applied to an entity BEFORE enforcement
        // runs, the record's status is updated to UnderLegalHold by ApplyHoldAsync itself.
        // The enforcement cycle then finds zero Active-expired records for this entity and
        // therefore does NOT delete it. The record remains UnderLegalHold throughout.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRetention(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.EnableAutomaticEnforcement = false;
            options.TrackAuditTrail = false;
        });
        var provider = services.BuildServiceProvider();

        var recordStore = provider.GetRequiredService<IRetentionRecordStore>();
        var enforcer = provider.GetRequiredService<IRetentionEnforcer>();
        var holdManager = provider.GetRequiredService<ILegalHoldManager>();

        // Act: Create an already-expired retention record (status = Active, ExpiresAtUtc in past)
        var now = DateTimeOffset.UtcNow;
        var record = RetentionRecord.Create(
            entityId: "held-entity-002",
            dataCategory: "financial-records",
            createdAtUtc: now.AddDays(-400),
            expiresAtUtc: now.AddDays(-35),
            policyId: null);

        var createRecordResult = await recordStore.CreateAsync(record);
        createRecordResult.IsRight.Should().BeTrue("record creation should succeed");

        // Act: Apply a legal hold — this transitions the record status to UnderLegalHold
        var hold = LegalHold.Create(
            entityId: "held-entity-002",
            reason: "Pending regulatory audit — must not delete",
            appliedByUserId: "legal-team@company.com");

        var applyHoldResult = await holdManager.ApplyHoldAsync("held-entity-002", hold);
        applyHoldResult.IsRight.Should().BeTrue("legal hold application should succeed");

        // Verify the entity is now under legal hold
        var isHeldResult = await holdManager.IsUnderHoldAsync("held-entity-002");
        isHeldResult.IsRight.Should().BeTrue();
        isHeldResult.Match(Right: h => h, Left: _ => false)
            .Should().BeTrue("entity should be under legal hold after ApplyHoldAsync");

        // Verify the record status was cascaded to UnderLegalHold by ApplyHoldAsync
        var recordBeforeEnforce = await recordStore.GetByIdAsync(record.Id);
        var statusBeforeEnforce = recordBeforeEnforce
            .Match(Right: opt => opt, Left: _ => default)
            .Match(Some: r => r.Status, None: () => RetentionStatus.Active);
        statusBeforeEnforce.Should().Be(RetentionStatus.UnderLegalHold,
            "ApplyHoldAsync should cascade UnderLegalHold status to all matching retention records");

        // Act: Run enforcement cycle
        // The record is UnderLegalHold (not Active), so GetExpiredRecordsAsync returns 0 records
        // for this entity. The enforcement cycle evaluates 0 records and deletes nothing.
        var enforceResult = await enforcer.EnforceRetentionAsync();
        enforceResult.IsRight.Should().BeTrue("enforcement should succeed even with no eligible records");

        var deletionResult = enforceResult.Match(Right: r => r, Left: _ => null!);
        deletionResult.RecordsDeleted.Should().Be(0,
            "the held record should NOT have been deleted by enforcement");

        // Assert: Record is still UnderLegalHold after enforcement — it was never deleted
        var afterResult = await recordStore.GetByIdAsync(record.Id);
        var afterStatus = afterResult
            .Match(Right: opt => opt, Left: _ => default)
            .Match(Some: r => r.Status, None: () => RetentionStatus.Active);
        afterStatus.Should().Be(RetentionStatus.UnderLegalHold,
            "record should remain UnderLegalHold after enforcement when a legal hold is active");
    }

    [Fact]
    public async Task CreateRecord_ApplyLegalHold_ReleaseHold_RecordTransitionsToExpired()
    {
        // Arrange
        // This test verifies the full legal hold lifecycle: apply hold → record becomes
        // UnderLegalHold → release hold → record reverts to Expired (because the retention
        // period has already elapsed). The record is now eligible for manual or future
        // automated deletion. The enforcement cycle itself only processes Active records,
        // so this test verifies the status lifecycle managed by ILegalHoldManager.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRetention(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.EnableAutomaticEnforcement = false;
            options.TrackAuditTrail = false;
        });
        var provider = services.BuildServiceProvider();

        var recordStore = provider.GetRequiredService<IRetentionRecordStore>();
        var holdManager = provider.GetRequiredService<ILegalHoldManager>();

        // Act: Create an already-expired retention record
        var now = DateTimeOffset.UtcNow;
        var record = RetentionRecord.Create(
            entityId: "release-entity-003",
            dataCategory: "session-logs",
            createdAtUtc: now.AddDays(-100),
            expiresAtUtc: now.AddDays(-5),
            policyId: null);

        var createRecordResult = await recordStore.CreateAsync(record);
        createRecordResult.IsRight.Should().BeTrue("record creation should succeed");

        // Act: Apply a legal hold — transitions record to UnderLegalHold
        var hold = LegalHold.Create(
            entityId: "release-entity-003",
            reason: "Temporary hold for compliance review",
            appliedByUserId: "compliance@company.com");

        var applyHoldResult = await holdManager.ApplyHoldAsync("release-entity-003", hold);
        applyHoldResult.IsRight.Should().BeTrue("hold application should succeed");

        // Verify hold is active and record is UnderLegalHold
        var isHeldBefore = await holdManager.IsUnderHoldAsync("release-entity-003");
        isHeldBefore.Match(Right: h => h, Left: _ => false).Should().BeTrue("hold should be active");

        var statusAfterApply = (await recordStore.GetByIdAsync(record.Id))
            .Match(Right: opt => opt, Left: _ => default)
            .Match(Some: r => r.Status, None: () => RetentionStatus.Active);
        statusAfterApply.Should().Be(RetentionStatus.UnderLegalHold,
            "record should be UnderLegalHold after hold is applied");

        // Act: Release the legal hold — transitions record to Expired (period already elapsed)
        var releaseResult = await holdManager.ReleaseHoldAsync(hold.Id, "compliance@company.com");
        releaseResult.IsRight.Should().BeTrue("hold release should succeed");

        // Assert: Entity is no longer under hold
        var isHeldAfterRelease = await holdManager.IsUnderHoldAsync("release-entity-003");
        isHeldAfterRelease.Match(Right: h => h, Left: _ => false)
            .Should().BeFalse("hold should be released");

        // Assert: Record reverted to Expired status because the retention period elapsed
        // while the hold was active. ReleaseHoldAsync recalculates: ExpiresAtUtc < now → Expired.
        var statusAfterRelease = (await recordStore.GetByIdAsync(record.Id))
            .Match(Right: opt => opt, Left: _ => default)
            .Match(Some: r => r.Status, None: () => RetentionStatus.Active);
        statusAfterRelease.Should().Be(RetentionStatus.Expired,
            "record should be Expired after hold release when the retention period has already passed");

        // Assert: Active holds list is now empty for the released entity
        var activeHolds = await holdManager.GetActiveHoldsAsync();
        activeHolds.IsRight.Should().BeTrue();
        var holdsList = activeHolds.Match(Right: h => h, Left: _ => []);
        holdsList.Should().NotContain(h => h.EntityId == "release-entity-003" && h.IsActive,
            "no active holds should remain for the entity after release");
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task ConcurrentRecordCreation_AllSucceed_NoDataCorruption()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRetention(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.EnableAutomaticEnforcement = false;
        });
        var provider = services.BuildServiceProvider();

        var recordStore = provider.GetRequiredService<IRetentionRecordStore>();
        var recordCount = 50;
        var now = DateTimeOffset.UtcNow;

        // Act: Create many records concurrently
        var tasks = Enumerable.Range(0, recordCount).Select(async i =>
        {
            var record = RetentionRecord.Create(
                entityId: $"concurrent-entity-{i}",
                dataCategory: "concurrent-test-data",
                createdAtUtc: now,
                expiresAtUtc: now.AddDays(365),
                policyId: null);

            var result = await recordStore.CreateAsync(record);
            return (EntityId: record.EntityId, RecordId: record.Id, Result: result);
        });

        var results = await Task.WhenAll(tasks);

        // Assert: All creations succeeded
        results.Should().AllSatisfy(r =>
            r.Result.IsRight.Should().BeTrue($"record creation for '{r.EntityId}' should succeed"));

        // Assert: All records are retrievable with correct data
        foreach (var (entityId, recordId, _) in results)
        {
            var retrieved = await recordStore.GetByIdAsync(recordId);
            retrieved.IsRight.Should().BeTrue($"record '{recordId}' should be retrievable");

            var recordOption = retrieved.Match(Right: opt => opt, Left: _ => default);
            recordOption.IsSome.Should().BeTrue($"record '{recordId}' should exist in the store");

            var storedRecord = recordOption.Match(Some: r => r, None: () => null!);
            storedRecord.EntityId.Should().Be(entityId, "stored EntityId should match what was created");
            storedRecord.DataCategory.Should().Be("concurrent-test-data");
            storedRecord.Status.Should().Be(RetentionStatus.Active);
        }

        // Assert: All entity IDs are unique (no overwrite/collision)
        var entityIds = results.Select(r => r.EntityId).ToList();
        entityIds.Should().OnlyHaveUniqueItems("each concurrent write should produce a distinct entity entry");
    }

    #endregion
}
