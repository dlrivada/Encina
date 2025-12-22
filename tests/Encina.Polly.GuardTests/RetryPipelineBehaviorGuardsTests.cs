using Microsoft.Extensions.Logging;

namespace Encina.Polly.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="RetryPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies null argument validation.
/// </summary>
public class RetryPipelineBehaviorGuardsTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<RetryPipelineBehavior<TestRequest, string>> logger = null!;

        // Act & Assert
        var act = () => new RetryPipelineBehavior<TestRequest, string>(logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Retry]
    private sealed record TestRequest : IRequest<string>;
}
