using Encina.Cdc.Abstractions;
using Encina.Cdc.MongoDb;
using MongoDB.Bson;
using Shouldly;

namespace Encina.UnitTests.Cdc.MongoDb;

/// <summary>
/// Unit tests for <see cref="MongoCdcPosition"/>.
/// </summary>
public sealed class MongoCdcPositionTests
{
    #region Constructor

    [Fact]
    public void Constructor_SetsResumeToken()
    {
        var token = new BsonDocument("_data", "abc123");

        var position = new MongoCdcPosition(token);

        position.ResumeToken.ShouldBeSameAs(token);
    }

    [Fact]
    public void Constructor_NullResumeToken_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MongoCdcPosition(null!));
    }

    [Fact]
    public void Constructor_EmptyDocument_IsValid()
    {
        var token = new BsonDocument();

        var position = new MongoCdcPosition(token);

        position.ResumeToken.ShouldNotBeNull();
        position.ResumeToken.ElementCount.ShouldBe(0);
    }

    #endregion

    #region ToBytes / FromBytes Round-Trip

    [Fact]
    public void FromBytes_ToBytes_RoundTrip_SimpleDocument()
    {
        var token = new BsonDocument("_data", "resume-token-value");
        var original = new MongoCdcPosition(token);

        var bytes = original.ToBytes();
        var restored = MongoCdcPosition.FromBytes(bytes);

        restored.ResumeToken["_data"].AsString.ShouldBe("resume-token-value");
    }

    [Fact]
    public void FromBytes_ToBytes_RoundTrip_ComplexDocument()
    {
        var token = new BsonDocument
        {
            { "_data", "82abc123" },
            { "_typeBits", new BsonBinaryData(new byte[] { 1, 2, 3 }) }
        };
        var original = new MongoCdcPosition(token);

        var bytes = original.ToBytes();
        var restored = MongoCdcPosition.FromBytes(bytes);

        restored.ResumeToken["_data"].AsString.ShouldBe("82abc123");
        restored.ResumeToken.Contains("_typeBits").ShouldBeTrue();
    }

    [Fact]
    public void FromBytes_ToBytes_RoundTrip_EmptyDocument()
    {
        var original = new MongoCdcPosition(new BsonDocument());

        var bytes = original.ToBytes();
        var restored = MongoCdcPosition.FromBytes(bytes);

        restored.ResumeToken.ElementCount.ShouldBe(0);
    }

    [Fact]
    public void ToBytes_ReturnsNonEmptyArray()
    {
        var position = new MongoCdcPosition(new BsonDocument("key", "value"));

        var bytes = position.ToBytes();

        bytes.ShouldNotBeEmpty();
    }

    #endregion

    #region FromBytes Validation

    [Fact]
    public void FromBytes_NullBytes_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            MongoCdcPosition.FromBytes(null!));
    }

    #endregion

    #region CompareTo

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        var position = new MongoCdcPosition(new BsonDocument("_data", "abc"));

        position.CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_SameDocument_ReturnsZero()
    {
        var a = new MongoCdcPosition(new BsonDocument("_data", "abc"));
        var b = new MongoCdcPosition(new BsonDocument("_data", "abc"));

        a.CompareTo(b).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_DifferentDocuments_ReturnsNonZero()
    {
        var a = new MongoCdcPosition(new BsonDocument("_data", "abc"));
        var b = new MongoCdcPosition(new BsonDocument("_data", "xyz"));

        // The exact ordering depends on BsonDocument.CompareTo implementation;
        // we just verify it is consistent and non-zero.
        var result = a.CompareTo(b);
        result.ShouldNotBe(0);

        // Verify antisymmetry: if a < b then b > a
        var reverse = b.CompareTo(a);
        (result > 0 ? reverse < 0 : reverse > 0).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_EmptyDocuments_ReturnsZero()
    {
        var a = new MongoCdcPosition(new BsonDocument());
        var b = new MongoCdcPosition(new BsonDocument());

        a.CompareTo(b).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_DifferentType_ThrowsArgumentException()
    {
        var mongoPosition = new MongoCdcPosition(new BsonDocument("_data", "abc"));
        var otherPosition = new TestCdcPosition(10);

        Should.Throw<ArgumentException>(() =>
            mongoPosition.CompareTo(otherPosition));
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsResumeTokenPrefix()
    {
        var position = new MongoCdcPosition(new BsonDocument("_data", "abc"));

        var result = position.ToString();

        result.ShouldStartWith("ResumeToken:");
    }

    [Fact]
    public void ToString_ContainsDocumentRepresentation()
    {
        var token = new BsonDocument("_data", "abc");
        var position = new MongoCdcPosition(token);

        var result = position.ToString();

        result.ShouldBe($"ResumeToken:{token}");
    }

    #endregion
}
