using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.GDPR;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.DataSubjectRights;

/// <summary>
/// Integration tests for the full Encina.Compliance.DataSubjectRights pipeline.
/// Tests DI registration, full DSR lifecycle flows, health check integration,
/// and cross-component interactions.
/// No Docker containers needed — all operations use in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DSRPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaDataSubjectRights_RegistersAllDefaultServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IDSRRequestStore>().Should().NotBeNull();
        provider.GetService<IDSRAuditStore>().Should().NotBeNull();
        provider.GetService<IDataErasureStrategy>().Should().NotBeNull();
        provider.GetService<IDataSubjectIdExtractor>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_DefaultStore_IsInMemory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        // Assert
        var store = provider.GetRequiredService<IDSRRequestStore>();
        store.Should().BeOfType<InMemoryDSRRequestStore>();

        var auditStore = provider.GetRequiredService<IDSRAuditStore>();
        auditStore.Should().BeOfType<InMemoryDSRAuditStore>();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataSubjectRights(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_CustomStoreRegisteredBefore_ShouldNotBeOverridden()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDSRRequestStore, InMemoryDSRRequestStore>();

        // Act
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        // Assert — should use the pre-registered store (TryAdd semantics)
        var store = provider.GetRequiredService<IDSRRequestStore>();
        store.Should().BeOfType<InMemoryDSRRequestStore>();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataSubjectRights(options =>
        {
            options.RestrictionEnforcementMode = DSREnforcementMode.Warn;
            options.DefaultDeadlineDays = 45;
            options.MaxExtensionDays = 60;
            options.TrackAuditTrail = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DataSubjectRightsOptions>>().Value;
        options.RestrictionEnforcementMode.Should().Be(DSREnforcementMode.Warn);
        options.DefaultDeadlineDays.Should().Be(45);
        options.MaxExtensionDays.Should().Be(60);
        options.TrackAuditTrail.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_RegistersExportFormatWriters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<JsonExportFormatWriter>().Should().NotBeNull();
        provider.GetService<CsvExportFormatWriter>().Should().NotBeNull();
        provider.GetService<XmlExportFormatWriter>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaDataSubjectRights_ScopedServices_ResolveCorrectly()
    {
        // Arrange — register user-provided dependencies required by default implementations
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IPersonalDataLocator>());
        services.AddSingleton(Substitute.For<IProcessingActivityRegistry>());
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        // Act & Assert — scoped services must resolve within a scope
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetService<IDataSubjectRightsHandler>();
        handler.Should().NotBeNull();

        var erasureExecutor = scope.ServiceProvider.GetService<IDataErasureExecutor>();
        erasureExecutor.Should().NotBeNull();

        var exporter = scope.ServiceProvider.GetService<IDataPortabilityExporter>();
        exporter.Should().NotBeNull();
    }

    #endregion

    #region Full DSR Request Lifecycle

    [Fact]
    public async Task FullLifecycle_SubmitAndTrackRequest_WithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights(options =>
        {
            options.RestrictionEnforcementMode = DSREnforcementMode.Block;
        });
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDSRRequestStore>();

        // Act: Submit request
        var request = DSRRequest.Create(
            Guid.NewGuid().ToString(),
            "subject-lifecycle-test",
            DataSubjectRight.Access,
            DateTimeOffset.UtcNow);

        var createResult = await store.CreateAsync(request);
        createResult.IsRight.Should().BeTrue();

        // Act: Retrieve by ID
        var getResult = await store.GetByIdAsync(request.Id);
        getResult.IsRight.Should().BeTrue();
        var retrieved = (Option<DSRRequest>)getResult;
        retrieved.IsSome.Should().BeTrue();
        var found = (DSRRequest)retrieved;
        found.SubjectId.Should().Be("subject-lifecycle-test");
        found.Status.Should().Be(DSRRequestStatus.Received);

        // Act: Update status to IdentityVerified
        var updateResult = await store.UpdateStatusAsync(request.Id, DSRRequestStatus.IdentityVerified, null);
        updateResult.IsRight.Should().BeTrue();

        // Act: Update to InProgress
        var progressResult = await store.UpdateStatusAsync(request.Id, DSRRequestStatus.InProgress, null);
        progressResult.IsRight.Should().BeTrue();

        // Act: Complete the request
        var completeResult = await store.UpdateStatusAsync(request.Id, DSRRequestStatus.Completed, null);
        completeResult.IsRight.Should().BeTrue();

        // Verify final state
        var finalGet = await store.GetByIdAsync(request.Id);
        var finalOption = (Option<DSRRequest>)finalGet;
        var finalRequest = (DSRRequest)finalOption;
        finalRequest.Status.Should().Be(DSRRequestStatus.Completed);
    }

    [Fact]
    public async Task FullLifecycle_MultipleRequestTypes_TrackedIndependently()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDSRRequestStore>();
        var subjectId = "subject-multi-type";

        // Act: Submit different right types for the same subject
        var rights = new[] { DataSubjectRight.Access, DataSubjectRight.Erasure, DataSubjectRight.Portability };
        foreach (var right in rights)
        {
            var request = DSRRequest.Create(
                Guid.NewGuid().ToString(),
                subjectId,
                right,
                DateTimeOffset.UtcNow);
            await store.CreateAsync(request);
        }

        // Assert: All requests for the subject are tracked
        var subjectRequests = await store.GetBySubjectIdAsync(subjectId);
        subjectRequests.IsRight.Should().BeTrue();
        var list = subjectRequests.RightAsEnumerable().First();
        list.Should().HaveCount(3);
        list.Select(r => r.RightType).Should().Contain(rights);
    }

    [Fact]
    public async Task FullLifecycle_RestrictionEnforcement_BlocksProcessing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights(options =>
        {
            options.RestrictionEnforcementMode = DSREnforcementMode.Block;
        });
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDSRRequestStore>();
        var subjectId = "subject-restriction";

        // Act: Submit a restriction request
        var restrictionRequest = DSRRequest.Create(
            Guid.NewGuid().ToString(),
            subjectId,
            DataSubjectRight.Restriction,
            DateTimeOffset.UtcNow);
        await store.CreateAsync(restrictionRequest);

        // Assert: Subject has active restriction
        var hasRestriction = await store.HasActiveRestrictionAsync(subjectId);
        hasRestriction.IsRight.Should().BeTrue();
        ((bool)hasRestriction).Should().BeTrue();

        // Act: Complete the restriction
        await store.UpdateStatusAsync(restrictionRequest.Id, DSRRequestStatus.Completed, null);

        // Assert: Restriction is lifted
        var afterComplete = await store.HasActiveRestrictionAsync(subjectId);
        ((bool)afterComplete).Should().BeFalse();
    }

    #endregion

    #region Audit Trail Integration

    [Fact]
    public async Task AuditTrail_RecordAndRetrieve_WithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        var auditStore = provider.GetRequiredService<IDSRAuditStore>();
        var requestId = Guid.NewGuid().ToString();

        // Act: Record multiple audit entries
        var entries = new[]
        {
            new DSRAuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                DSRRequestId = requestId,
                Action = "RequestReceived",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                PerformedByUserId = "system"
            },
            new DSRAuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                DSRRequestId = requestId,
                Action = "IdentityVerified",
                OccurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                PerformedByUserId = "admin"
            },
            new DSRAuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                DSRRequestId = requestId,
                Action = "DataErased",
                OccurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                PerformedByUserId = "system",
                Detail = "All personal data erased"
            }
        };

        foreach (var entry in entries)
        {
            var result = await auditStore.RecordAsync(entry);
            result.IsRight.Should().BeTrue();
        }

        // Assert: Full audit trail is preserved
        var trailResult = await auditStore.GetAuditTrailAsync(requestId);
        trailResult.IsRight.Should().BeTrue();
        var trail = trailResult.RightAsEnumerable().First();
        trail.Should().HaveCount(3);
        trail.Select(e => e.Action).Should().ContainInOrder("RequestReceived", "IdentityVerified", "DataErased");
    }

    [Fact]
    public async Task AuditTrail_IsolatedByRequest_NoCrossTalk()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        var auditStore = provider.GetRequiredService<IDSRAuditStore>();
        var requestId1 = Guid.NewGuid().ToString();
        var requestId2 = Guid.NewGuid().ToString();

        // Act: Record entries for different requests
        await auditStore.RecordAsync(new DSRAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            DSRRequestId = requestId1,
            Action = "Action1",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            PerformedByUserId = "system"
        });

        await auditStore.RecordAsync(new DSRAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            DSRRequestId = requestId2,
            Action = "Action2",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            PerformedByUserId = "system"
        });

        // Assert: Each request gets only its own entries
        var trail1 = await auditStore.GetAuditTrailAsync(requestId1);
        trail1.RightAsEnumerable().First().Should().HaveCount(1);
        trail1.RightAsEnumerable().First()[0].Action.Should().Be("Action1");

        var trail2 = await auditStore.GetAuditTrailAsync(requestId2);
        trail2.RightAsEnumerable().First().Should().HaveCount(1);
        trail2.RightAsEnumerable().First()[0].Action.Should().Be("Action2");
    }

    #endregion

    #region Pending and Overdue Requests

    [Fact]
    public async Task PendingRequests_OnlyReturnsNonTerminalRequests()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDSRRequestStore>();

        // Create requests in various statuses
        var receivedRequest = DSRRequest.Create("r1", "s1", DataSubjectRight.Access, DateTimeOffset.UtcNow);
        var completedId = Guid.NewGuid().ToString();
        var completedRequest = DSRRequest.Create(completedId, "s2", DataSubjectRight.Access, DateTimeOffset.UtcNow);
        var rejectedId = Guid.NewGuid().ToString();
        var rejectedRequest = DSRRequest.Create(rejectedId, "s3", DataSubjectRight.Access, DateTimeOffset.UtcNow);

        await store.CreateAsync(receivedRequest);
        await store.CreateAsync(completedRequest);
        await store.CreateAsync(rejectedRequest);

        await store.UpdateStatusAsync(completedId, DSRRequestStatus.Completed, null);
        await store.UpdateStatusAsync(rejectedId, DSRRequestStatus.Rejected, null);

        // Act
        var pending = await store.GetPendingRequestsAsync();
        pending.IsRight.Should().BeTrue();
        var pendingList = pending.RightAsEnumerable().First();

        // Assert: Only non-terminal requests
        pendingList.Should().ContainSingle(r => r.Id == "r1");
        pendingList.Should().NotContain(r => r.Id == completedId);
        pendingList.Should().NotContain(r => r.Id == rejectedId);
    }

    [Fact]
    public async Task OverdueRequests_DetectsExpiredDeadlines()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDSRRequestStore>();

        // Create a request with a past received date (31 days ago = overdue)
        var pastDate = DateTimeOffset.UtcNow.AddDays(-31);
        var overdueRequest = DSRRequest.Create(
            Guid.NewGuid().ToString(),
            "overdue-subject",
            DataSubjectRight.Access,
            pastDate);

        await store.CreateAsync(overdueRequest);

        // Create a recent request (not overdue)
        var recentRequest = DSRRequest.Create(
            Guid.NewGuid().ToString(),
            "recent-subject",
            DataSubjectRight.Access,
            DateTimeOffset.UtcNow);

        await store.CreateAsync(recentRequest);

        // Act
        var overdue = await store.GetOverdueRequestsAsync();
        overdue.IsRight.Should().BeTrue();
        var overdueList = overdue.RightAsEnumerable().First();

        // Assert: Only the overdue request
        overdueList.Should().Contain(r => r.SubjectId == "overdue-subject");
        overdueList.Should().NotContain(r => r.SubjectId == "recent-subject");
    }

    #endregion

    #region Health Check Integration

    [Fact]
    public async Task HealthCheck_EmptyStore_ReturnsHealthy()
    {
        // Arrange — register user-provided dependencies so health check can resolve all services
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IPersonalDataLocator>());
        services.AddEncinaDataSubjectRights(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.Should().Be(HealthStatus.Healthy);
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task ConcurrentAccess_MultipleSubjects_NoDataCorruption()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDSRRequestStore>();
        var subjectCount = 50;

        // Act: Create requests concurrently
        var tasks = Enumerable.Range(0, subjectCount).Select(async i =>
        {
            var request = DSRRequest.Create(
                Guid.NewGuid().ToString(),
                $"concurrent-subject-{i}",
                (DataSubjectRight)(i % 8),
                DateTimeOffset.UtcNow);
            return await store.CreateAsync(request);
        });

        var results = await Task.WhenAll(tasks);

        // Assert: All succeeded without data corruption
        results.Should().AllSatisfy(r => r.IsRight.Should().BeTrue());

        var allRequests = await store.GetAllAsync();
        allRequests.IsRight.Should().BeTrue();
        var all = allRequests.RightAsEnumerable().First();
        all.Count.Should().BeGreaterThanOrEqualTo(subjectCount);
    }

    [Fact]
    public async Task ConcurrentAccess_StatusUpdatesAndReads_Consistent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataSubjectRights();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDSRRequestStore>();
        var requestCount = 20;

        // Seed requests
        var requestIds = new List<string>();
        for (var i = 0; i < requestCount; i++)
        {
            var id = Guid.NewGuid().ToString();
            requestIds.Add(id);
            await store.CreateAsync(DSRRequest.Create(id, $"subject-{i}", DataSubjectRight.Access, DateTimeOffset.UtcNow));
        }

        // Act: Concurrent status updates
        var updateTasks = requestIds.Select(async id =>
        {
            await store.UpdateStatusAsync(id, DSRRequestStatus.IdentityVerified, null);
            await store.UpdateStatusAsync(id, DSRRequestStatus.InProgress, null);
            await store.UpdateStatusAsync(id, DSRRequestStatus.Completed, null);
        });

        await Task.WhenAll(updateTasks);

        // Assert: All should be completed
        foreach (var id in requestIds)
        {
            var result = await store.GetByIdAsync(id);
            var option = (Option<DSRRequest>)result;
            var request = (DSRRequest)option;
            request.Status.Should().Be(DSRRequestStatus.Completed);
        }
    }

    #endregion
}
