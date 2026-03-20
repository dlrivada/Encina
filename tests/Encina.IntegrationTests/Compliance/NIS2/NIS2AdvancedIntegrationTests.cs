#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using Encina.Caching;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;
using Encina.Security.Encryption.Abstractions;
using Encina.Testing.Fakes.Providers;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Polly;
using Polly.Registry;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Compliance.NIS2;

/// <summary>
/// Advanced integration tests for NIS2 compliance verifying cross-cutting integrations:
/// caching with <see cref="FakeCacheProvider"/>, resilience with real Polly pipelines,
/// multi-tenancy via <see cref="IRequestContext"/>, breach notification forwarding,
/// and encryption infrastructure verification via <see cref="IKeyProvider"/>.
/// All tests use real DI wiring — no mocks for the NIS2 services themselves.
/// </summary>
[Trait("Category", "Integration")]
public sealed class NIS2AdvancedIntegrationTests
{
    private static NIS2Options CreateFullyCompliantOptions() => new()
    {
        EntityType = NIS2EntityType.Essential,
        Sector = NIS2Sector.DigitalInfrastructure,
        EnforcementMode = NIS2EnforcementMode.Block,
        HasRiskAnalysisPolicy = true,
        HasIncidentHandlingProcedures = true,
        HasBusinessContinuityPlan = true,
        HasNetworkSecurityPolicy = true,
        HasEffectivenessAssessment = true,
        HasCyberHygieneProgram = true,
        HasHumanResourcesSecurity = true,
        EnforceMFA = true,
        EnforceEncryption = true,
        ComplianceCacheTTL = TimeSpan.FromMinutes(5)
    };

    private static ServiceProvider BuildProvider(
        Action<NIS2Options>? configure = null,
        FakeCacheProvider? cacheProvider = null,
        IRequestContext? requestContext = null,
        IBreachNotificationService? breachService = null,
        IKeyProvider? keyProvider = null,
        ResiliencePipelineRegistry<string>? pipelineRegistry = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Cross-cutting integrations (optional)
        if (cacheProvider is not null)
            services.AddSingleton<ICacheProvider>(cacheProvider);

        if (requestContext is not null)
            services.AddScoped<IRequestContext>(_ => requestContext);

        if (breachService is not null)
            services.AddSingleton(breachService);

        if (keyProvider is not null)
            services.AddSingleton(keyProvider);

        if (pipelineRegistry is not null)
            services.AddSingleton<ResiliencePipelineProvider<string>>(pipelineRegistry);

        services.AddEncinaNIS2(options =>
        {
            var defaults = CreateFullyCompliantOptions();
            options.EntityType = defaults.EntityType;
            options.Sector = defaults.Sector;
            options.EnforcementMode = defaults.EnforcementMode;
            options.HasRiskAnalysisPolicy = defaults.HasRiskAnalysisPolicy;
            options.HasIncidentHandlingProcedures = defaults.HasIncidentHandlingProcedures;
            options.HasBusinessContinuityPlan = defaults.HasBusinessContinuityPlan;
            options.HasNetworkSecurityPolicy = defaults.HasNetworkSecurityPolicy;
            options.HasEffectivenessAssessment = defaults.HasEffectivenessAssessment;
            options.HasCyberHygieneProgram = defaults.HasCyberHygieneProgram;
            options.HasHumanResourcesSecurity = defaults.HasHumanResourcesSecurity;
            options.EnforceMFA = defaults.EnforceMFA;
            options.EnforceEncryption = defaults.EnforceEncryption;
            options.ComplianceCacheTTL = defaults.ComplianceCacheTTL;
            options.CompetentAuthority = "csirt@test.eu";
            options.EncryptedDataCategories.Add("PII");
            options.EncryptedEndpoints.Add("https://api.test.com");
            options.AddSupplier("test-supplier", s =>
            {
                s.Name = "Test";
                s.RiskLevel = SupplierRiskLevel.Low;
                s.LastAssessmentAtUtc = DateTimeOffset.UtcNow;
                s.CertificationStatus = "ISO 27001";
            });
            options.ManagementAccountability = new ManagementAccountabilityRecord
            {
                ResponsiblePerson = "Jane Doe",
                Role = "CISO",
                AcknowledgedAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
                ComplianceAreas = ["Risk Analysis", "Incident Handling", "Supply Chain"],
                TrainingCompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-15)
            };

            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    #region Caching Integration

    [Fact]
    public async Task ValidateAsync_WithCacheProvider_CachesResult()
    {
        // Arrange
        var cache = new FakeCacheProvider();
        var provider = BuildProvider(cacheProvider: cache);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act
        var result = await validator.ValidateAsync();

        // Assert — result is Right and cache was written
        result.IsRight.ShouldBeTrue();
        cache.CachedKeys.ShouldContain(k => k.StartsWith("nis2:compliance:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_SecondCall_ReturnsCachedResult()
    {
        // Arrange
        var cache = new FakeCacheProvider();
        var provider = BuildProvider(cacheProvider: cache);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act — call twice
        var result1 = await validator.ValidateAsync();
        var result2 = await validator.ValidateAsync();

        // Assert — both succeed, cache was hit on second call
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        // The cache key should have been read twice (miss + hit) and written once
        var cacheKey = cache.CachedKeys.First(k => k.StartsWith("nis2:compliance:", StringComparison.Ordinal));
        cache.GetRequestCount(cacheKey).ShouldBe(2, "should have 2 get operations (miss + hit)");
        cache.CachedKeys.Count(k => k == cacheKey).ShouldBe(1, "should have cached only once");
    }

    [Fact]
    public async Task ValidateAsync_ZeroCacheTTL_DoesNotCache()
    {
        // Arrange — disable caching via zero TTL
        var cache = new FakeCacheProvider();
        var provider = BuildProvider(
            configure: o => o.ComplianceCacheTTL = TimeSpan.Zero,
            cacheProvider: cache);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act
        await validator.ValidateAsync();

        // Assert — no cache operations
        cache.CachedKeys.ShouldBeEmpty();
        cache.GetOperations.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_NoCacheProvider_StillSucceeds()
    {
        // Arrange — no cache registered
        var provider = BuildProvider();

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act
        var result = await validator.ValidateAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_CacheProviderFails_StillSucceeds()
    {
        // Arrange — cache provider that throws on all operations
        var cache = new FakeCacheProvider { SimulateErrors = true };
        var provider = BuildProvider(cacheProvider: cache);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act — should succeed despite cache failure (resilience catches it)
        var result = await validator.ValidateAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Multi-Tenancy Integration

    [Fact]
    public async Task ValidateAsync_WithTenantId_CacheKeyIncludesTenant()
    {
        // Arrange
        var cache = new FakeCacheProvider();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.TenantId.Returns("tenant-42");
        var provider = BuildProvider(cacheProvider: cache, requestContext: requestContext);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act
        await validator.ValidateAsync();

        // Assert — cache key contains tenant ID
        cache.CachedKeys.ShouldContain(k => k.Contains("tenant-42", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_DifferentTenants_DifferentCacheKeys()
    {
        // Arrange
        var cache = new FakeCacheProvider();

        // Tenant A
        var ctxA = Substitute.For<IRequestContext>();
        ctxA.TenantId.Returns("tenant-A");
        var providerA = BuildProvider(cacheProvider: cache, requestContext: ctxA);

        using (var scope = providerA.CreateScope())
        {
            var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();
            await validator.ValidateAsync();
        }

        // Tenant B — use same cache, different tenant context
        var ctxB = Substitute.For<IRequestContext>();
        ctxB.TenantId.Returns("tenant-B");
        var providerB = BuildProvider(cacheProvider: cache, requestContext: ctxB);

        using (var scope = providerB.CreateScope())
        {
            var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();
            await validator.ValidateAsync();
        }

        // Assert — two distinct cache keys
        var nis2Keys = cache.CachedKeys
            .Where(k => k.StartsWith("nis2:compliance:", StringComparison.Ordinal))
            .Distinct()
            .ToList();
        nis2Keys.Count.ShouldBeGreaterThanOrEqualTo(2);
        nis2Keys.ShouldContain(k => k.Contains("tenant-A", StringComparison.Ordinal));
        nis2Keys.ShouldContain(k => k.Contains("tenant-B", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_NoTenantId_UsesGlobalCacheKey()
    {
        // Arrange — no IRequestContext registered
        var cache = new FakeCacheProvider();
        var provider = BuildProvider(cacheProvider: cache);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act
        await validator.ValidateAsync();

        // Assert — cache key does not contain tenant prefix
        var cacheKey = cache.CachedKeys.First(k => k.StartsWith("nis2:compliance:", StringComparison.Ordinal));
        cacheKey.ShouldNotContain("tenant");
    }

    #endregion

    #region Resilience Integration

    [Fact]
    public async Task ValidateAsync_WithPollyPipeline_UsesRegisteredPipeline()
    {
        // Arrange — register a real Polly pipeline with a generous timeout
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(NIS2ResilienceHelper.PipelineKey,
            (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(30)));

        var cache = new FakeCacheProvider();
        var provider = BuildProvider(cacheProvider: cache, pipelineRegistry: registry);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act — validation + cache write both go through the pipeline
        var result = await validator.ValidateAsync();

        // Assert — succeeded through the pipeline
        result.IsRight.ShouldBeTrue();
        cache.CachedKeys.ShouldContain(k => k.StartsWith("nis2:compliance:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithoutPollyPipeline_FallsBackToTimeout()
    {
        // Arrange — no Polly pipeline, but cache is registered
        var cache = new FakeCacheProvider();
        var provider = BuildProvider(
            configure: o => o.ExternalCallTimeout = TimeSpan.FromSeconds(10),
            cacheProvider: cache);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        // Act
        var result = await validator.ValidateAsync();

        // Assert — succeeded via timeout fallback path
        result.IsRight.ShouldBeTrue();
        cache.CachedKeys.ShouldContain(k => k.StartsWith("nis2:compliance:", StringComparison.Ordinal));
    }

    #endregion

    #region Breach Notification Forwarding

    [Fact]
    public async Task ReportIncidentAsync_WithBreachService_ForwardsToBreachNotification()
    {
        // Arrange
        var breachService = Substitute.For<IBreachNotificationService>();
        breachService.RecordBreachAsync(
            Arg.Any<string>(),
            Arg.Any<BreachSeverity>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Guid>>(
                Right<EncinaError, Guid>(Guid.NewGuid())));

        var provider = BuildProvider(breachService: breachService);

        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<INIS2IncidentHandler>();

        var incident = NIS2Incident.Create(
            "Data breach detected",
            NIS2IncidentSeverity.Critical,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["service-a"],
            "Unauthorized access detected");

        // Act
        var result = await handler.ReportIncidentAsync(incident);

        // Assert
        result.IsRight.ShouldBeTrue();
        await breachService.Received(1).RecordBreachAsync(
            Arg.Is<string>(s => s.Contains("Data breach detected", StringComparison.Ordinal)),
            BreachSeverity.Critical,
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportIncidentAsync_WithoutBreachService_StillSucceeds()
    {
        // Arrange — no IBreachNotificationService registered
        var provider = BuildProvider();

        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<INIS2IncidentHandler>();

        var incident = NIS2Incident.Create(
            "Minor incident",
            NIS2IncidentSeverity.Low,
            DateTimeOffset.UtcNow,
            isSignificant: false,
            ["service-b"],
            "Low-priority alert");

        // Act
        var result = await handler.ReportIncidentAsync(incident);

        // Assert — succeeds without breach forwarding
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ReportIncidentAsync_BreachServiceFails_StillReportsSuccessfully()
    {
        // Arrange — breach service that throws
        var breachService = Substitute.For<IBreachNotificationService>();
        breachService.RecordBreachAsync(
            Arg.Any<string>(),
            Arg.Any<BreachSeverity>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Breach service unavailable"));

        var provider = BuildProvider(breachService: breachService);

        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<INIS2IncidentHandler>();

        var incident = NIS2Incident.Create(
            "Critical incident",
            NIS2IncidentSeverity.High,
            DateTimeOffset.UtcNow,
            isSignificant: true,
            ["service-c"],
            "Service failure");

        // Act — should succeed despite breach service failure (resilience catches it)
        var result = await handler.ReportIncidentAsync(incident);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Encryption Infrastructure Verification

    [Fact]
    public async Task ValidateEncryptionPolicy_WithActiveKeyProvider_ReturnsTrue()
    {
        // Arrange — IKeyProvider with active key
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, string>>(
                Right<EncinaError, string>("active-key-123")));

        var provider = BuildProvider(keyProvider: keyProvider);
        var encryptionValidator = provider.GetRequiredService<IEncryptionValidator>();

        // Act
        var result = await encryptionValidator.ValidateEncryptionPolicyAsync();

        // Assert — config + active key = true
        result.IsRight.ShouldBeTrue();
        var hasPolicy = result.Match(r => r, _ => false);
        hasPolicy.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateEncryptionPolicy_KeyProviderReturnsEmpty_ReturnsFalse()
    {
        // Arrange — IKeyProvider returns empty key (no active key)
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, string>>(
                Right<EncinaError, string>(string.Empty)));

        var provider = BuildProvider(keyProvider: keyProvider);
        var encryptionValidator = provider.GetRequiredService<IEncryptionValidator>();

        // Act
        var result = await encryptionValidator.ValidateEncryptionPolicyAsync();

        // Assert — config exists but no active key = false
        result.IsRight.ShouldBeTrue();
        var hasPolicy = result.Match(r => r, _ => true);
        hasPolicy.ShouldBeFalse("IKeyProvider has no active key — infrastructure mismatch");
    }

    [Fact]
    public async Task ValidateEncryptionPolicy_KeyProviderThrows_ReturnsFalse()
    {
        // Arrange — IKeyProvider throws (resilience catches)
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Key vault down"));

        var provider = BuildProvider(keyProvider: keyProvider);
        var encryptionValidator = provider.GetRequiredService<IEncryptionValidator>();

        // Act
        var result = await encryptionValidator.ValidateEncryptionPolicyAsync();

        // Assert — exception caught by resilience, treated as no active key
        result.IsRight.ShouldBeTrue();
        var hasPolicy = result.Match(r => r, _ => true);
        hasPolicy.ShouldBeFalse("key provider exception should be caught by resilience");
    }

    [Fact]
    public async Task ValidateEncryptionPolicy_NoKeyProvider_ReturnsConfigOnly()
    {
        // Arrange — no IKeyProvider registered but config has categories
        var provider = BuildProvider();
        var encryptionValidator = provider.GetRequiredService<IEncryptionValidator>();

        // Act
        var result = await encryptionValidator.ValidateEncryptionPolicyAsync();

        // Assert — config-only validation (categories exist → true)
        result.IsRight.ShouldBeTrue();
        var hasPolicy = result.Match(r => r, _ => false);
        hasPolicy.ShouldBeTrue("config-only validation when no IKeyProvider");
    }

    #endregion

    #region Full Compliance Lifecycle

    [Fact]
    public async Task FullLifecycle_ValidateAndReportIncident_WithAllIntegrations()
    {
        // Arrange — all cross-cutting services active
        var cache = new FakeCacheProvider();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.TenantId.Returns("tenant-lifecycle");

        var breachService = Substitute.For<IBreachNotificationService>();
        breachService.RecordBreachAsync(
            Arg.Any<string>(),
            Arg.Any<BreachSeverity>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Guid>>(
                Right<EncinaError, Guid>(Guid.NewGuid())));

        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, string>>(
                Right<EncinaError, string>("master-key-v1")));

        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(NIS2ResilienceHelper.PipelineKey,
            (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(30)));

        var provider = BuildProvider(
            cacheProvider: cache,
            requestContext: requestContext,
            breachService: breachService,
            keyProvider: keyProvider,
            pipelineRegistry: registry);

        // Act 1 — Validate compliance (cold cache)
        NIS2ComplianceResult complianceResult;
        using (var scope = provider.CreateScope())
        {
            var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();
            var result = await validator.ValidateAsync();
            result.IsRight.ShouldBeTrue();
            complianceResult = result.Match(r => r, _ => throw new InvalidOperationException());
        }

        // Assert 1 — fully compliant and cached with tenant key
        complianceResult.IsCompliant.ShouldBeTrue();
        complianceResult.CompliancePercentage.ShouldBe(100);
        cache.CachedKeys.ShouldContain(k => k.Contains("tenant-lifecycle", StringComparison.Ordinal));

        // Act 2 — Validate again (cache hit)
        using (var scope = provider.CreateScope())
        {
            var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();
            var result = await validator.ValidateAsync();
            result.IsRight.ShouldBeTrue();
        }

        // Assert 2 — cache was hit
        var cacheKey = cache.CachedKeys.First(k => k.Contains("tenant-lifecycle", StringComparison.Ordinal));
        cache.GetRequestCount(cacheKey).ShouldBeGreaterThanOrEqualTo(2);

        // Act 3 — Report incident (forwarded to breach notification)
        using (var scope = provider.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<INIS2IncidentHandler>();
            var incident = NIS2Incident.Create(
                "Security breach",
                NIS2IncidentSeverity.Critical,
                DateTimeOffset.UtcNow,
                isSignificant: true,
                ["service-a"],
                "Full lifecycle test");

            var result = await handler.ReportIncidentAsync(incident);
            result.IsRight.ShouldBeTrue();
        }

        // Assert 3 — breach notification was forwarded
        await breachService.Received(1).RecordBreachAsync(
            Arg.Any<string>(),
            BreachSeverity.Critical,
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
