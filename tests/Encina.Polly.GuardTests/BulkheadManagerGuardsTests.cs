namespace Encina.Polly.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="BulkheadManager"/>.
/// Verifies null argument validation.
/// </summary>
public class BulkheadManagerGuardsTests : IDisposable
{
    private readonly BulkheadManager _manager = new();

    public void Dispose()
    {
        _manager.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        TimeProvider timeProvider = null!;

        // Act & Assert
        var act = () => new BulkheadManager(timeProvider);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public async Task TryAcquireAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null!;
        var config = new BulkheadAttribute();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => _manager.TryAcquireAsync(key, config).AsTask());
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task TryAcquireAsync_NullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test";
        BulkheadAttribute config = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => _manager.TryAcquireAsync(key, config).AsTask());
        ex.ParamName.ShouldBe("config");
    }

    [Fact]
    public void GetMetrics_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null!;

        // Act & Assert
        Action act = () => _ = _manager.GetMetrics(key);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public void Reset_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null!;

        // Act & Assert
        var act = () => _manager.Reset(key);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }
}
