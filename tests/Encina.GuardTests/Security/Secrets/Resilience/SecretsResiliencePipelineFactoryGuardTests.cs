using Encina.Security.Secrets.Resilience;
using Shouldly;

namespace Encina.GuardTests.Security.Secrets.Resilience;

/// <summary>
/// Guard clause tests for <see cref="SecretsResiliencePipelineFactory"/>.
/// Verifies that null arguments are properly rejected in the Create method.
/// </summary>
/// <remarks>
/// <see cref="SecretsResiliencePipelineFactory"/> is internal, so these tests
/// exercise it through the public <see cref="ServiceCollectionExtensions"/>
/// registration path where possible, and directly via InternalsVisibleTo.
/// </remarks>
public sealed class SecretsResiliencePipelineFactoryGuardTests
{
    #region Create Method Guards

    [Fact]
    public void Create_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => SecretsResiliencePipelineFactory.Create(
            null!,
            new SecretsCircuitBreakerState(),
            Substitute.For<ILogger<object>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Create_NullCircuitBreakerState_ThrowsArgumentNullException()
    {
        var act = () => SecretsResiliencePipelineFactory.Create(
            new SecretsResilienceOptions(),
            null!,
            Substitute.For<ILogger<object>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("circuitBreakerState");
    }

    [Fact]
    public void Create_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => SecretsResiliencePipelineFactory.Create(
            new SecretsResilienceOptions(),
            new SecretsCircuitBreakerState(),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Create_ValidArguments_DoesNotThrow()
    {
        var act = () => SecretsResiliencePipelineFactory.Create(
            new SecretsResilienceOptions(),
            new SecretsCircuitBreakerState(),
            Substitute.For<ILogger<object>>());

        Should.NotThrow(act);
    }

    #endregion
}
