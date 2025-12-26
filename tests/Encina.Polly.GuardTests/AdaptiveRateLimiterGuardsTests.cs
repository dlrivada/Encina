namespace Encina.Polly.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="AdaptiveRateLimiter"/>.
/// Verifies null argument validation.
/// </summary>
public class AdaptiveRateLimiterGuardsTests
{
    [Fact]
    public void AcquireAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        var attribute = new RateLimitAttribute();
        string key = null!;

        // Act & Assert
        var act = async () => await rateLimiter.AcquireAsync(key, attribute, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void AcquireAsync_NullAttribute_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        RateLimitAttribute attribute = null!;

        // Act & Assert
        var act = async () => await rateLimiter.AcquireAsync("test-key", attribute, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void RecordSuccess_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        var act = () => rateLimiter.RecordSuccess(key);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void RecordFailure_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        var act = () => rateLimiter.RecordFailure(key);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void GetState_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        var act = () => rateLimiter.GetState(key);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void Reset_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        var act = () => rateLimiter.Reset(key);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }
}
