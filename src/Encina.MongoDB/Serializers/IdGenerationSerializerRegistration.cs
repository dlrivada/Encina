using MongoDB.Bson.Serialization;

namespace Encina.MongoDB.Serializers;

/// <summary>
/// Registers all Encina ID generation BSON serializers with the MongoDB driver.
/// </summary>
public static class IdGenerationSerializerRegistration
{
    private static bool _isRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures all ID generation serializers are registered with <see cref="BsonSerializer"/>.
    /// This method is thread-safe and idempotent.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_isRegistered) return;

        lock (_lock)
        {
            if (_isRegistered) return;

            BsonSerializer.RegisterSerializer(SnowflakeIdSerializer.Instance);
            BsonSerializer.RegisterSerializer(UlidIdSerializer.Instance);
            BsonSerializer.RegisterSerializer(UuidV7IdSerializer.Instance);
            BsonSerializer.RegisterSerializer(ShardPrefixedIdSerializer.Instance);

            _isRegistered = true;
        }
    }
}
