using Encina.Polly;
using Encina.Recoverability;

namespace Encina.GuardTests.Infrastructure.Polly;

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
        var act = () => new RetryPipelineBehavior<TestRequest, string>(logger, new DefaultErrorClassifier());
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullErrorClassifier_ThrowsArgumentNullException()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RetryPipelineBehavior<TestRequest, string>>.Instance;

        var act = () => new RetryPipelineBehavior<TestRequest, string>(logger, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("errorClassifier");
    }

    [Retry]
    private sealed record TestRequest : IRequest<string>;
}
