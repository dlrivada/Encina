using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Health;
using Encina.Compliance.AIAct.Model;
using Encina.Compliance.AIAct.Notifications;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

namespace Encina.GuardTests.Compliance.AIAct;

/// <summary>
/// Guard tests for the AI Act services not covered by <see cref="AIActGuardTests"/>: null arguments are
/// rejected with the right parameter name, and valid arguments run the real code path.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Feature", "AIAct")]
public sealed class AIActGuardExtendedTests
{
    [HighRiskAI(Category = AISystemCategory.EmploymentWorkersManagement, SystemId = "guard-scan-system", Provider = "Acme", Version = "1.0", Description = "Guard scan")]
    private sealed record ScannedRequest : ICommand<Unit>;

    [HighRiskAI(Category = AISystemCategory.EmploymentWorkersManagement, SystemId = "guard-compliant")]
    private sealed record CompliantRequest : ICommand<Unit>;

    private readonly FakeTimeProvider _time = new();

    private InMemoryAISystemRegistry NewRegistry() => new(_time);

    private static AISystemRegistration Registration(string id, AIRiskLevel level = AIRiskLevel.HighRisk) => new()
    {
        SystemId = id,
        Name = id,
        Category = AISystemCategory.EmploymentWorkersManagement,
        RiskLevel = level,
        RegisteredAtUtc = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero)
    };

    // -- DefaultAIActDocumentation --

    [Fact]
    public void Documentation_Constructor_NullRegistry_Throws()
    {
        var act = () => new DefaultAIActDocumentation(null!, _time);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("registry");
    }

    [Fact]
    public void Documentation_Constructor_NullTimeProvider_Throws()
    {
        var act = () => new DefaultAIActDocumentation(NewRegistry(), null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task Documentation_GenerateDocumentationAsync_NullSystemId_Throws()
    {
        var sut = new DefaultAIActDocumentation(NewRegistry(), _time);
        var act = () => sut.GenerateDocumentationAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("systemId");
    }

    [Fact]
    public async Task Documentation_UpdateDocumentationAsync_NullSystemId_Throws()
    {
        var sut = new DefaultAIActDocumentation(NewRegistry(), _time);
        var documentation = new TechnicalDocumentation { SystemId = "s", Description = "d", GeneratedAtUtc = _time.GetUtcNow() };
        var act = () => sut.UpdateDocumentationAsync(null!, documentation).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("systemId");
    }

    [Fact]
    public async Task Documentation_UpdateDocumentationAsync_NullDocumentation_Throws()
    {
        var sut = new DefaultAIActDocumentation(NewRegistry(), _time);
        var act = () => sut.UpdateDocumentationAsync("s", null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("documentation");
    }

    [Fact]
    public async Task Documentation_RegisteredSystem_GeneratesAndUpdates()
    {
        var registry = NewRegistry();
        (await registry.RegisterSystemAsync(Registration("doc-sys"))).IsRight.ShouldBeTrue();
        var sut = new DefaultAIActDocumentation(registry, _time);

        var generated = await sut.GenerateDocumentationAsync("doc-sys");
        var documentation = (TechnicalDocumentation)generated;
        var updated = await sut.UpdateDocumentationAsync("doc-sys", documentation);

        documentation.SystemId.ShouldBe("doc-sys");
        documentation.GeneratedAtUtc.ShouldBe(_time.GetUtcNow());
        updated.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Documentation_UnknownSystem_ReturnsLeft()
    {
        var sut = new DefaultAIActDocumentation(NewRegistry(), _time);

        (await sut.GenerateDocumentationAsync("missing")).IsLeft.ShouldBeTrue();
    }

    // -- DefaultDataQualityValidator --

    [Fact]
    public void DataQuality_Constructor_NullTimeProvider_Throws()
    {
        var act = () => new DefaultDataQualityValidator(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task DataQuality_ValidateTrainingDataAsync_NullDatasetId_Throws()
    {
        var sut = new DefaultDataQualityValidator(_time);
        var act = () => sut.ValidateTrainingDataAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("datasetId");
    }

    [Fact]
    public async Task DataQuality_DetectBiasAsync_NullDatasetId_Throws()
    {
        var sut = new DefaultDataQualityValidator(_time);
        var act = () => sut.DetectBiasAsync(null!, ["gender"]).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("datasetId");
    }

    [Fact]
    public async Task DataQuality_DetectBiasAsync_NullProtectedAttributes_Throws()
    {
        var sut = new DefaultDataQualityValidator(_time);
        var act = () => sut.DetectBiasAsync("ds", null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("protectedAttributes");
    }

    [Fact]
    public async Task DataQuality_ValidArguments_ReturnReports()
    {
        var sut = new DefaultDataQualityValidator(_time);

        var quality = await sut.ValidateTrainingDataAsync("ds-1");
        var bias = await sut.DetectBiasAsync("ds-1", ["gender", "age"]);

        ((DataQualityReport)quality).MeetsAIActRequirements.ShouldBeTrue();
        ((BiasReport)bias).ProtectedAttributes.Count.ShouldBe(2);
    }

    // -- InMemoryAISystemRegistry (members the base guard tests do not reach) --

    [Fact]
    public async Task Registry_ReclassifyAsync_PublishesNotification_WhenEncinaIsProvided()
    {
        var encina = Substitute.For<IEncina>();
        var registry = new InMemoryAISystemRegistry(_time, encina);
        (await registry.RegisterSystemAsync(Registration("reclass"))).IsRight.ShouldBeTrue();

        var result = await registry.ReclassifyAsync("reclass", AIRiskLevel.LimitedRisk, "reviewed");

        result.IsRight.ShouldBeTrue();
        await encina.Received(1).Publish(
            Arg.Is<AISystemReclassifiedNotification>(n =>
                n.SystemId == "reclass"
                && n.PreviousRiskLevel == AIRiskLevel.HighRisk
                && n.NewRiskLevel == AIRiskLevel.LimitedRisk
                && n.Reason == "reviewed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Registry_ReclassifyAsync_UnknownSystem_ReturnsLeft()
    {
        var registry = NewRegistry();

        (await registry.ReclassifyAsync("missing", AIRiskLevel.HighRisk, "why")).IsLeft.ShouldBeTrue();
    }

    // -- DefaultAIActClassifier / Validator happy paths --

    [Fact]
    public async Task Classifier_UnknownSystem_ReturnsLeftForEveryOperation()
    {
        var sut = new DefaultAIActClassifier(NewRegistry(), _time);

        (await sut.ClassifySystemAsync("missing")).IsLeft.ShouldBeTrue();
        (await sut.IsProhibitedAsync("missing")).IsLeft.ShouldBeTrue();
        (await sut.EvaluateComplianceAsync("missing")).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Classifier_ProhibitedPractice_ClassifiesAsProhibited()
    {
        var registry = NewRegistry();
        (await registry.RegisterSystemAsync(Registration("banned") with
        {
            ProhibitedPractices = [ProhibitedPractice.SocialScoring]
        })).IsRight.ShouldBeTrue();
        var sut = new DefaultAIActClassifier(registry, _time);

        ((AIRiskLevel)await sut.ClassifySystemAsync("banned")).ShouldBe(AIRiskLevel.Prohibited);
        ((bool)await sut.IsProhibitedAsync("banned")).ShouldBeTrue();
        ((AIActComplianceResult)await sut.EvaluateComplianceAsync("banned")).Violations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Validator_RegisteredSystem_ReturnsResultForThatSystem()
    {
        var registry = NewRegistry();
        (await registry.RegisterSystemAsync(Registration("guard-validated", AIRiskLevel.LimitedRisk))).IsRight.ShouldBeTrue();
        var sut = new DefaultAIActComplianceValidator(
            new DefaultAIActClassifier(registry, _time), registry, new DefaultHumanOversightEnforcer(), _time);

        var result = await sut.ValidateAsync(new object(), "guard-validated");

        var compliance = (AIActComplianceResult)result;
        compliance.SystemId.ShouldBe("guard-validated");
        compliance.RiskLevel.ShouldBe(AIRiskLevel.LimitedRisk);
        compliance.RequiresTransparency.ShouldBeTrue();
    }

    // -- DefaultHumanOversightEnforcer --

    [Fact]
    public async Task OversightEnforcer_RecordsAndFindsDecision()
    {
        var sut = new DefaultHumanOversightEnforcer();
        var decisionId = Guid.NewGuid();

        var recorded = await sut.RecordHumanDecisionAsync(new HumanDecisionRecord
        {
            DecisionId = decisionId,
            SystemId = "sys",
            ReviewerId = "reviewer",
            ReviewedAtUtc = _time.GetUtcNow(),
            Decision = "approved",
            Rationale = "looks fine"
        });

        recorded.IsRight.ShouldBeTrue();
        ((bool)await sut.HasHumanApprovalAsync(decisionId)).ShouldBeTrue();
        ((bool)await sut.RequiresHumanReviewAsync(new object())).ShouldBeFalse();
    }

    // -- Pipeline behavior constructor --

    [Fact]
    public void PipelineBehavior_Constructor_NullValidator_Throws()
    {
        var act = () => new AIActCompliancePipelineBehavior<CompliantRequest, Unit>(
            null!, Options.Create(new AIActOptions()), NullLogger<AIActCompliancePipelineBehavior<CompliantRequest, Unit>>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("validator");
    }

    [Fact]
    public void PipelineBehavior_Constructor_NullOptions_Throws()
    {
        var act = () => new AIActCompliancePipelineBehavior<CompliantRequest, Unit>(
            Substitute.For<IAIActComplianceValidator>(), null!, NullLogger<AIActCompliancePipelineBehavior<CompliantRequest, Unit>>.Instance);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void PipelineBehavior_Constructor_NullLogger_Throws()
    {
        var act = () => new AIActCompliancePipelineBehavior<CompliantRequest, Unit>(
            Substitute.For<IAIActComplianceValidator>(), Options.Create(new AIActOptions()), null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    // -- Options validator --

    [Fact]
    public void OptionsValidator_DefaultOptions_Succeeds()
    {
        new AIActOptionsValidator().Validate(null, new AIActOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void OptionsValidator_AssembliesWithoutAutoRegistration_Fails()
    {
        var options = new AIActOptions { AutoRegisterFromAttributes = false };
        options.AssembliesToScan.Add(typeof(AIActGuardExtendedTests).Assembly);

        new AIActOptionsValidator().Validate(null, options).Failed.ShouldBeTrue();
    }

    // -- Auto-registration hosted service (internal) --

    [Fact]
    public async Task HostedService_StartAsync_RegistersDecoratedTypesAndSkipsKnownOnes()
    {
        var registry = NewRegistry();
        var descriptor = new AIActAutoRegistrationDescriptor([typeof(AIActGuardExtendedTests).Assembly]);
        var sut = new AIActAutoRegistrationHostedService(
            registry, descriptor, _time, NullLogger<AIActAutoRegistrationHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        registry.IsRegistered("guard-scan-system").ShouldBeTrue();
        var registered = (AISystemRegistration)await registry.GetSystemAsync("guard-scan-system");
        registered.Provider.ShouldBe("Acme");
        registered.Version.ShouldBe("1.0");
        registered.Description.ShouldBe("Guard scan");
        registered.RegisteredAtUtc.ShouldBe(_time.GetUtcNow());

        // A second start finds the systems already registered and keeps the registry unchanged.
        var countBefore = (await CountSystemsAsync(registry));
        await sut.StartAsync(CancellationToken.None);
        var countAfter = (await CountSystemsAsync(registry));
        countAfter.ShouldBe(countBefore);

        await sut.StopAsync(CancellationToken.None);
    }

    private static async Task<int> CountSystemsAsync(InMemoryAISystemRegistry registry)
    {
        var all = await registry.GetAllSystemsAsync();
        return all.Match(Right: systems => systems.Count, Left: _ => -1);
    }

    // -- ServiceCollectionExtensions and health check --

    [Fact]
    public async Task AddEncinaAIAct_WithHealthCheckAndNoAutoRegistration_ResolvesAndReportsHealth()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.AddHealthCheck = true;
        });

        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOptions<AIActOptions>>().Value.AddHealthCheck.ShouldBeTrue();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IAIActComplianceValidator>().ShouldBeOfType<DefaultAIActComplianceValidator>();
        scope.ServiceProvider.GetRequiredService<IAIActDocumentation>().ShouldBeOfType<DefaultAIActDocumentation>();
        scope.ServiceProvider.GetRequiredService<IDataQualityValidator>().ShouldBeOfType<DefaultDataQualityValidator>();

        var healthCheck = new AIActHealthCheck(provider, NullLogger<AIActHealthCheck>.Instance);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // No systems are registered, so the check is degraded but the infrastructure resolves.
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public void AddEncinaAIAct_WithAutoRegistration_AddsHostedService()
    {
        var services = new ServiceCollection();

        services.AddEncinaAIAct(options => options.AssembliesToScan.Add(typeof(AIActGuardExtendedTests).Assembly));

        services.ShouldContain(d => d.ServiceType == typeof(IHostedService));
    }

    // -- Notifications and records --

    [Fact]
    public void Notifications_AreConstructedWithTheirValues()
    {
        var when = _time.GetUtcNow();

        var reclassified = new AISystemReclassifiedNotification("s", AIRiskLevel.HighRisk, AIRiskLevel.LimitedRisk, "r", when);
        var oversight = new HumanOversightRequiredNotification("Req", "s", "reason", when);
        var blocked = new ProhibitedUseBlockedNotification("Req", "s", ProhibitedPractice.SocialScoring, when);

        reclassified.NewRiskLevel.ShouldBe(AIRiskLevel.LimitedRisk);
        oversight.Reason.ShouldBe("reason");
        blocked.Practice.ShouldBe(ProhibitedPractice.SocialScoring);
        blocked.OccurredAtUtc.ShouldBe(when);
    }

    [Fact]
    public void ReclassificationRecord_KeepsValues()
    {
        var record = new ReclassificationRecord
        {
            SystemId = "s",
            PreviousRiskLevel = AIRiskLevel.HighRisk,
            NewRiskLevel = AIRiskLevel.MinimalRisk,
            Reason = "assessed",
            ReclassifiedAtUtc = _time.GetUtcNow(),
            ReclassifiedBy = "auditor"
        };

        record.ReclassifiedBy.ShouldBe("auditor");
        (record with { Reason = "other" }).ShouldNotBe(record);
    }
}
