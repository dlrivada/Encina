#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Health;
using Encina.Compliance.PrivacyByDesign.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.PrivacyByDesign;

/// <summary>
/// Integration tests for the Privacy by Design pipeline verifying DI registration,
/// options configuration, validator roundtrips, pipeline enforcement behavior,
/// purpose registry operations, health check, and concurrent access patterns
/// using in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PrivacyByDesignPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaPrivacyByDesign_RegistersIDataMinimizationAnalyzer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign();
        var provider = services.BuildServiceProvider();

        var analyzer = provider.GetService<IDataMinimizationAnalyzer>();
        analyzer.Should().NotBeNull();
        analyzer.Should().BeOfType<DefaultDataMinimizationAnalyzer>();
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_RegistersIPurposeRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<IPurposeRegistry>();
        registry.Should().NotBeNull();
        registry.Should().BeOfType<InMemoryPurposeRegistry>();
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_RegistersIPrivacyByDesignValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<IPrivacyByDesignValidator>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<DefaultPrivacyByDesignValidator>();
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign();
        var provider = services.BuildServiceProvider();

        var optionsValidator = provider.GetService<IValidateOptions<PrivacyByDesignOptions>>();
        optionsValidator.Should().NotBeNull();
        optionsValidator.Should().BeOfType<PrivacyByDesignOptionsValidator>();
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign(options =>
        {
            options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
            options.MinimizationScoreThreshold = 0.7;
            options.PrivacyLevel = PrivacyLevel.Maximum;
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PrivacyByDesignOptions>>().Value;
        options.EnforcementMode.Should().Be(PrivacyByDesignEnforcementMode.Block);
        options.MinimizationScoreThreshold.Should().Be(0.7);
        options.PrivacyLevel.Should().Be(PrivacyLevel.Maximum);
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_TryAdd_AllowsCustomImplementation()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom implementation BEFORE AddEncinaPrivacyByDesign
        var customRegistry = new InMemoryPurposeRegistry(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryPurposeRegistry>.Instance);
        services.AddSingleton<IPurposeRegistry>(customRegistry);

        services.AddEncinaPrivacyByDesign();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IPurposeRegistry>();
        registry.Should().BeSameAs(customRegistry, "TryAdd should not override existing registration");
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaPrivacyByDesign_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public async Task HealthCheck_FullyConfigured_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheck = provider.GetRequiredService<IEnumerable<IHealthCheck>>()
            .OfType<PrivacyByDesignHealthCheck>()
            .FirstOrDefault();

        // Even if not found via IEnumerable, resolve directly
        if (healthCheck is null)
        {
            healthCheck = ActivatorUtilities.CreateInstance<PrivacyByDesignHealthCheck>(provider);
        }

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                PrivacyByDesignHealthCheck.DefaultName,
                healthCheck,
                HealthStatus.Unhealthy,
                PrivacyByDesignHealthCheck.Tags)
        };

        var result = await healthCheck.CheckHealthAsync(context);
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("enforcementMode");
        result.Data.Should().ContainKey("privacyLevel");
        result.Data.Should().ContainKey("validatorType");
        result.Data.Should().ContainKey("purposeRegistryType");
    }

    #endregion

    #region Validator End-to-End

    [Fact]
    public async Task Validator_CompliantRequest_ReturnsNoViolations()
    {
        var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();

        var request = new CompliantRequest { ProductId = "P001", Quantity = 5 };
        var result = await validator.ValidateAsync(request);

        result.IsRight.Should().BeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.IsCompliant.Should().BeTrue();
        validation.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task Validator_NonCompliantRequest_ReturnsViolations()
    {
        var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();

        var request = new NonCompliantRequest
        {
            ProductId = "P001",
            ReferralSource = "Google Ads",  // unnecessary field with value
            CampaignCode = "SUMMER2026"     // unnecessary field with value
        };

        var result = await validator.ValidateAsync(request);

        result.IsRight.Should().BeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.IsCompliant.Should().BeFalse();
        validation.Violations.Should().NotBeEmpty();
        validation.MinimizationReport.Should().NotBeNull();
        validation.MinimizationReport!.MinimizationScore.Should().BeLessThan(1.0);
    }

    [Fact]
    public async Task Analyzer_ProducesCorrectMinimizationScore()
    {
        var provider = BuildServiceProvider();
        var analyzer = provider.GetRequiredService<IDataMinimizationAnalyzer>();

        var request = new NonCompliantRequest { ProductId = "P001" };
        var result = await analyzer.AnalyzeAsync(request);

        result.IsRight.Should().BeTrue();
        var report = (MinimizationReport)result;
        // 1 necessary (ProductId) out of 3 total = 1/3
        report.MinimizationScore.Should().BeApproximately(1.0 / 3.0, 0.01);
        report.NecessaryFields.Should().HaveCount(1);
        report.UnnecessaryFields.Should().HaveCount(2);
    }

    #endregion

    #region Purpose Registry End-to-End

    [Fact]
    public async Task PurposeRegistry_RegisterAndRetrieve_Roundtrips()
    {
        var provider = BuildServiceProvider();
        var registry = provider.GetRequiredService<IPurposeRegistry>();

        var purpose = new PurposeDefinition
        {
            PurposeId = Guid.NewGuid().ToString(),
            Name = "Integration Test Purpose",
            Description = "Testing purpose roundtrip",
            LegalBasis = "Contract",
            AllowedFields = ["Field1", "Field2"],
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var registerResult = await registry.RegisterPurposeAsync(purpose);
        registerResult.IsRight.Should().BeTrue();

        var getResult = await registry.GetPurposeAsync("Integration Test Purpose");
        getResult.IsRight.Should().BeTrue();

        var option = getResult.Match(o => o, _ => LanguageExt.Option<PurposeDefinition>.None);
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task PurposeRegistry_ModuleFallback_WorksCorrectly()
    {
        var provider = BuildServiceProvider();
        var registry = provider.GetRequiredService<IPurposeRegistry>();

        var globalPurpose = new PurposeDefinition
        {
            PurposeId = Guid.NewGuid().ToString(),
            Name = "Global Purpose",
            Description = "Globally available",
            LegalBasis = "Consent",
            AllowedFields = ["Name", "Email"],
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await registry.RegisterPurposeAsync(globalPurpose);

        // Lookup with moduleId should fall back to global
        var result = await registry.GetPurposeAsync("Global Purpose", "orders-module");
        result.IsRight.Should().BeTrue();
        var option = result.Match(o => o, _ => LanguageExt.Option<PurposeDefinition>.None);
        option.IsSome.Should().BeTrue("Global purpose should be found via module fallback");
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task Validator_ConcurrentValidations_AllSucceed()
    {
        var provider = BuildServiceProvider();

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            using var scope = provider.CreateScope();
            var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();
            var request = new CompliantRequest { ProductId = $"P{i:D4}", Quantity = i };
            var result = await validator.ValidateAsync(request);
            return result.IsRight;
        });

        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(r => r.Should().BeTrue());
    }

    [Fact]
    public async Task PurposeRegistry_ConcurrentRegistrations_AllSucceed()
    {
        var provider = BuildServiceProvider();
        var registry = provider.GetRequiredService<IPurposeRegistry>();

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var purpose = new PurposeDefinition
            {
                PurposeId = Guid.NewGuid().ToString(),
                Name = $"Concurrent Purpose {i}",
                Description = $"Test {i}",
                LegalBasis = "Contract",
                AllowedFields = [$"Field{i}"],
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            var result = await registry.RegisterPurposeAsync(purpose);
            return result.IsRight;
        });

        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(r => r.Should().BeTrue());
    }

    #endregion

    #region Defaults Validation

    [Fact]
    public async Task Validator_DefaultsCheck_DetectsOverrides()
    {
        var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();

        var request = new RequestWithDefaults
        {
            ShareData = true, // overrides default of false
            MarketingConsent = "opt-in" // overrides default of null
        };

        var result = await validator.ValidateDefaultsAsync(request);
        result.IsRight.Should().BeTrue();

        var defaults = result.Match(d => d, _ => (IReadOnlyList<DefaultPrivacyFieldInfo>)[]);
        defaults.Should().NotBeEmpty();
        defaults.Should().Contain(d => !d.MatchesDefault);
    }

    #endregion

    #region Test Types and Helpers

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign(options =>
        {
            options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
            options.PrivacyLevel = PrivacyLevel.Maximum;
        });
        return services.BuildServiceProvider();
    }

    [EnforceDataMinimization]
    private sealed class CompliantRequest
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }
    }

    [EnforceDataMinimization]
    private sealed class NonCompliantRequest
    {
        public string ProductId { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Analytics only")]
        public string? ReferralSource { get; set; }

        [NotStrictlyNecessary(Reason = "Marketing campaign")]
        public string? CampaignCode { get; set; }
    }

    private sealed class RequestWithDefaults
    {
        [PrivacyDefault(false)]
        public bool ShareData { get; set; }

        [PrivacyDefault(null)]
        public string? MarketingConsent { get; set; }
    }

    #endregion
}
