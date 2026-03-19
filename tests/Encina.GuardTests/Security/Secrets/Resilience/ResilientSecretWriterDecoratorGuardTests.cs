using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;

namespace Encina.GuardTests.Security.Secrets.Resilience;

/// <summary>
/// Guard clause tests for <see cref="ResilientSecretWriterDecorator"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class ResilientSecretWriterDecoratorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            null!,
            ResiliencePipeline.Empty,
            new SecretsResilienceOptions(),
            Substitute.For<ILogger<ResilientSecretWriterDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            Substitute.For<ISecretWriter>(),
            null!,
            new SecretsResilienceOptions(),
            Substitute.For<ILogger<ResilientSecretWriterDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            Substitute.For<ISecretWriter>(),
            ResiliencePipeline.Empty,
            null!,
            Substitute.For<ILogger<ResilientSecretWriterDecorator>>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            Substitute.For<ISecretWriter>(),
            ResiliencePipeline.Empty,
            new SecretsResilienceOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
