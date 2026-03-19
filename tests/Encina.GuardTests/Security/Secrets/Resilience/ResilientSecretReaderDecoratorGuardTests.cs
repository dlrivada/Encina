using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;

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

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            null!,
            new SecretsResilienceOptions(),
            Substitute.For<ILogger<ResilientSecretReaderDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            ResiliencePipeline.Empty,
            null!,
            Substitute.For<ILogger<ResilientSecretReaderDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            Substitute.For<ISecretReader>(),
            ResiliencePipeline.Empty,
            new SecretsResilienceOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
