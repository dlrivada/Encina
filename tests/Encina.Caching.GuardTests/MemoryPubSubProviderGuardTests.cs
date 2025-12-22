namespace Encina.Caching.GuardTests;

/// <summary>
/// Guard tests for <see cref="MemoryPubSubProvider"/> to verify null parameter handling.
/// </summary>
public class MemoryPubSubProviderGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MemoryPubSubProvider(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that SubscribeAsync throws ArgumentNullException when channel is null.
    /// </summary>
    [Fact]
    public async Task SubscribeAsync_NullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string channel = null!;

        // Act & Assert
        var act = async () => await provider.SubscribeAsync(
            channel,
            _ => Task.CompletedTask,
            CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("channel");
    }

    /// <summary>
    /// Verifies that SubscribeAsync throws ArgumentNullException when handler is null.
    /// </summary>
    [Fact]
    public async Task SubscribeAsync_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        Func<string, Task> handler = null!;

        // Act & Assert
        var act = async () => await provider.SubscribeAsync(
            "channel",
            handler,
            CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("handler");
    }

    /// <summary>
    /// Verifies that UnsubscribeAsync throws ArgumentNullException when channel is null.
    /// </summary>
    [Fact]
    public async Task UnsubscribeAsync_NullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string channel = null!;

        // Act & Assert
        var act = async () => await provider.UnsubscribeAsync(channel, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("channel");
    }

    /// <summary>
    /// Verifies that PublishAsync throws ArgumentNullException when channel is null.
    /// </summary>
    [Fact]
    public async Task PublishAsync_NullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string channel = null!;

        // Act & Assert
        var act = async () => await provider.PublishAsync(channel, "message", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("channel");
    }

    /// <summary>
    /// Verifies that PublishAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task PublishAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string message = null!;

        // Act & Assert
        var act = async () => await provider.PublishAsync("channel", message, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    private static MemoryPubSubProvider CreateProvider()
    {
        return new MemoryPubSubProvider(NullLogger<MemoryPubSubProvider>.Instance);
    }
}
