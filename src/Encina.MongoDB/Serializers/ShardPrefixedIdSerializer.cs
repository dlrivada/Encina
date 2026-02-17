using Encina.IdGeneration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Encina.MongoDB.Serializers;

/// <summary>
/// BSON serializer for <see cref="ShardPrefixedId"/>, stored as a BSON String.
/// </summary>
public sealed class ShardPrefixedIdSerializer : SerializerBase<ShardPrefixedId>
{
    /// <summary>
    /// Gets a singleton instance of the serializer.
    /// </summary>
    public static readonly ShardPrefixedIdSerializer Instance = new();

    /// <inheritdoc />
    public override ShardPrefixedId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        ArgumentNullException.ThrowIfNull(context);

        var bsonType = context.Reader.GetCurrentBsonType();
        return bsonType switch
        {
            BsonType.String => ShardPrefixedId.Parse(context.Reader.ReadString()),
            _ => throw new BsonSerializationException(
                $"Cannot deserialize ShardPrefixedId from BsonType {bsonType}.")
        };
    }

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ShardPrefixedId value)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Writer.WriteString(value.ToString());
    }
}
