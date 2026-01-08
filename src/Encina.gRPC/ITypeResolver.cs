namespace Encina.gRPC;

/// <summary>
/// Defines a contract for resolving types from their string representation.
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// Resolves a request type from its fully qualified name.
    /// </summary>
    /// <param name="typeName">The fully qualified type name. Must not be null, empty, or whitespace.</param>
    /// <returns>The resolved type, or <see langword="null"/> if the type is not found or not registered.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="typeName"/> is <see langword="null"/>, empty, or whitespace.</exception>
    Type? ResolveRequestType(string typeName);

    /// <summary>
    /// Resolves a notification type from its fully qualified name.
    /// </summary>
    /// <param name="typeName">The fully qualified type name. Must not be null, empty, or whitespace.</param>
    /// <returns>The resolved type, or <see langword="null"/> if the type is not found or not registered.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="typeName"/> is <see langword="null"/>, empty, or whitespace.</exception>
    Type? ResolveNotificationType(string typeName);
}
