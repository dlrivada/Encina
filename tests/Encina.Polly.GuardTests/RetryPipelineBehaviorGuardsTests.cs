using Microsoft.Extensions.Logging;
using Shouldly;

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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Retry]
    private sealed record TestRequest : IRequest<string>;
}
