using Microsoft.Extensions.Logging;

namespace Encina.Polly.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies null argument validation.
/// </summary>
public class CircuitBreakerPipelineBehaviorGuardsTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>> logger = null!;

        // Act & Assert
        var act = () => new CircuitBreakerPipelineBehavior<TestRequest, string>(logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [CircuitBreaker]
    private sealed record TestRequest : IRequest<string>;
}
