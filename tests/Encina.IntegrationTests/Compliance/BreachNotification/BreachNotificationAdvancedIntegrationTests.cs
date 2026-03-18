using Encina.Caching;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Testing.Fakes.Providers;

using Marten;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.BreachNotification;

/// <summary>
/// Advanced integration tests for breach notification features:
/// - DI registration and service resolution
/// - Full lifecycle via service interface (create → query → update → verify state)
/// - Projection produces correct read model from event stream
/// - Event stream audit trail (GetHistoryAsync)
/// - Cache invalidation on write operations
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class BreachNotificationAdvancedIntegrationTests
{
    private readonly MartenFixture _fixture;

    public BreachNotificationAdvancedIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    private ServiceProvider BuildServiceProvider(
        Action<BreachNotificationOptions>? configure = null,
        FakeCacheProvider? cacheProvider = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());

        services.AddEncinaMarten();

        services.AddEncinaBreachNotification(configure ?? (options =>
        {
            options.AddHealthCheck = true;
        }));
        services.AddBreachNotificationAggregates();

        services.AddSingleton<ICacheProvider>(cacheProvider ?? new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        return services.BuildServiceProvider();
    }

    #region DI Registration

    [Fact]
    public void AddEncinaBreachNotification_RegistersAllServices()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider();

        // Assert
        provider.GetService<IBreachNotificationService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddBreachNotificationAggregates_RegistersRepositories()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IAggregateRepository<BreachAggregate>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaBreachNotification_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider(options =>
        {
            options.AddHealthCheck = true;
        });

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.ShouldNotBeNull();
    }

    #endregion

    #region Full Lifecycle via Service Interface

    [Fact]
    public async Task Service_RecordBreach_PersistsToMartenAndReturnsId()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();

        // Act
        var result = await service.RecordBreachAsync(
            "unauthorized access", BreachSeverity.High, "UnauthorizedAccessRule",
            500, "Mass unauthorized access detected", "user-1", "tenant-1", "compliance");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(id => id.ShouldNotBe(Guid.Empty));
    }

    [Fact]
    public async Task Service_RecordAndGetBreach_ReturnsReadModel()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid breachId;

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.RecordBreachAsync(
                "data exfiltration", BreachSeverity.Medium, "MassDataExfiltrationRule",
                200, "Bulk export detected", "sec-system");
            breachId = result.Match(id => id, _ => throw new InvalidOperationException("Create failed"));
        }

        // Act — get breach (fresh scope)
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var getResult = await service.GetBreachAsync(breachId);

            // Assert
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(breachId);
                rm.Nature.ShouldBe("data exfiltration");
                rm.Severity.ShouldBe(BreachSeverity.Medium);
                rm.Status.ShouldBe(BreachStatus.Detected);
                rm.EstimatedAffectedSubjects.ShouldBe(200);
                rm.DetectedByRule.ShouldBe("MassDataExfiltrationRule");
            });
        }
    }

    [Fact]
    public async Task Service_FullLifecycle_Detected_To_Closed()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid breachId;

        // Step 1: Record breach
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.RecordBreachAsync(
                "ransomware", BreachSeverity.Critical, "ManualDetection",
                10000, "Ransomware attack", "incident-team", "tenant-1");
            breachId = result.Match(id => id, _ => throw new InvalidOperationException());
        }

        // Step 2: Assess breach
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.AssessBreachAsync(
                breachId, BreachSeverity.Critical, 15000, "Full system compromise confirmed", "assessor-1");
            result.IsRight.ShouldBeTrue();
        }

        // Step 3: Report to DPA
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.ReportToDPAAsync(
                breachId, "AEPD", "aepd@aepd.es", "Emergency notification per Art. 33", "dpo-1");
            result.IsRight.ShouldBeTrue();
        }

        // Step 4: Notify subjects
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.NotifySubjectsAsync(
                breachId, 15000, "email", SubjectNotificationExemption.None, "comms-1");
            result.IsRight.ShouldBeTrue();
        }

        // Step 5: Contain breach
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.ContainBreachAsync(
                breachId, "Systems isolated, credentials rotated", "ops-1");
            result.IsRight.ShouldBeTrue();
        }

        // Step 6: Close breach
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.CloseBreachAsync(
                breachId, "Root cause: phishing. Remediation: MFA enforced", "dpo-1");
            result.IsRight.ShouldBeTrue();
        }

        // Verify final state
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var getResult = await service.GetBreachAsync(breachId);
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Status.ShouldBe(BreachStatus.Closed);
                rm.Severity.ShouldBe(BreachSeverity.Critical);
                rm.EstimatedAffectedSubjects.ShouldBe(15000);
                rm.AuthorityName.ShouldBe("AEPD");
                rm.SubjectCount.ShouldBe(15000);
                rm.ContainmentMeasures.ShouldBe("Systems isolated, credentials rotated");
                rm.ResolutionSummary.ShouldBe("Root cause: phishing. Remediation: MFA enforced");
                rm.ClosedAtUtc.ShouldNotBeNull();
            });
        }
    }

    #endregion

    #region Projection Produces Correct Read Models

    [Fact]
    public async Task Service_GetBreach_ReturnsReadModelWithAllFieldsMapped()
    {
        // Arrange — create a breach and progress through lifecycle
        using var provider = BuildServiceProvider();
        Guid breachId;

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.RecordBreachAsync(
                "projection test", BreachSeverity.High, "ProjectionRule",
                800, "Test projection mapping", "user-proj", "tenant-proj", "mod-proj");
            breachId = result.Match(id => id, _ => throw new InvalidOperationException());
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service.AssessBreachAsync(
                breachId, BreachSeverity.Critical, 1200, "Upgraded to critical", "assessor-proj");
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service.ReportToDPAAsync(
                breachId, "ICO", "ico@ico.org.uk", "Formal report", "dpo-proj");
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service.AddPhasedReportAsync(breachId, "Phase 1: forensic analysis complete", "analyst-1");
        }

        // Act — get read model
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var getResult = await service.GetBreachAsync(breachId);

            // Assert — all fields mapped correctly
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(breachId);
                rm.Nature.ShouldBe("projection test");
                rm.Severity.ShouldBe(BreachSeverity.Critical);
                rm.Status.ShouldBe(BreachStatus.AuthorityNotified);
                rm.DetectedByRule.ShouldBe("ProjectionRule");
                rm.EstimatedAffectedSubjects.ShouldBe(1200);
                rm.Description.ShouldBe("Test projection mapping");
                rm.AuthorityName.ShouldBe("ICO");
                rm.AssessmentSummary.ShouldBe("Upgraded to critical");
                rm.TenantId.ShouldBe("tenant-proj");
                rm.ModuleId.ShouldBe("mod-proj");
                rm.PhasedReports.Count.ShouldBe(1);
                rm.PhasedReports[0].PhaseNumber.ShouldBe(1);
                rm.PhasedReports[0].ReportContent.ShouldBe("Phase 1: forensic analysis complete");
                rm.DetectedAtUtc.ShouldNotBe(default);
                rm.DeadlineUtc.ShouldNotBe(default);
                rm.AssessedAtUtc.ShouldNotBeNull();
                rm.ReportedToDPAAtUtc.ShouldNotBeNull();
                rm.LastModifiedAtUtc.ShouldNotBe(default);
            });
        }
    }

    #endregion

    #region Service Error Handling

    [Fact]
    public async Task Service_GetNonExistentBreach_ReturnsError()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();

        // Act
        var result = await service.GetBreachAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Service_AssessNonExistentBreach_ReturnsError()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();

        // Act
        var result = await service.AssessBreachAsync(
            Guid.NewGuid(), BreachSeverity.High, 100, "summary", "user-1");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Cache Invalidation on Write Operations

    [Fact]
    public async Task Service_ModifyBreach_InvalidatesCache()
    {
        // Arrange
        var fakeCache = new FakeCacheProvider();
        using var provider = BuildServiceProvider(cacheProvider: fakeCache);
        Guid breachId;

        // Create breach
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.RecordBreachAsync(
                "cache test", BreachSeverity.Low, "CacheRule",
                10, "Cache invalidation test", "user-cache");
            breachId = result.Match(id => id, _ => throw new InvalidOperationException());
        }

        // First read — should populate cache
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var getResult = await service.GetBreachAsync(breachId);
            getResult.IsRight.ShouldBeTrue();
        }

        // Cache should have been populated
        var cachedKeysBefore = fakeCache.CachedKeys.ToList();
        cachedKeysBefore.ShouldContain(k => k.Contains(breachId.ToString()));

        // Act — modify breach (should invalidate cache)
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service.AssessBreachAsync(
                breachId, BreachSeverity.Medium, 20, "Upgraded", "assessor-cache");
        }

        // Assert — cache was invalidated (key was removed)
        var removedKeys = fakeCache.RemovedKeys.ToList();
        removedKeys.ShouldContain(k => k.Contains(breachId.ToString()));
    }

    [Fact]
    public async Task Service_SecondRead_AfterModification_ReturnsUpdatedData()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid breachId;

        // Create
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.RecordBreachAsync(
                "refresh test", BreachSeverity.Low, "RefreshRule",
                5, "Refresh data test", "user-refresh");
            breachId = result.Match(id => id, _ => throw new InvalidOperationException());
        }

        // First read — Detected status
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var r = await service.GetBreachAsync(breachId);
            r.IfRight(rm => rm.Status.ShouldBe(BreachStatus.Detected));
        }

        // Modify — assess
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service.AssessBreachAsync(
                breachId, BreachSeverity.High, 50, "Severity upgraded", "assessor-refresh");
        }

        // Act — second read should see updated state
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var r = await service.GetBreachAsync(breachId);

            // Assert
            r.IsRight.ShouldBeTrue();
            r.IfRight(rm =>
            {
                rm.Status.ShouldBe(BreachStatus.Investigating);
                rm.Severity.ShouldBe(BreachSeverity.High);
                rm.AssessmentSummary.ShouldBe("Severity upgraded");
            });
        }
    }

    #endregion

    #region Query Operations

    [Fact]
    public async Task Service_GetBreachesByStatus_ReturnsFilteredResults()
    {
        // Arrange
        using var provider = BuildServiceProvider();

        // Create two breaches in Detected status
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service.RecordBreachAsync(
                "status-filter-1", BreachSeverity.Low, "Rule1", 10, "Test 1", "user-1");
            await service.RecordBreachAsync(
                "status-filter-2", BreachSeverity.Medium, "Rule2", 20, "Test 2", "user-2");
        }

        // Act
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.GetBreachesByStatusAsync(BreachStatus.Detected);

            // Assert — should find at least the 2 we created
            result.IsRight.ShouldBeTrue();
            result.IfRight(breaches =>
            {
                breaches.Count.ShouldBeGreaterThanOrEqualTo(2);
                breaches.ShouldAllBe(b => b.Status == BreachStatus.Detected);
            });
        }
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void Options_ConfiguresCorrectly()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider(options =>
        {
            options.EnforcementMode = BreachDetectionEnforcementMode.Block;
            options.NotificationDeadlineHours = 48;
            options.AutoNotifyOnHighSeverity = true;
        });

        // Assert
        var opts = provider.GetRequiredService<IOptions<BreachNotificationOptions>>().Value;
        opts.EnforcementMode.ShouldBe(BreachDetectionEnforcementMode.Block);
        opts.NotificationDeadlineHours.ShouldBe(48);
        opts.AutoNotifyOnHighSeverity.ShouldBeTrue();
    }

    #endregion
}
