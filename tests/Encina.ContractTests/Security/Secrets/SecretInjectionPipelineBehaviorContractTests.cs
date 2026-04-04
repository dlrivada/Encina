using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Injection;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.Secrets;

/// <summary>
/// Contract tests for <see cref="SecretInjectionPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies the pipeline behavior correctly invokes the next step and handles
/// injection results as specified by the contract.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Secrets")]
public sealed class SecretInjectionPipelineBehaviorContractTests
{
    #region Pipeline Behavior Contract: No Injectable Properties

    [Fact]
    public async Task Handle_RequestWithoutInjectableProperties_CallsNextStep()
    {
        // Arrange
        SecretPropertyCache.ClearCache();

        var orchestrator = CreateOrchestrator(
            Substitute.For<ISecretReader>());

        var options = Options.Create(new SecretsOptions());
        var logger = NullLogger<SecretInjectionPipelineBehavior<PlainRequest, string>>.Instance;

        var behavior = new SecretInjectionPipelineBehavior<PlainRequest, string>(
            orchestrator, options, logger);

        var request = new PlainRequest { Name = "test" };
        var context = Substitute.For<IRequestContext>();
        var expectedResult = Right<EncinaError, string>("handler-result");
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue(
            "Pipeline must call nextStep and propagate its result when no injectable properties exist");
        result.Match(Right: v => v, Left: _ => string.Empty)
            .ShouldBe("handler-result");
    }

    #endregion

    #region Pipeline Behavior Contract: Successful Injection

    [Fact]
    public async Task Handle_RequestWithInjectableProperties_InjectsAndCallsNextStep()
    {
        // Arrange
        SecretPropertyCache.ClearCache();

        var secretReader = Substitute.For<ISecretReader>();
        secretReader.GetSecretAsync("my-api-key", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, string>("secret-value-123"));

        var orchestrator = CreateOrchestrator(secretReader);
        var options = Options.Create(new SecretsOptions());
        var logger = NullLogger<SecretInjectionPipelineBehavior<InjectableRequest, string>>.Instance;

        var behavior = new SecretInjectionPipelineBehavior<InjectableRequest, string>(
            orchestrator, options, logger);

        var request = new InjectableRequest();
        var context = Substitute.For<IRequestContext>();
        var expectedResult = Right<EncinaError, string>("ok");
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue(
            "Pipeline must call nextStep after successful injection");
    }

    #endregion

    #region Pipeline Behavior Contract: Failed Required Injection Short-Circuits

    [Fact]
    public async Task Handle_RequiredSecretNotFound_ReturnsLeftWithoutCallingNextStep()
    {
        // Arrange
        SecretPropertyCache.ClearCache();

        var secretReader = Substitute.For<ISecretReader>();
        secretReader.GetSecretAsync("my-api-key", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, string>(SecretsErrors.NotFound("my-api-key")));

        var orchestrator = CreateOrchestrator(secretReader);
        var options = Options.Create(new SecretsOptions());
        var logger = NullLogger<SecretInjectionPipelineBehavior<InjectableRequest, string>>.Instance;

        var behavior = new SecretInjectionPipelineBehavior<InjectableRequest, string>(
            orchestrator, options, logger);

        var request = new InjectableRequest();
        var context = Substitute.For<IRequestContext>();
        var nextStepCalled = false;
        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("should-not-reach"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "Pipeline must short-circuit with Left when a required secret injection fails");
        nextStepCalled.ShouldBeFalse(
            "Pipeline must NOT call nextStep when injection fails for a required secret");
    }

    #endregion

    #region Test Types

    private sealed class PlainRequest : IRequest<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class InjectableRequest : IRequest<string>
    {
        [InjectSecret("my-api-key")]
        public string? ApiKey { get; set; }
    }

    #endregion

    #region Helpers

    private static SecretInjectionOrchestrator CreateOrchestrator(ISecretReader reader)
    {
        return new SecretInjectionOrchestrator(
            reader,
            NullLogger<SecretInjectionOrchestrator>.Instance);
    }

    #endregion
}
