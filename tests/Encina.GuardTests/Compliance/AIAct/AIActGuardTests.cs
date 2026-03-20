using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Health;
using Encina.Compliance.AIAct.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.GuardTests.Compliance.AIAct;

/// <summary>
/// Guard clause tests for Encina.Compliance.AIAct types.
/// Verifies that null arguments are properly rejected.
/// </summary>
public class AIActGuardTests
{
    // -- InMemoryAISystemRegistry --

    [Fact]
    public void Registry_Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryAISystemRegistry(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task Registry_GetSystemAsync_NullSystemId_ThrowsArgumentNullException()
    {
        var sut = new InMemoryAISystemRegistry(new FakeTimeProvider());
        var act = () => sut.GetSystemAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("systemId");
    }

    [Fact]
    public async Task Registry_RegisterSystemAsync_NullRegistration_ThrowsArgumentNullException()
    {
        var sut = new InMemoryAISystemRegistry(new FakeTimeProvider());
        var act = () => sut.RegisterSystemAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("registration");
    }

    [Fact]
    public async Task Registry_ReclassifyAsync_NullSystemId_ThrowsArgumentNullException()
    {
        var sut = new InMemoryAISystemRegistry(new FakeTimeProvider());
        var act = () => sut.ReclassifyAsync(null!, AIRiskLevel.HighRisk, "test").AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("systemId");
    }

    [Fact]
    public async Task Registry_ReclassifyAsync_NullReason_ThrowsArgumentNullException()
    {
        var sut = new InMemoryAISystemRegistry(new FakeTimeProvider());
        var act = () => sut.ReclassifyAsync("sys-1", AIRiskLevel.HighRisk, null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Registry_IsRegistered_NullSystemId_ThrowsArgumentNullException()
    {
        var sut = new InMemoryAISystemRegistry(new FakeTimeProvider());
        Action act = () => sut.IsRegistered(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("systemId");
    }

    // -- DefaultAIActClassifier --

    [Fact]
    public void Classifier_Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        var act = () => new DefaultAIActClassifier(null!, new FakeTimeProvider());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    [Fact]
    public void Classifier_Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<IAISystemRegistry>();
        var act = () => new DefaultAIActClassifier(registry, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task Classifier_ClassifySystemAsync_NullSystemId_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<IAISystemRegistry>();
        var sut = new DefaultAIActClassifier(registry, new FakeTimeProvider());
        var act = () => sut.ClassifySystemAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("systemId");
    }

    [Fact]
    public async Task Classifier_IsProhibitedAsync_NullSystemId_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<IAISystemRegistry>();
        var sut = new DefaultAIActClassifier(registry, new FakeTimeProvider());
        var act = () => sut.IsProhibitedAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("systemId");
    }

    [Fact]
    public async Task Classifier_EvaluateComplianceAsync_NullSystemId_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<IAISystemRegistry>();
        var sut = new DefaultAIActClassifier(registry, new FakeTimeProvider());
        var act = () => sut.EvaluateComplianceAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("systemId");
    }

    // -- DefaultAIActComplianceValidator --

    [Fact]
    public void Validator_Constructor_NullClassifier_ThrowsArgumentNullException()
    {
        var registry = Substitute.For<IAISystemRegistry>();
        var enforcer = Substitute.For<IHumanOversightEnforcer>();
        var act = () => new DefaultAIActComplianceValidator(null!, registry, enforcer, new FakeTimeProvider());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("classifier");
    }

    [Fact]
    public void Validator_Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        var classifier = Substitute.For<IAIActClassifier>();
        var enforcer = Substitute.For<IHumanOversightEnforcer>();
        var act = () => new DefaultAIActComplianceValidator(classifier, null!, enforcer, new FakeTimeProvider());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    [Fact]
    public void Validator_Constructor_NullEnforcer_ThrowsArgumentNullException()
    {
        var classifier = Substitute.For<IAIActClassifier>();
        var registry = Substitute.For<IAISystemRegistry>();
        var act = () => new DefaultAIActComplianceValidator(classifier, registry, null!, new FakeTimeProvider());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("oversightEnforcer");
    }

    [Fact]
    public void Validator_Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var classifier = Substitute.For<IAIActClassifier>();
        var registry = Substitute.For<IAISystemRegistry>();
        var enforcer = Substitute.For<IHumanOversightEnforcer>();
        var act = () => new DefaultAIActComplianceValidator(classifier, registry, enforcer, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task Validator_ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var classifier = Substitute.For<IAIActClassifier>();
        var registry = Substitute.For<IAISystemRegistry>();
        var enforcer = Substitute.For<IHumanOversightEnforcer>();
        var sut = new DefaultAIActComplianceValidator(classifier, registry, enforcer, new FakeTimeProvider());
        var act = () => sut.ValidateAsync<string>(null!, null).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    // -- DefaultHumanOversightEnforcer --

    [Fact]
    public async Task OversightEnforcer_RequiresHumanReviewAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new DefaultHumanOversightEnforcer();
        var act = () => sut.RequiresHumanReviewAsync<string>(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task OversightEnforcer_RecordHumanDecisionAsync_NullDecision_ThrowsArgumentNullException()
    {
        var sut = new DefaultHumanOversightEnforcer();
        var act = () => sut.RecordHumanDecisionAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("decision");
    }

    // -- AIActCompliancePipelineBehavior --

    [Fact]
    public async Task PipelineBehavior_Handle_NullRequest_ThrowsArgumentNullException()
    {
        var validator = Substitute.For<IAIActComplianceValidator>();
        var options = Options.Create(new AIActOptions());
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AIActCompliancePipelineBehavior<TestCommand, LanguageExt.Unit>>();

        var sut = new AIActCompliancePipelineBehavior<TestCommand, LanguageExt.Unit>(
            validator, options, logger);

        var act = () => sut.Handle(
            null!,
            RequestContext.CreateForTest(),
            () => ValueTask.FromResult<LanguageExt.Either<EncinaError, LanguageExt.Unit>>(LanguageExt.Unit.Default),
            CancellationToken.None).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    // -- AIActOptionsValidator --

    [Fact]
    public void OptionsValidator_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new AIActOptionsValidator();
        var act = () => sut.Validate(null, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    // -- AIActHealthCheck --

    [Fact]
    public void HealthCheck_Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AIActHealthCheck>();
        Action act = () => _ = new AIActHealthCheck(null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void HealthCheck_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        Action act = () => _ = new AIActHealthCheck(services, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    // -- ServiceCollectionExtensions --

    [Fact]
    public void AddEncinaAIAct_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var act = () => services.AddEncinaAIAct();
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    // Test stub
    private sealed record TestCommand : ICommand<LanguageExt.Unit>;
}
