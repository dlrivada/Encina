using Encina.Cdc.MongoDb;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using MongoDB.Bson;

namespace Encina.PropertyTests.Cdc.MongoDb;

/// <summary>
/// Property-based tests for <see cref="MongoCdcPosition"/> invariants.
/// Verifies serialization round-trips using BsonDocument resume tokens.
/// </summary>
[Trait("Category", "Property")]
public sealed class MongoCdcPositionPropertyTests
{
    #region ToBytes/FromBytes Round-Trip

    /// <summary>
    /// Property: For any simple BsonDocument, ToBytes then FromBytes returns an equivalent document.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_RoundTrip_PreservesResumeToken()
    {
        return Prop.ForAll(
            Arb.From(GenSimpleBsonDocument()),
            doc =>
            {
                var original = new MongoCdcPosition(doc);
                var restored = MongoCdcPosition.FromBytes(original.ToBytes());

                return restored.ResumeToken == original.ResumeToken;
            });
    }

    /// <summary>
    /// Property: Round-tripped positions compare equal to the original.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_RoundTrip_ComparesToZero()
    {
        return Prop.ForAll(
            Arb.From(GenSimpleBsonDocument()),
            doc =>
            {
                var original = new MongoCdcPosition(doc);
                var restored = MongoCdcPosition.FromBytes(original.ToBytes());

                return original.CompareTo(restored) == 0;
            });
    }

    /// <summary>
    /// Property: ToBytes produces a non-empty byte array.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ToBytes_ProducesNonEmptyBytes()
    {
        return Prop.ForAll(
            Arb.From(GenSimpleBsonDocument()),
            doc =>
            {
                var position = new MongoCdcPosition(doc);

                return position.ToBytes().Length > 0;
            });
    }

    /// <summary>
    /// Property: ToBytes is deterministic (same document produces same bytes).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ToBytes_IsDeterministic()
    {
        return Prop.ForAll(
            Arb.From(GenSimpleBsonDocument()),
            doc =>
            {
                var a = new MongoCdcPosition(doc);
                var b = new MongoCdcPosition(doc);

                return a.ToBytes().SequenceEqual(b.ToBytes());
            });
    }

    #endregion

    #region CompareTo

    /// <summary>
    /// Property: CompareTo is reflexive (a.CompareTo(a) == 0).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_CompareTo_IsReflexive()
    {
        return Prop.ForAll(
            Arb.From(GenSimpleBsonDocument()),
            doc =>
            {
                var position = new MongoCdcPosition(doc);

                return position.CompareTo(position) == 0;
            });
    }

    /// <summary>
    /// Property: CompareTo with null always returns positive.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_CompareTo_NullReturnsPositive()
    {
        return Prop.ForAll(
            Arb.From(GenSimpleBsonDocument()),
            doc =>
            {
                var position = new MongoCdcPosition(doc);

                return position.CompareTo(null) > 0;
            });
    }

    #endregion

    #region ToString

    /// <summary>
    /// Property: ToString always returns a non-null, non-empty string.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ToString_IsNonEmpty()
    {
        return Prop.ForAll(
            Arb.From(GenSimpleBsonDocument()),
            doc =>
            {
                var position = new MongoCdcPosition(doc);

                return !string.IsNullOrWhiteSpace(position.ToString());
            });
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates simple BsonDocuments with a random key and integer value,
    /// suitable for use as resume token stand-ins.
    /// </summary>
    private static Gen<BsonDocument> GenSimpleBsonDocument()
    {
        return Gen.Elements("_data", "token", "pos", "id", "key")
            .SelectMany(key =>
                Gen.Choose(1, 1_000_000).Select(value =>
                    new BsonDocument(key, new BsonInt32(value))));
    }

    #endregion
}
