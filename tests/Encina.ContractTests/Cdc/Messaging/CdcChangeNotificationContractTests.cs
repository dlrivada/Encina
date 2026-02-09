using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;

namespace Encina.ContractTests.Cdc.Messaging;

/// <summary>
/// Contract tests verifying that <see cref="CdcChangeNotification"/> correctly implements
/// the <see cref="INotification"/> interface and that the factory method
/// <see cref="CdcChangeNotification.FromChangeEvent"/> produces valid, well-formed
/// notifications with properly formatted topic names.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CdcChangeNotificationContractTests
{
    #region Test Helpers

    /// <summary>
    /// Test-only CDC position for constructing <see cref="ChangeMetadata"/>.
    /// </summary>
    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;

        public long Value { get; }

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static ChangeEvent CreateChangeEvent(
        string tableName = "Orders",
        ChangeOperation operation = ChangeOperation.Insert,
        object? before = null,
        object? after = null)
    {
        var metadata = new ChangeMetadata(
            new TestCdcPosition(1),
            DateTime.UtcNow,
            TransactionId: null,
            SourceDatabase: null,
            SourceSchema: null);

        return new ChangeEvent(tableName, operation, before, after ?? new { Id = 1 }, metadata);
    }

    #endregion

    #region INotification Implementation Contract

    /// <summary>
    /// Contract: <see cref="CdcChangeNotification"/> must implement
    /// <see cref="INotification"/> to be publishable through the Encina messaging pipeline.
    /// </summary>
    [Fact]
    public void Contract_CdcChangeNotification_ImplementsINotification()
    {
        typeof(INotification).IsAssignableFrom(typeof(CdcChangeNotification))
            .ShouldBeTrue("CdcChangeNotification must implement INotification");
    }

    /// <summary>
    /// Contract: An instance of <see cref="CdcChangeNotification"/> must be assignable
    /// to <see cref="INotification"/> for polymorphic handling.
    /// </summary>
    [Fact]
    public void Contract_CdcChangeNotification_Instance_IsINotification()
    {
        // Arrange
        var changeEvent = CreateChangeEvent();
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.ShouldBeAssignableTo<INotification>(
            "CdcChangeNotification instances must be assignable to INotification");
    }

    #endregion

    #region FromChangeEvent Factory Method Contract

    /// <summary>
    /// Contract: <see cref="CdcChangeNotification.FromChangeEvent"/> must produce
    /// a valid notification that preserves the table name from the source change event.
    /// </summary>
    [Fact]
    public void Contract_FromChangeEvent_PreservesTableName()
    {
        // Arrange
        var changeEvent = CreateChangeEvent(tableName: "Customers");

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.TableName.ShouldBe("Customers",
            "FromChangeEvent must preserve the table name from the source ChangeEvent");
    }

    /// <summary>
    /// Contract: <see cref="CdcChangeNotification.FromChangeEvent"/> must preserve
    /// the operation type from the source change event.
    /// </summary>
    [Fact]
    public void Contract_FromChangeEvent_PreservesOperation()
    {
        // Arrange
        var changeEvent = CreateChangeEvent(operation: ChangeOperation.Update);

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.Operation.ShouldBe(ChangeOperation.Update,
            "FromChangeEvent must preserve the operation from the source ChangeEvent");
    }

    /// <summary>
    /// Contract: <see cref="CdcChangeNotification.FromChangeEvent"/> must preserve
    /// the before and after payloads from the source change event.
    /// </summary>
    [Fact]
    public void Contract_FromChangeEvent_PreservesBeforeAndAfter()
    {
        // Arrange
        var before = new { Id = 1, Name = "Old" };
        var after = new { Id = 1, Name = "New" };
        var changeEvent = CreateChangeEvent(
            operation: ChangeOperation.Update,
            before: before,
            after: after);

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.Before.ShouldBe(before,
            "FromChangeEvent must preserve the Before payload");
        notification.After.ShouldBe(after,
            "FromChangeEvent must preserve the After payload");
    }

    /// <summary>
    /// Contract: <see cref="CdcChangeNotification.FromChangeEvent"/> must preserve
    /// the metadata from the source change event.
    /// </summary>
    [Fact]
    public void Contract_FromChangeEvent_PreservesMetadata()
    {
        // Arrange
        var changeEvent = CreateChangeEvent();

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.Metadata.ShouldBe(changeEvent.Metadata,
            "FromChangeEvent must preserve the Metadata from the source ChangeEvent");
    }

    #endregion

    #region TopicName Formatting Contract

    /// <summary>
    /// Contract: The default topic pattern <c>{tableName}.{operation}</c> must produce
    /// a topic name in the format "TableName.operation" (operation is lower-case).
    /// </summary>
    [Fact]
    public void Contract_TopicName_DefaultPattern_IsProperlyFormatted()
    {
        // Arrange
        var changeEvent = CreateChangeEvent(tableName: "Orders", operation: ChangeOperation.Insert);

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.TopicName.ShouldBe("Orders.insert",
            "Default topic pattern must produce 'TableName.operation' with lower-case operation");
    }

    /// <summary>
    /// Contract: Custom topic patterns must be correctly applied, replacing
    /// <c>{tableName}</c> and <c>{operation}</c> placeholders.
    /// </summary>
    [Fact]
    public void Contract_TopicName_CustomPattern_IsProperlyFormatted()
    {
        // Arrange
        var changeEvent = CreateChangeEvent(tableName: "Products", operation: ChangeOperation.Delete);

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(
            changeEvent,
            topicPattern: "cdc.{tableName}.{operation}");

        // Assert
        notification.TopicName.ShouldBe("cdc.Products.delete",
            "Custom topic pattern must replace {tableName} and {operation} placeholders");
    }

    /// <summary>
    /// Contract: Topic name must correctly handle all <see cref="ChangeOperation"/> values.
    /// </summary>
    [Theory]
    [InlineData(ChangeOperation.Insert, "Orders.insert")]
    [InlineData(ChangeOperation.Update, "Orders.update")]
    [InlineData(ChangeOperation.Delete, "Orders.delete")]
    [InlineData(ChangeOperation.Snapshot, "Orders.snapshot")]
    public void Contract_TopicName_AllOperations_AreProperlyFormatted(
        ChangeOperation operation,
        string expectedTopicName)
    {
        // Arrange
        var changeEvent = CreateChangeEvent(tableName: "Orders", operation: operation);

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.TopicName.ShouldBe(expectedTopicName,
            $"Topic name for {operation} must be properly formatted");
    }

    /// <summary>
    /// Contract: Topic name must not be null or whitespace regardless of the
    /// combination of table name and operation.
    /// </summary>
    [Fact]
    public void Contract_TopicName_IsNeverNullOrWhitespace()
    {
        // Arrange
        var changeEvent = CreateChangeEvent();

        // Act
        var notification = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification.TopicName.ShouldNotBeNullOrWhiteSpace(
            "TopicName must never be null or whitespace");
    }

    #endregion

    #region Record Semantics Contract

    /// <summary>
    /// Contract: <see cref="CdcChangeNotification"/> must be a sealed record type.
    /// </summary>
    [Fact]
    public void Contract_CdcChangeNotification_IsSealed()
    {
        typeof(CdcChangeNotification).IsSealed.ShouldBeTrue(
            "CdcChangeNotification must be sealed");
    }

    /// <summary>
    /// Contract: <see cref="CdcChangeNotification"/> must be a record type
    /// with value-based equality semantics.
    /// </summary>
    [Fact]
    public void Contract_CdcChangeNotification_IsRecord()
    {
        // Records have a compiler-generated <Clone>$ method
        var cloneMethod = typeof(CdcChangeNotification).GetMethod(
            "<Clone>$",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        cloneMethod.ShouldNotBeNull(
            "CdcChangeNotification must be a record type (missing <Clone>$ method)");
    }

    /// <summary>
    /// Contract: Two notifications created from the same change event and pattern
    /// must be equal (record value-based equality).
    /// </summary>
    [Fact]
    public void Contract_CdcChangeNotification_ValueEquality()
    {
        // Arrange
        var changeEvent = CreateChangeEvent(tableName: "Orders", operation: ChangeOperation.Insert);

        // Act
        var notification1 = CdcChangeNotification.FromChangeEvent(changeEvent);
        var notification2 = CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        notification1.ShouldBe(notification2,
            "Notifications created from the same ChangeEvent must be equal (record value semantics)");
    }

    #endregion
}
