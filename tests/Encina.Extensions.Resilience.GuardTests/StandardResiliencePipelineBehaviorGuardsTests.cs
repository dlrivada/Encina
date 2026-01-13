using Encina.Extensions.Resilience;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Encina.Extensions.Resilience.GuardTests;

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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("pipelineProvider");
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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
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
        Should.NotThrow(act);
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse;
}
