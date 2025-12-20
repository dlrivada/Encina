using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.Registry;
using SimpleMediator.Extensions.Resilience;
using Xunit;

namespace SimpleMediator.Extensions.Resilience.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="StandardResiliencePipelineBehavior{TRequest, TResponse}"/>.
/// Verifies that all null checks and invalid inputs throw appropriate exceptions.
/// </summary>
public class StandardResiliencePipelineBehaviorGuardsTests
{
    [Fact]
    public void Constructor_NullPipelineProvider_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencePipelineProvider<string>? pipelineProvider = null;
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();

        // Act
        var act = () => new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(
            pipelineProvider!,
            logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipelineProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var pipelineProvider = new ResiliencePipelineRegistry<string>();
        ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>? logger = null;

        // Act
        var act = () => new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(
            pipelineProvider,
            logger!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_BothParametersNull_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencePipelineProvider<string>? pipelineProvider = null;
        ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>? logger = null;

        // Act
        var act = () => new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(
            pipelineProvider!,
            logger!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var pipelineProvider = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();

        // Act
        var act = () => new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(
            pipelineProvider,
            logger);

        // Assert
        act.Should().NotThrow();
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse;
}
