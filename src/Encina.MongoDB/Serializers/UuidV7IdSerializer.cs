using Encina.IdGeneration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Encina.MongoDB.Serializers;

/// <summary>
/// BSON serializer for <see cref="UuidV7Id"/>, stored as a BSON String (standard GUID format).
/// </summary>
/// <remarks>
/// Stored as string rather than Binary for human readability in MongoDB queries and indexes.
/// </remarks>
public sealed class UuidV7IdSerializer : SerializerBase<UuidV7Id>
{
    /// <summary>
    /// Gets a singleton instance of the serializer.
    /// </summary>
    public static readonly UuidV7IdSerializer Instance = new();

    /// <inheritdoc />
    public override UuidV7Id Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        ArgumentNullException.ThrowIfNull(context);

        var bsonType = context.Reader.GetCurrentBsonType();
        return bsonType switch
        {
            BsonType.String => UuidV7Id.Parse(context.Reader.ReadString()),
            BsonType.Binary => new UuidV7Id(context.Reader.ReadBinaryData().ToGuid()),
            _ => throw new BsonSerializationException(
                $"Cannot deserialize UuidV7Id from BsonType {bsonType}.")
        };
    }

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UuidV7Id value)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Writer.WriteString(value.Value.ToString());
    }
}
