using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Injection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Security.Secrets.Injection;

/// <summary>
/// Guard clause tests for <see cref="SecretInjectionPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies that null arguments are properly rejected in the constructor and Handle method.
/// </summary>
public sealed class SecretInjectionPipelineBehaviorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new SecretInjectionPipelineBehavior<FakeRequest, string>(
            null!,
            Options.Create(new SecretsOptions()),
            NullLogger<SecretInjectionPipelineBehavior<FakeRequest, string>>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("orchestrator");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SecretInjectionPipelineBehavior<FakeRequest, string>(
            CreateOrchestrator(),
            null!,
            NullLogger<SecretInjectionPipelineBehavior<FakeRequest, string>>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SecretInjectionPipelineBehavior<FakeRequest, string>(
            CreateOrchestrator(),
            Options.Create(new SecretsOptions()),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Handle Method Guards

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult<Either<EncinaError, string>>("ok"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.Handle(
            new FakeRequest(),
            null!,
            () => ValueTask.FromResult<Either<EncinaError, string>>("ok"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region Helpers

    private static SecretInjectionOrchestrator CreateOrchestrator()
    {
        return new SecretInjectionOrchestrator(
            Substitute.For<ISecretReader>(),
            NullLogger<SecretInjectionOrchestrator>.Instance);
    }

    private static SecretInjectionPipelineBehavior<FakeRequest, string> CreateSut()
    {
        return new SecretInjectionPipelineBehavior<FakeRequest, string>(
            CreateOrchestrator(),
            Options.Create(new SecretsOptions()),
            NullLogger<SecretInjectionPipelineBehavior<FakeRequest, string>>.Instance);
    }

    private sealed class FakeRequest : IRequest<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
