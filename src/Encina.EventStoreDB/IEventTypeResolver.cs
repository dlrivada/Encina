namespace Encina.EventStoreDB;

/// <summary>
/// Interface for resolving event types by name and vice versa.
/// </summary>
public interface IEventTypeResolver
{
    /// <summary>
    /// Gets the type name to use in the event store for the given event type.
    /// </summary>
    /// <param name="eventType">The CLR event type.</param>
    /// <returns>The event type name to store.</returns>
    string GetTypeName(Type eventType);

    /// <summary>
    /// Resolves the CLR type from an event type name stored in EventStoreDB.
    /// </summary>
    /// <param name="typeName">The event type name from the store.</param>
    /// <returns>The CLR type, or null if not found.</returns>
    Type? ResolveType(string typeName);

    /// <summary>
    /// Registers an event type for resolution.
    /// </summary>
    /// <typeparam name="TEvent">The event type to register.</typeparam>
    /// <param name="typeName">Optional custom type name. If null, uses the type name.</param>
    void Register<TEvent>(string? typeName = null) where TEvent : class;

    /// <summary>
    /// Registers an event type for resolution.
    /// </summary>
    /// <param name="eventType">The event type to register.</param>
    /// <param name="typeName">Optional custom type name. If null, uses the type name.</param>
    void Register(Type eventType, string? typeName = null);
}
