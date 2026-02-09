using Encina.Cdc.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Encina.Cdc.MongoDb;

/// <summary>
/// Represents a CDC position based on a MongoDB Change Stream resume token.
/// The resume token is a <see cref="BsonDocument"/> that can be used to resume
/// a Change Stream from a specific point.
/// </summary>
public sealed class MongoCdcPosition : CdcPosition
{
    /// <summary>
    /// Gets the Change Stream resume token.
    /// </summary>
    public BsonDocument ResumeToken { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoCdcPosition"/> class.
    /// </summary>
    /// <param name="resumeToken">The Change Stream resume token.</param>
    public MongoCdcPosition(BsonDocument resumeToken)
    {
        ArgumentNullException.ThrowIfNull(resumeToken);
        ResumeToken = resumeToken;
    }

    /// <summary>
    /// Creates a <see cref="MongoCdcPosition"/> from a byte array previously produced by <see cref="ToBytes"/>.
    /// </summary>
    /// <param name="bytes">A BSON-serialized resume token.</param>
    /// <returns>A new <see cref="MongoCdcPosition"/>.</returns>
    public static MongoCdcPosition FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        using var stream = new MemoryStream(bytes);
        using var reader = new BsonBinaryReader(stream);

        var document = BsonSerializer.Deserialize<BsonDocument>(reader);
        return new MongoCdcPosition(document);
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BsonBinaryWriter(stream);

        BsonSerializer.Serialize(writer, ResumeToken);
        return stream.ToArray();
    }

    /// <inheritdoc />
    public override int CompareTo(CdcPosition? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other is not MongoCdcPosition mongoPosition)
        {
            throw new ArgumentException(
                $"Cannot compare MongoCdcPosition with {other.GetType().Name}.",
                nameof(other));
        }

        // Resume tokens are opaque; compare their BSON representation
        return ResumeToken.CompareTo(mongoPosition.ResumeToken);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"ResumeToken:{ResumeToken}";
    }
}
