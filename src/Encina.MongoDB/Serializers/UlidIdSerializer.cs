using Encina.IdGeneration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Encina.MongoDB.Serializers;

/// <summary>
/// BSON serializer for <see cref="UlidId"/>, stored as a BSON String (Crockford Base32).
/// </summary>
public sealed class UlidIdSerializer : SerializerBase<UlidId>
{
    /// <summary>
    /// Gets a singleton instance of the serializer.
    /// </summary>
    public static readonly UlidIdSerializer Instance = new();

    /// <inheritdoc />
    public override UlidId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        ArgumentNullException.ThrowIfNull(context);

        var bsonType = context.Reader.GetCurrentBsonType();
        return bsonType switch
        {
            BsonType.String => UlidId.Parse(context.Reader.ReadString()),
            BsonType.Binary => new UlidId(context.Reader.ReadBinaryData().Bytes),
            _ => throw new BsonSerializationException(
                $"Cannot deserialize UlidId from BsonType {bsonType}.")
        };
    }

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UlidId value)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Writer.WriteString(value.ToString());
    }
}
