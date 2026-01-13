namespace Encina.Polly.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="AdaptiveRateLimiter"/>.
/// Verifies null argument validation.
/// </summary>
public class AdaptiveRateLimiterGuardsTests
{
    [Fact]
    public async Task AcquireAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        var attribute = new RateLimitAttribute();
        string key = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => rateLimiter.AcquireAsync(key, attribute, CancellationToken.None).AsTask());
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task AcquireAsync_NullAttribute_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        RateLimitAttribute attribute = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => rateLimiter.AcquireAsync("test-key", attribute, CancellationToken.None).AsTask());
        ex.ParamName.ShouldBe("config");
    }

    [Fact]
    public void RecordSuccess_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        var act = () => rateLimiter.RecordSuccess(key);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public void RecordFailure_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        var act = () => rateLimiter.RecordFailure(key);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public void GetState_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        Action act = () => _ = rateLimiter.GetState(key);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public void Reset_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        string key = null!;

        // Act & Assert
        var act = () => rateLimiter.Reset(key);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }
}
