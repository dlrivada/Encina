#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Injection;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretInjectionPipelineBehaviorTests : IDisposable
{
    private readonly ISecretReader _secretReader;
    private readonly ILogger<SecretInjectionOrchestrator> _orchestratorLogger;
    private readonly ILogger<SecretInjectionPipelineBehavior<TestSecretCommand, Unit>> _behaviorLogger;
    private readonly IRequestContext _context;

    public SecretInjectionPipelineBehaviorTests()
    {
        _secretReader = Substitute.For<ISecretReader>();
        _orchestratorLogger = NullLogger<SecretInjectionOrchestrator>.Instance;
        _behaviorLogger = NullLogger<SecretInjectionPipelineBehavior<TestSecretCommand, Unit>>.Instance;
        _context = Substitute.For<IRequestContext>();
        _context.CorrelationId.Returns(Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        SecretPropertyCache.ClearCache();
    }

    #region Test Fixtures

    internal sealed class TestPlainCommand : IRequest<Unit>
    {
        public string Name { get; set; } = "";
    }

    internal sealed class TestSecretCommand : IRequest<Unit>
    {
        [InjectSecret("test-api-key")]
        public string ApiKey { get; set; } = "";
    }

    #endregion

    #region Helpers

    private SecretInjectionPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        bool enableTracing = false,
        bool enableMetrics = false)
        where TRequest : IRequest<TResponse>
    {
        var orchestrator = new SecretInjectionOrchestrator(_secretReader, _orchestratorLogger);
        var options = Options.Create(new SecretsOptions
        {
            EnableTracing = enableTracing,
            EnableMetrics = enableMetrics,
            EnableSecretInjection = true
        });

        return new SecretInjectionPipelineBehavior<TRequest, TResponse>(
            orchestrator,
            options,
            NullLogger<SecretInjectionPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    private bool _nextStepCalled;

    private RequestHandlerCallback<Unit> CreateTrackedNextStep()
    {
        _nextStepCalled = false;
        return () =>
        {
            _nextStepCalled = true;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        };
    }

    #endregion

    #region Handle - No Injectable Properties

    [Fact]
    public async Task Handle_NoInjectableProperties_PassesThroughToNextStep()
    {
        var behavior = CreateBehavior<TestPlainCommand, Unit>();
        var request = new TestPlainCommand { Name = "test" };
        var nextStep = CreateTrackedNextStep();

        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
        await _secretReader.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - With Injectable Properties

    [Fact]
    public async Task Handle_WithInjectableProperties_CallsSecretReader()
    {
        var behavior = CreateBehavior<TestSecretCommand, Unit>();
        var request = new TestSecretCommand();
        _secretReader.GetSecretAsync("test-api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("injected-key"));
        var nextStep = CreateTrackedNextStep();

        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _nextStepCalled.Should().BeTrue();
        request.ApiKey.Should().Be("injected-key");
    }

    [Fact]
    public async Task Handle_OrchestratorFails_ReturnsError_DoesNotCallNextStep()
    {
        var behavior = CreateBehavior<TestSecretCommand, Unit>();
        var request = new TestSecretCommand();
        _secretReader.GetSecretAsync("test-api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("test-api-key")));
        var nextStep = CreateTrackedNextStep();

        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        _nextStepCalled.Should().BeFalse();
    }

    #endregion

    #region Handle - Input Validation

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior<TestSecretCommand, Unit>();
        var nextStep = CreateTrackedNextStep();

        var act = () => behavior.Handle(null!, _context, nextStep, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior<TestSecretCommand, Unit>();
        var request = new TestSecretCommand();
        var nextStep = CreateTrackedNextStep();

        var act = () => behavior.Handle(request, null!, nextStep, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region Handle - Constructor Validation

    [Fact]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        var options = Options.Create(new SecretsOptions());

        var act = () => new SecretInjectionPipelineBehavior<TestSecretCommand, Unit>(
            null!, options, _behaviorLogger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("orchestrator");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var orchestrator = new SecretInjectionOrchestrator(_secretReader, _orchestratorLogger);

        var act = () => new SecretInjectionPipelineBehavior<TestSecretCommand, Unit>(
            orchestrator, null!, _behaviorLogger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var orchestrator = new SecretInjectionOrchestrator(_secretReader, _orchestratorLogger);
        var options = Options.Create(new SecretsOptions());

        var act = () => new SecretInjectionPipelineBehavior<TestSecretCommand, Unit>(
            orchestrator, options, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Handle - NextStep Error Propagation

    [Fact]
    public async Task Handle_NextStepReturnsError_PropagatesError()
    {
        var behavior = CreateBehavior<TestSecretCommand, Unit>();
        var request = new TestSecretCommand();
        _secretReader.GetSecretAsync("test-api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("injected-key"));

        var handlerError = SecretsErrors.ProviderUnavailable("test");
        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(handlerError);

        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion
}
