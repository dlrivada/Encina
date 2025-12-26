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
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public async Task TryAcquireAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null!;
        var config = new BulkheadAttribute();

        // Act & Assert
        var act = async () => await _manager.TryAcquireAsync(key, config);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public async Task TryAcquireAsync_NullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test";
        BulkheadAttribute config = null!;

        // Act & Assert
        var act = async () => await _manager.TryAcquireAsync(key, config);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void GetMetrics_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null!;

        // Act & Assert
        var act = () => _manager.GetMetrics(key);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void Reset_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null!;

        // Act & Assert
        var act = () => _manager.Reset(key);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }
}
