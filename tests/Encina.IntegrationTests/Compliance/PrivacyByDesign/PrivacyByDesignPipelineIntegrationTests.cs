#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Health;
using Encina.Compliance.PrivacyByDesign.Model;

using Shouldly;

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
        analyzer.ShouldNotBeNull();
        analyzer.ShouldBeOfType<DefaultDataMinimizationAnalyzer>();
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_RegistersIPurposeRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<IPurposeRegistry>();
        registry.ShouldNotBeNull();
        registry.ShouldBeOfType<InMemoryPurposeRegistry>();
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
        validator.ShouldNotBeNull();
        validator.ShouldBeOfType<DefaultPrivacyByDesignValidator>();
    }

    [Fact]
    public void AddEncinaPrivacyByDesign_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPrivacyByDesign();
        var provider = services.BuildServiceProvider();

        var optionsValidator = provider.GetService<IValidateOptions<PrivacyByDesignOptions>>();
        optionsValidator.ShouldNotBeNull();
        optionsValidator.ShouldBeOfType<PrivacyByDesignOptionsValidator>();
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
        options.EnforcementMode.ShouldBe(PrivacyByDesignEnforcementMode.Block);
        options.MinimizationScoreThreshold.ShouldBe(0.7);
        options.PrivacyLevel.ShouldBe(PrivacyLevel.Maximum);
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
        registry.ShouldBeSameAs(customRegistry);
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
        healthCheckService.ShouldNotBeNull();
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
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("enforcementMode");
        result.Data.ShouldContainKey("privacyLevel");
        result.Data.ShouldContainKey("validatorType");
        result.Data.ShouldContainKey("purposeRegistryType");
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

        result.IsRight.ShouldBeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.IsCompliant.ShouldBeTrue();
        validation.Violations.ShouldBeEmpty();
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

        result.IsRight.ShouldBeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.IsCompliant.ShouldBeFalse();
        validation.Violations.ShouldNotBeEmpty();
        validation.MinimizationReport.ShouldNotBeNull();
        validation.MinimizationReport!.MinimizationScore.ShouldBeLessThan(1.0);
    }

    [Fact]
    public async Task Analyzer_ProducesCorrectMinimizationScore()
    {
        var provider = BuildServiceProvider();
        var analyzer = provider.GetRequiredService<IDataMinimizationAnalyzer>();

        var request = new NonCompliantRequest { ProductId = "P001" };
        var result = await analyzer.AnalyzeAsync(request);

        result.IsRight.ShouldBeTrue();
        var report = (MinimizationReport)result;
        // 1 necessary (ProductId) out of 3 total = 1/3
        report.MinimizationScore.ShouldBeInRange(1.0 / 3.0 - 0.01, 1.0 / 3.0 + 0.01);
        report.NecessaryFields.Count.ShouldBe(1);
        report.UnnecessaryFields.Count.ShouldBe(2);
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
        registerResult.IsRight.ShouldBeTrue();

        var getResult = await registry.GetPurposeAsync("Integration Test Purpose");
        getResult.IsRight.ShouldBeTrue();

        var option = getResult.Match(o => o, _ => LanguageExt.Option<PurposeDefinition>.None);
        option.IsSome.ShouldBeTrue();
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
        result.IsRight.ShouldBeTrue();
        var option = result.Match(o => o, _ => LanguageExt.Option<PurposeDefinition>.None);
        option.IsSome.ShouldBeTrue();
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
        results.ShouldAllBe(r => r);
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
        results.ShouldAllBe(r => r);
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
        result.IsRight.ShouldBeTrue();

        var defaults = result.Match(d => d, _ => (IReadOnlyList<DefaultPrivacyFieldInfo>)[]);
        defaults.ShouldNotBeEmpty();
        defaults.ShouldContain(d => !d.MatchesDefault);
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

    #region Pipeline Behavior End-to-End

    [Fact]
    public async Task PipelineBehavior_BlockMode_CompliantRequest_AllowsThrough()
    {
        var provider = BuildPipelineServiceProvider(PrivacyByDesignEnforcementMode.Block);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var command = new PipelineCompliantCommand { ProductId = "P001", Quantity = 5 };
        var result = await encina.Send(command);

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(42));
    }

    [Fact]
    public async Task PipelineBehavior_BlockMode_NonCompliantRequest_BlocksRequest()
    {
        var provider = BuildPipelineServiceProvider(PrivacyByDesignEnforcementMode.Block);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var command = new PipelineNonCompliantCommand
        {
            ProductId = "P001",
            ReferralSource = "Google Ads",
            CampaignCode = "SUMMER2026"
        };
        var result = await encina.Send(command);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task PipelineBehavior_WarnMode_NonCompliantRequest_AllowsThrough()
    {
        var provider = BuildPipelineServiceProvider(PrivacyByDesignEnforcementMode.Warn);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var command = new PipelineNonCompliantCommand
        {
            ProductId = "P001",
            ReferralSource = "Google Ads",
            CampaignCode = "SUMMER2026"
        };
        var result = await encina.Send(command);

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(42));
    }

    [Fact]
    public async Task PipelineBehavior_DisabledMode_SkipsValidationEntirely()
    {
        var provider = BuildPipelineServiceProvider(PrivacyByDesignEnforcementMode.Disabled);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var command = new PipelineNonCompliantCommand
        {
            ProductId = "P001",
            ReferralSource = "Google Ads",
            CampaignCode = "SUMMER2026"
        };
        var result = await encina.Send(command);

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(42));
    }

    [Fact]
    public async Task PipelineBehavior_NoAttribute_SkipsValidation()
    {
        var provider = BuildPipelineServiceProvider(PrivacyByDesignEnforcementMode.Block);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var command = new NoAttributeCommand("test-data");
        var result = await encina.Send(command);

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(99));
    }

    [Fact]
    public async Task PipelineBehavior_ConcurrentRequests_AllProcessedCorrectly()
    {
        var provider = BuildPipelineServiceProvider(PrivacyByDesignEnforcementMode.Block);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 100).Select(async i =>
            {
                using var scope = provider.CreateScope();
                var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                if (i % 2 == 0)
                {
                    var command = new PipelineCompliantCommand { ProductId = $"P{i}", Quantity = i };
                    var result = await encina.Send(command);
                    return (Expected: true, Actual: result.IsRight);
                }
                else
                {
                    var command = new PipelineNonCompliantCommand
                    {
                        ProductId = $"P{i}",
                        ReferralSource = "ad",
                        CampaignCode = "C1"
                    };
                    var result = await encina.Send(command);
                    return (Expected: false, Actual: result.IsRight);
                }
            }));

        foreach (var (expected, actual) in results)
        {
            if (expected)
                actual.ShouldBeTrue();
            else
                actual.ShouldBeFalse();
        }
    }

    private static ServiceProvider BuildPipelineServiceProvider(PrivacyByDesignEnforcementMode mode)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<PrivacyByDesignPipelineIntegrationTests>());

        services.AddEncinaPrivacyByDesign(options =>
        {
            options.EnforcementMode = mode;
            options.PrivacyLevel = PrivacyLevel.Maximum;
        });

        services.AddScoped<IRequestHandler<PipelineCompliantCommand, int>, PipelineCompliantHandler>();
        services.AddScoped<IRequestHandler<PipelineNonCompliantCommand, int>, PipelineNonCompliantHandler>();
        services.AddScoped<IRequestHandler<NoAttributeCommand, int>, NoAttributeHandler>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    [EnforceDataMinimization]
    private sealed class PipelineCompliantCommand : IRequest<int>
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }
    }

    private sealed class PipelineCompliantHandler : IRequestHandler<PipelineCompliantCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(PipelineCompliantCommand request, CancellationToken cancellationToken)
            => Task.FromResult(LanguageExt.Prelude.Right<EncinaError, int>(42));
    }

    [EnforceDataMinimization]
    private sealed class PipelineNonCompliantCommand : IRequest<int>
    {
        public string ProductId { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Analytics only")]
        public string? ReferralSource { get; set; }

        [NotStrictlyNecessary(Reason = "Marketing campaign")]
        public string? CampaignCode { get; set; }
    }

    private sealed class PipelineNonCompliantHandler : IRequestHandler<PipelineNonCompliantCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(PipelineNonCompliantCommand request, CancellationToken cancellationToken)
            => Task.FromResult(LanguageExt.Prelude.Right<EncinaError, int>(42));
    }

    private sealed record NoAttributeCommand(string Data) : IRequest<int>;

    private sealed class NoAttributeHandler : IRequestHandler<NoAttributeCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoAttributeCommand request, CancellationToken cancellationToken)
            => Task.FromResult(LanguageExt.Prelude.Right<EncinaError, int>(99));
    }

    #endregion
}
