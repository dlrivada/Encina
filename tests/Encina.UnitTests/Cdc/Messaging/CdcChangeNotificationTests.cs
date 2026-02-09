using Encina.Cdc;
using Encina.Cdc.Messaging;
using Shouldly;

namespace Encina.UnitTests.Cdc.Messaging;

/// <summary>
/// Unit tests for <see cref="CdcChangeNotification"/>.
/// </summary>
public sealed class CdcChangeNotificationTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    #region FromChangeEvent

    [Fact]
    public void FromChangeEvent_DefaultPattern_UsesTableNameAndOperation()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var evt = new ChangeEvent("Orders", ChangeOperation.Insert, null, new { Id = 1 }, metadata);

        var notification = CdcChangeNotification.FromChangeEvent(evt);

        notification.TopicName.ShouldBe("Orders.insert");
    }

    [Fact]
    public void FromChangeEvent_CustomPattern_ReplacesPlaceholders()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var evt = new ChangeEvent("Products", ChangeOperation.Update, "old", "new", metadata);

        var notification = CdcChangeNotification.FromChangeEvent(evt, "cdc.{tableName}.{operation}");

        notification.TopicName.ShouldBe("cdc.Products.update");
    }

    [Fact]
    public void FromChangeEvent_CopiesAllProperties()
    {
        var position = new TestCdcPosition(42);
        var metadata = new ChangeMetadata(position, FixedUtcNow, "tx-1", "mydb", "dbo");
        var before = new { Name = "Old" };
        var after = new { Name = "New" };
        var evt = new ChangeEvent("Users", ChangeOperation.Update, before, after, metadata);

        var notification = CdcChangeNotification.FromChangeEvent(evt);

        notification.TableName.ShouldBe("Users");
        notification.Operation.ShouldBe(ChangeOperation.Update);
        notification.Before.ShouldBe(before);
        notification.After.ShouldBe(after);
        notification.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void FromChangeEvent_NullChangeEvent_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            CdcChangeNotification.FromChangeEvent(null!));
    }

    [Fact]
    public void FromChangeEvent_NullTopicPattern_ThrowsArgumentException()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var evt = new ChangeEvent("T", ChangeOperation.Insert, null, "data", metadata);

        Should.Throw<ArgumentException>(() =>
            CdcChangeNotification.FromChangeEvent(evt, null!));
    }

    [Fact]
    public void FromChangeEvent_EmptyTopicPattern_ThrowsArgumentException()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var evt = new ChangeEvent("T", ChangeOperation.Insert, null, "data", metadata);

        Should.Throw<ArgumentException>(() =>
            CdcChangeNotification.FromChangeEvent(evt, ""));
    }

    [Theory]
    [InlineData(ChangeOperation.Insert, "insert")]
    [InlineData(ChangeOperation.Update, "update")]
    [InlineData(ChangeOperation.Delete, "delete")]
    [InlineData(ChangeOperation.Snapshot, "snapshot")]
    public void FromChangeEvent_OperationPlaceholder_UsesLowercase(
        ChangeOperation operation,
        string expectedSuffix)
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var evt = new ChangeEvent("T", operation, null, "data", metadata);

        var notification = CdcChangeNotification.FromChangeEvent(evt, "{tableName}.{operation}");

        notification.TopicName.ShouldBe($"T.{expectedSuffix}");
    }

    [Fact]
    public void FromChangeEvent_PatternWithoutPlaceholders_UsesLiteralPattern()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        var evt = new ChangeEvent("Orders", ChangeOperation.Insert, null, "data", metadata);

        var notification = CdcChangeNotification.FromChangeEvent(evt, "fixed-topic");

        notification.TopicName.ShouldBe("fixed-topic");
    }

    #endregion

    #region INotification

    [Fact]
    public void CdcChangeNotification_ImplementsINotification()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);

        var notification = new CdcChangeNotification(
            "T", ChangeOperation.Insert, null, "data", metadata, "topic");

        notification.ShouldBeAssignableTo<INotification>();
    }

    #endregion
}
