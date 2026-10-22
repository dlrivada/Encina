using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Encina.ContractTests.Compliance.AIAct;

/// <summary>
/// Contract tests for <see cref="AIActCompliancePipelineBehavior{TRequest, TResponse}"/>: requests without
/// AI Act attributes pass through, disabled enforcement is a no-op, compliant requests proceed, violations
/// block in Block mode and only warn in Warn mode, and a prohibited practice is blocked in both modes.
/// Runs the real behavior against the real registry, classifier, oversight enforcer and validator.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "AIAct")]
public sealed class AIActCompliancePipelineBehaviorContractTests
{
    private sealed record PlainRequest : ICommand<Unit>;

    [HighRiskAI(Category = AISystemCategory.EmploymentWorkersManagement, SystemId = "contract-compliant")]
    private sealed record CompliantRequest : ICommand<Unit>;

    [HighRiskAI(Category = AISystemCategory.EmploymentWorkersManagement, SystemId = "contract-prohibited")]
    private sealed record ProhibitedRequest : ICommand<Unit>;

    [HighRiskAI(Category = AISystemCategory.EmploymentWorkersManagement, SystemId = "contract-unregistered")]
    private sealed record UnregisteredRequest : ICommand<Unit>;

    [RequireHumanOversight(Reason = "Decisions need review", SystemId = "contract-oversight")]
    private sealed record OversightRequest : ICommand<Unit>;

    private readonly FakeTimeProvider _time = new();
    private readonly InMemoryAISystemRegistry _registry;
    private int _nextCalls;

    public AIActCompliancePipelineBehaviorContractTests()
    {
        _registry = new InMemoryAISystemRegistry(_time);
        Register("contract-compliant", AIRiskLevel.HighRisk);
        Register("contract-oversight", AIRiskLevel.MinimalRisk);
        Register("contract-prohibited", AIRiskLevel.HighRisk, [ProhibitedPractice.SocialScoring]);
    }

    private void Register(string systemId, AIRiskLevel level, IReadOnlyList<ProhibitedPractice>? practices = null)
    {
        var result = _registry.RegisterSystemAsync(new AISystemRegistration
        {
            SystemId = systemId,
            Name = systemId,
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = level,
            RegisteredAtUtc = _time.GetUtcNow(),
            ProhibitedPractices = practices ?? []
        }).AsTask().Result;
        result.IsRight.ShouldBeTrue();
    }

    private AIActCompliancePipelineBehavior<TRequest, Unit> Behavior<TRequest>(AIActEnforcementMode mode)
        where TRequest : IRequest<Unit>
    {
        var classifier = new DefaultAIActClassifier(_registry, _time);
        var validator = new DefaultAIActComplianceValidator(
            classifier, _registry, new DefaultHumanOversightEnforcer(), _time);
        var options = Options.Create(new AIActOptions { EnforcementMode = mode });

        return new AIActCompliancePipelineBehavior<TRequest, Unit>(
            validator, options, NullLogger<AIActCompliancePipelineBehavior<TRequest, Unit>>.Instance);
    }

    private RequestHandlerCallback<Unit> Next() => () =>
    {
        _nextCalls++;
        return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    };

    private static string CodeOf(Either<EncinaError, Unit> result)
        => ((EncinaError)result).GetCode().IfNone(string.Empty);

    [Theory]
    [InlineData(AIActEnforcementMode.Block)]
    [InlineData(AIActEnforcementMode.Warn)]
    [InlineData(AIActEnforcementMode.Disabled)]
    public async Task Handle_RequestWithoutAttributes_PassesThroughInEveryMode(AIActEnforcementMode mode)
    {
        var sut = Behavior<PlainRequest>(mode);

        var result = await sut.Handle(new PlainRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextCalls.ShouldBe(1);
    }

    [Theory]
    [InlineData(AIActEnforcementMode.Block)]
    [InlineData(AIActEnforcementMode.Warn)]
    public async Task Handle_CompliantRegisteredSystem_ProceedsToHandler(AIActEnforcementMode mode)
    {
        var sut = Behavior<CompliantRequest>(mode);

        var result = await sut.Handle(new CompliantRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_OversightAttributeOnRegisteredSystem_ProceedsToHandler()
    {
        var sut = Behavior<OversightRequest>(AIActEnforcementMode.Block);

        var result = await sut.Handle(new OversightRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_DisabledMode_SkipsChecksEvenForProhibitedSystem()
    {
        var sut = Behavior<ProhibitedRequest>(AIActEnforcementMode.Disabled);

        var result = await sut.Handle(new ProhibitedRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextCalls.ShouldBe(1);
    }

    [Theory]
    [InlineData(AIActEnforcementMode.Block)]
    [InlineData(AIActEnforcementMode.Warn)]
    public async Task Handle_ProhibitedPractice_IsBlockedInBlockAndWarnMode(AIActEnforcementMode mode)
    {
        var sut = Behavior<ProhibitedRequest>(mode);

        var result = await sut.Handle(new ProhibitedRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        CodeOf(result).ShouldBe(AIActErrors.ProhibitedUseCode);
        _nextCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_UnregisteredSystem_BlockMode_ReturnsComplianceValidationFailed()
    {
        var sut = Behavior<UnregisteredRequest>(AIActEnforcementMode.Block);

        var result = await sut.Handle(new UnregisteredRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        CodeOf(result).ShouldBe(AIActErrors.ComplianceValidationFailedCode);
        _nextCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_UnregisteredSystem_WarnMode_WarnsAndProceeds()
    {
        var sut = Behavior<UnregisteredRequest>(AIActEnforcementMode.Warn);

        var result = await sut.Handle(new UnregisteredRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _nextCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_HandlerResultIsReturnedUnchanged_WhenCheckPasses()
    {
        var sut = Behavior<CompliantRequest>(AIActEnforcementMode.Block);
        var handlerError = EncinaError.New("handler failed");

        var result = await sut.Handle(
            new CompliantRequest(),
            RequestContext.CreateForTest(),
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(handlerError),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidatorFailure_ReturnsValidatorErrorWithoutCallingHandler()
    {
        var failingValidator = new FailingValidator();
        var options = Options.Create(new AIActOptions());
        var sut = new AIActCompliancePipelineBehavior<CompliantRequest, Unit>(
            failingValidator, options, NullLogger<AIActCompliancePipelineBehavior<CompliantRequest, Unit>>.Instance);

        var result = await sut.Handle(new CompliantRequest(), RequestContext.CreateForTest(), Next(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        CodeOf(result).ShouldBe(AIActErrors.ValidatorErrorCode);
        _nextCalls.ShouldBe(0);
    }

    /// <summary>A real <see cref="IAIActComplianceValidator"/> that always reports a validator error.</summary>
    private sealed class FailingValidator : IAIActComplianceValidator
    {
        public ValueTask<Either<EncinaError, AIActComplianceResult>> ValidateAsync<TRequest>(
            TRequest request,
            string? systemId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, AIActComplianceResult>>(EncinaError.New("validator down"));
    }
}
