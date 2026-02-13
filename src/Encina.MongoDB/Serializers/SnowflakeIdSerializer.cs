using Encina.IdGeneration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Encina.MongoDB.Serializers;

/// <summary>
/// BSON serializer for <see cref="SnowflakeId"/>, stored as a BSON Int64.
/// </summary>
public sealed class SnowflakeIdSerializer : SerializerBase<SnowflakeId>
{
    /// <summary>
    /// Gets a singleton instance of the serializer.
    /// </summary>
    public static readonly SnowflakeIdSerializer Instance = new();

    /// <inheritdoc />
    public override SnowflakeId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        ArgumentNullException.ThrowIfNull(context);

        var bsonType = context.Reader.GetCurrentBsonType();
        return bsonType switch
        {
            BsonType.Int64 => new SnowflakeId(context.Reader.ReadInt64()),
            BsonType.Int32 => new SnowflakeId(context.Reader.ReadInt32()),
            BsonType.String => SnowflakeId.Parse(context.Reader.ReadString()),
            _ => throw new BsonSerializationException(
                $"Cannot deserialize SnowflakeId from BsonType {bsonType}.")
        };
    }

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, SnowflakeId value)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Writer.WriteInt64(value.Value);
    }
}
