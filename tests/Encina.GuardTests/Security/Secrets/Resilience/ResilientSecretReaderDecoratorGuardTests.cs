using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Resilience;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Shouldly;

namespace Encina.GuardTests.Security.Secrets.Resilience;

/// <summary>
/// Guard clause tests for <see cref="ResilientSecretReaderDecorator"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class ResilientSecretReaderDecoratorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            null!,
            ResiliencePipeline.Empty,
            new SecretsResilienceOptions(),
            Substitute.For<ILogger<ResilientSecretReaderDecorator>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            null!,
            new SecretsResilienceOptions(),
            Substitute.For<ILogger<ResilientSecretReaderDecorator>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("pipeline");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            ResiliencePipeline.Empty,
            null!,
            Substitute.For<ILogger<ResilientSecretReaderDecorator>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            ResiliencePipeline.Empty,
            new SecretsResilienceOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion
}
