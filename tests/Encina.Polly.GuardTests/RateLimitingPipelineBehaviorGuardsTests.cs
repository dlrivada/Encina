using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Polly.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="RateLimitingPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies null argument validation.
/// </summary>
public class RateLimitingPipelineBehaviorGuardsTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<RateLimitingPipelineBehavior<TestRateLimitedRequest, string>> logger = null!;
        var rateLimiter = Substitute.For<IRateLimiter>();

        // Act & Assert
        var act = () => new RateLimitingPipelineBehavior<TestRateLimitedRequest, string>(logger, rateLimiter);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullRateLimiter_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<RateLimitingPipelineBehavior<TestRateLimitedRequest, string>>.Instance;
        IRateLimiter rateLimiter = null!;

        // Act & Assert
        var act = () => new RateLimitingPipelineBehavior<TestRateLimitedRequest, string>(logger, rateLimiter);
        act.Should().Throw<ArgumentNullException>().WithParameterName("rateLimiter");
    }
}

[RateLimit]
public sealed record TestRateLimitedRequest : IRequest<string>;
