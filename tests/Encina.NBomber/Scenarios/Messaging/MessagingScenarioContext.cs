namespace Encina.NBomber.Scenarios.Messaging;

/// <summary>
/// Shared context for messaging load testing scenarios.
/// Contains the provider factory, configuration, and shared state.
/// </summary>
/// <param name="ProviderFactory">The messaging provider factory instance.</param>
/// <param name="ProviderName">The provider name (e.g., "inmemory").</param>
public sealed record MessagingScenarioContext(
    IMessagingProviderFactory ProviderFactory,
    string ProviderName)
{
    /// <summary>
    /// Gets the provider category.
    /// </summary>
    public MessagingProviderCategory Category => ProviderFactory.Category;

    /// <summary>
    /// Gets the provider options.
    /// </summary>
    public MessagingProviderOptions Options => ProviderFactory.Options;

    /// <summary>
    /// Thread-safe message ID generation.
    /// </summary>
    private long _messageSequence;

    /// <summary>
    /// Thread-safe handler ID generation.
    /// </summary>
    private long _handlerSequence;

    /// <summary>
    /// Generates the next unique message ID for load testing.
    /// </summary>
    /// <returns>A unique message ID.</returns>
    public long NextMessageId() => Interlocked.Increment(ref _messageSequence);

    /// <summary>
    /// Generates a unique GUID based on the message sequence.
    /// </summary>
    /// <returns>A deterministic GUID based on sequence.</returns>
    public Guid NextMessageGuid()
    {
        var id = NextMessageId();
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    /// <summary>
    /// Generates the next unique handler ID for load testing.
    /// </summary>
    /// <returns>A unique handler ID.</returns>
    public long NextHandlerId() => Interlocked.Increment(ref _handlerSequence);

    /// <summary>
    /// Gets a message type identifier based on iteration for variety.
    /// </summary>
    /// <param name="typeCount">Total number of message types to simulate.</param>
    /// <returns>A type identifier number.</returns>
    public int GetMessageTypeIndex(int typeCount = 5)
    {
        var id = NextMessageId();
        return (int)((id % typeCount) + 1);
    }
}
