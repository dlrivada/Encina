using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="ChangeMetadata"/> record.
/// </summary>
public sealed class ChangeMetadataTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        var position = new TestCdcPosition(100);

        var metadata = new ChangeMetadata(position, FixedUtcNow, "tx-123", "MyDatabase", "dbo");

        metadata.Position.ShouldBe(position);
        metadata.CapturedAtUtc.ShouldBe(FixedUtcNow);
        metadata.TransactionId.ShouldBe("tx-123");
        metadata.SourceDatabase.ShouldBe("MyDatabase");
        metadata.SourceSchema.ShouldBe("dbo");
    }

    [Fact]
    public void Constructor_WithNullOptionalParameters_AllowsNull()
    {
        var position = new TestCdcPosition(1);

        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);

        metadata.TransactionId.ShouldBeNull();
        metadata.SourceDatabase.ShouldBeNull();
        metadata.SourceSchema.ShouldBeNull();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var position = new TestCdcPosition(50);
        var m1 = new ChangeMetadata(position, FixedUtcNow, "tx", "db", "sch");
        var m2 = new ChangeMetadata(position, FixedUtcNow, "tx", "db", "sch");

        m1.ShouldBe(m2);
    }

    [Fact]
    public void Equality_DifferentPosition_AreNotEqual()
    {
        var m1 = new ChangeMetadata(new TestCdcPosition(1), FixedUtcNow, null, null, null);
        var m2 = new ChangeMetadata(new TestCdcPosition(2), FixedUtcNow, null, null, null);

        m1.ShouldNotBe(m2);
    }

    [Fact]
    public void WithExpression_ChangingTimestamp_CreatesNewInstance()
    {
        var position = new TestCdcPosition(1);
        var original = new ChangeMetadata(position, FixedUtcNow, "tx", "db", "sch");
        var newTime = FixedUtcNow.AddHours(1);

        var modified = original with { CapturedAtUtc = newTime };

        modified.CapturedAtUtc.ShouldBe(newTime);
        original.CapturedAtUtc.ShouldBe(FixedUtcNow);
    }
}
