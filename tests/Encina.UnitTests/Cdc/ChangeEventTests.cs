using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="ChangeEvent"/> record.
/// </summary>
public sealed class ChangeEventTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var position = new TestCdcPosition(42);
        var metadata = new ChangeMetadata(position, FixedUtcNow, "tx-1", "mydb", "dbo");

        // Act
        var evt = new ChangeEvent("Orders", ChangeOperation.Insert, null, new { Id = 1 }, metadata);

        // Assert
        evt.TableName.ShouldBe("Orders");
        evt.Operation.ShouldBe(ChangeOperation.Insert);
        evt.Before.ShouldBeNull();
        evt.After.ShouldNotBeNull();
        evt.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void Constructor_UpdateOperation_HasBothBeforeAndAfter()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var before = new { Name = "Old" };
        var after = new { Name = "New" };

        var evt = new ChangeEvent("Products", ChangeOperation.Update, before, after, metadata);

        evt.Operation.ShouldBe(ChangeOperation.Update);
        evt.Before.ShouldBe(before);
        evt.After.ShouldBe(after);
    }

    [Fact]
    public void Constructor_DeleteOperation_HasBeforeOnly()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var before = new { Id = 5 };

        var evt = new ChangeEvent("Users", ChangeOperation.Delete, before, null, metadata);

        evt.Operation.ShouldBe(ChangeOperation.Delete);
        evt.Before.ShouldBe(before);
        evt.After.ShouldBeNull();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, "tx", "db", "schema");
        var after = "data";

        var evt1 = new ChangeEvent("T", ChangeOperation.Insert, null, after, metadata);
        var evt2 = new ChangeEvent("T", ChangeOperation.Insert, null, after, metadata);

        evt1.ShouldBe(evt2);
    }

    [Fact]
    public void Equality_DifferentTableName_AreNotEqual()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);

        var evt1 = new ChangeEvent("Table1", ChangeOperation.Insert, null, "x", metadata);
        var evt2 = new ChangeEvent("Table2", ChangeOperation.Insert, null, "x", metadata);

        evt1.ShouldNotBe(evt2);
    }

    [Fact]
    public void WithExpression_ChangingOperation_CreatesNewInstance()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var original = new ChangeEvent("T", ChangeOperation.Insert, null, "data", metadata);

        var modified = original with { Operation = ChangeOperation.Snapshot };

        modified.Operation.ShouldBe(ChangeOperation.Snapshot);
        modified.TableName.ShouldBe(original.TableName);
        original.Operation.ShouldBe(ChangeOperation.Insert);
    }
}
