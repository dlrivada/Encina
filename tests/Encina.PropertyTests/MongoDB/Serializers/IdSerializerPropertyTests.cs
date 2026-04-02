using Encina.IdGeneration;
using Encina.MongoDB.Serializers;
using FsCheck;
using FsCheck.Xunit;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Encina.PropertyTests.MongoDB.Serializers;

/// <summary>
/// Property-based tests verifying round-trip serialization invariants for
/// MongoDB BSON serializers of Encina ID generation types.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "MongoDB")]
public sealed class IdSerializerPropertyTests
{
    #region SnowflakeId round-trip

    [Property(MaxTest = 50)]
    public bool SnowflakeId_SerializeDeserialize_RoundTrip_Int64(PositiveInt rawValue)
    {
        var original = new SnowflakeId(rawValue.Get);
        var serializer = SnowflakeIdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        var deserialized = DeserializeFromDocument<SnowflakeId>(bsonDoc, "value",
            (context, args) => serializer.Deserialize(context, args));

        return deserialized.Value == original.Value;
    }

    [Property(MaxTest = 50)]
    public bool SnowflakeId_SerializeDeserialize_PreservesValue(long rawValue)
    {
        var original = new SnowflakeId(rawValue);
        var serializer = SnowflakeIdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        var deserialized = DeserializeFromDocument<SnowflakeId>(bsonDoc, "value",
            (context, args) => serializer.Deserialize(context, args));

        return deserialized.Value == rawValue;
    }

    #endregion

    #region UlidId round-trip

    [Property(MaxTest = 50)]
    public bool UlidId_SerializeDeserialize_RoundTrip()
    {
        var original = UlidId.NewUlid();
        var serializer = UlidIdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        var deserialized = DeserializeFromDocument<UlidId>(bsonDoc, "value",
            (context, args) => serializer.Deserialize(context, args));

        return deserialized.Equals(original);
    }

    [Property(MaxTest = 50)]
    public bool UlidId_SerializeDeserialize_StringFormat_PreservesIdentity()
    {
        var original = UlidId.NewUlid();
        var serializer = UlidIdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        // Verify it was stored as string
        var storedValue = bsonDoc["value"].AsString;
        var parsed = UlidId.Parse(storedValue);

        return parsed.Equals(original);
    }

    #endregion

    #region UuidV7Id round-trip

    [Property(MaxTest = 50)]
    public bool UuidV7Id_SerializeDeserialize_RoundTrip()
    {
        var original = UuidV7Id.NewUuidV7();
        var serializer = UuidV7IdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        var deserialized = DeserializeFromDocument<UuidV7Id>(bsonDoc, "value",
            (context, args) => serializer.Deserialize(context, args));

        return deserialized.Equals(original);
    }

    [Property(MaxTest = 50)]
    public bool UuidV7Id_SerializeDeserialize_StringFormat_PreservesGuid()
    {
        var original = UuidV7Id.NewUuidV7();
        var serializer = UuidV7IdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        var storedValue = bsonDoc["value"].AsString;
        var parsed = UuidV7Id.Parse(storedValue);

        return parsed.Equals(original);
    }

    #endregion

    #region ShardPrefixedId round-trip

    [Property(MaxTest = 50)]
    public bool ShardPrefixedId_SerializeDeserialize_RoundTrip(NonEmptyString shardId, NonEmptyString sequence)
    {
        // Filter out values that contain the delimiter ':' or whitespace
        var cleanShardId = new string(shardId.Get.Where(c => !char.IsWhiteSpace(c) && c != ':').ToArray());
        var cleanSequence = new string(sequence.Get.Where(c => !char.IsWhiteSpace(c) && c != ':').ToArray());

        if (string.IsNullOrEmpty(cleanShardId) || string.IsNullOrEmpty(cleanSequence))
        {
            return true; // Skip invalid inputs
        }

        var original = new ShardPrefixedId(cleanShardId, cleanSequence);
        var serializer = ShardPrefixedIdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        var deserialized = DeserializeFromDocument<ShardPrefixedId>(bsonDoc, "value",
            (context, args) => serializer.Deserialize(context, args));

        return deserialized.ShardId == original.ShardId
            && deserialized.Sequence == original.Sequence;
    }

    [Property(MaxTest = 50)]
    public bool ShardPrefixedId_Serialize_ProducesStringBsonType()
    {
        var original = new ShardPrefixedId("shard1", "seq001");
        var serializer = ShardPrefixedIdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        return bsonDoc["value"].BsonType == BsonType.String;
    }

    #endregion

    #region SnowflakeId serialization format

    [Property(MaxTest = 50)]
    public bool SnowflakeId_Serialize_ProducesInt64BsonType(PositiveInt rawValue)
    {
        var original = new SnowflakeId(rawValue.Get);
        var serializer = SnowflakeIdSerializer.Instance;

        var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs(),
            original));

        return bsonDoc["value"].BsonType == BsonType.Int64;
    }

    #endregion

    #region Multiple IDs maintain uniqueness

    [Property(MaxTest = 50)]
    public bool UlidId_MultipleSerializations_ProduceDistinctStrings(PositiveInt count)
    {
        var size = Math.Min(count.Get, 100);
        var serializer = UlidIdSerializer.Instance;
        var storedValues = new System.Collections.Generic.HashSet<string>(size);

        for (var i = 0; i < size; i++)
        {
            var id = UlidId.NewUlid();
            var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
                BsonSerializationContext.CreateRoot(writer),
                new BsonSerializationArgs(),
                id));

            storedValues.Add(bsonDoc["value"].AsString);
        }

        return storedValues.Count == size;
    }

    [Property(MaxTest = 50)]
    public bool UuidV7Id_MultipleSerializations_ProduceDistinctStrings(PositiveInt count)
    {
        var size = Math.Min(count.Get, 100);
        var serializer = UuidV7IdSerializer.Instance;
        var storedValues = new System.Collections.Generic.HashSet<string>(size);

        for (var i = 0; i < size; i++)
        {
            var id = UuidV7Id.NewUuidV7();
            var bsonDoc = SerializeToDocument("value", writer => serializer.Serialize(
                BsonSerializationContext.CreateRoot(writer),
                new BsonSerializationArgs(),
                id));

            storedValues.Add(bsonDoc["value"].AsString);
        }

        return storedValues.Count == size;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Serializes a single value into a BsonDocument wrapper.
    /// </summary>
    private static BsonDocument SerializeToDocument(string fieldName, Action<IBsonWriter> serializeValue)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        writer.WriteStartDocument();
        writer.WriteName(fieldName);
        serializeValue(writer);
        writer.WriteEndDocument();
        return document;
    }

    /// <summary>
    /// Deserializes a value from a BsonDocument wrapper.
    /// </summary>
    private static T DeserializeFromDocument<T>(
        BsonDocument document,
        string fieldName,
        Func<BsonDeserializationContext, BsonDeserializationArgs, T> deserialize)
    {
        using var reader = new BsonDocumentReader(document);
        reader.ReadStartDocument();
        reader.ReadName(fieldName);
        var context = BsonDeserializationContext.CreateRoot(reader);
        var args = new BsonDeserializationArgs();
        return deserialize(context, args);
    }

    #endregion
}
