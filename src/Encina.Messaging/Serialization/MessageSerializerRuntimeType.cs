using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Messaging.Serialization;

/// <summary>
/// Serializes a message typed only as <see cref="object"/> through
/// <see cref="IMessageSerializer.Serialize{T}"/> closed over its runtime type.
/// </summary>
/// <remarks>
/// <see cref="IMessageSerializer.Serialize{T}"/> is generic: calling it with <c>T = object</c> would
/// lose the runtime type's properties and, with <c>EncryptingMessageSerializer</c>, its
/// <c>[EncryptedMessage]</c> attribute. The closed method is built once per type and invoked with
/// <see cref="BindingFlags.DoNotWrapExceptions"/>, so a serializer failure (for example an
/// encryption failure) propagates as itself.
/// </remarks>
internal static class MessageSerializerRuntimeType
{
    private static readonly MethodInfo SerializeMethodDefinition =
        typeof(IMessageSerializer).GetMethod(nameof(IMessageSerializer.Serialize))!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> SerializeMethodCache = new();

    /// <summary>Serializes <paramref name="message"/> using its runtime type.</summary>
    internal static string SerializeAsRuntimeType(this IMessageSerializer serializer, object message)
    {
        var serializeMethod = SerializeMethodCache.GetOrAdd(
            message.GetType(),
            static t => SerializeMethodDefinition.MakeGenericMethod(t));

        return (string)serializeMethod.Invoke(
            serializer,
            BindingFlags.DoNotWrapExceptions,
            binder: null,
            parameters: [message],
            culture: null)!;
    }
}
