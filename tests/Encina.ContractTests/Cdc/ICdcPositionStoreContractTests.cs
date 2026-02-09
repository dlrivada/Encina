using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Cdc;

/// <summary>
/// Contract tests verifying that any <see cref="ICdcPositionStore"/> implementation
/// correctly satisfies the interface contract for position persistence, retrieval,
/// deletion, and case-insensitive connector identity.
/// Uses <see cref="InMemoryCdcPositionStore"/> as the concrete implementation under test.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ICdcPositionStoreContractTests
{
    #region Test Helpers

    /// <summary>
    /// Test-only CDC position backed by a simple <see cref="long"/> value.
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

    private static InMemoryCdcPositionStore CreateStore() => new();

    #endregion

    #region GetPositionAsync Contract

    /// <summary>
    /// Contract: GetPositionAsync must return <see cref="Option{A}.None"/> when no
    /// position has been saved for the given connector identifier.
    /// </summary>
    [Fact]
    public async Task Contract_GetPosition_ReturnsNone_ForNonExistentConnector()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetPositionAsync("non-existent-connector");

        // Assert
        result.IsRight.ShouldBeTrue("GetPositionAsync must return Right for non-existent connector");

        result.IfRight(option =>
            option.IsNone.ShouldBeTrue("GetPositionAsync must return None when connector has no saved position"));
    }

    #endregion

    #region SavePositionAsync / GetPositionAsync Round-Trip Contract

    /// <summary>
    /// Contract: A position saved via SavePositionAsync must be retrievable
    /// via GetPositionAsync for the same connector identifier.
    /// </summary>
    [Fact]
    public async Task Contract_SaveThenGet_ReturnsSavedPosition()
    {
        // Arrange
        var store = CreateStore();
        var connectorId = "test-connector";
        var position = new TestCdcPosition(42);

        // Act
        var saveResult = await store.SavePositionAsync(connectorId, position);
        var getResult = await store.GetPositionAsync(connectorId);

        // Assert
        saveResult.IsRight.ShouldBeTrue("SavePositionAsync must return Right on success");

        getResult.IsRight.ShouldBeTrue("GetPositionAsync must return Right on success");
        getResult.IfRight(option =>
        {
            option.IsSome.ShouldBeTrue("GetPositionAsync must return Some after a successful save");
            option.IfSome(retrieved =>
            {
                retrieved.ShouldBeOfType<TestCdcPosition>();
                ((TestCdcPosition)retrieved).Value.ShouldBe(42L,
                    "Retrieved position must match the saved position value");
            });
        });
    }

    /// <summary>
    /// Contract: SavePositionAsync must overwrite a previously stored position
    /// for the same connector identifier.
    /// </summary>
    [Fact]
    public async Task Contract_Save_OverwritesPreviousPosition()
    {
        // Arrange
        var store = CreateStore();
        var connectorId = "test-connector";

        // Act
        await store.SavePositionAsync(connectorId, new TestCdcPosition(10));
        await store.SavePositionAsync(connectorId, new TestCdcPosition(20));
        var getResult = await store.GetPositionAsync(connectorId);

        // Assert
        getResult.IsRight.ShouldBeTrue("GetPositionAsync must return Right on success");
        getResult.IfRight(option =>
        {
            option.IsSome.ShouldBeTrue("Position must exist after overwrite");
            option.IfSome(retrieved =>
            {
                ((TestCdcPosition)retrieved).Value.ShouldBe(20L,
                    "Position must reflect the latest saved value, not the original");
            });
        });
    }

    #endregion

    #region DeletePositionAsync Contract

    /// <summary>
    /// Contract: DeletePositionAsync must remove a previously saved position so that
    /// subsequent GetPositionAsync returns <see cref="Option{A}.None"/>.
    /// </summary>
    [Fact]
    public async Task Contract_Delete_RemovesSavedPosition()
    {
        // Arrange
        var store = CreateStore();
        var connectorId = "test-connector";
        await store.SavePositionAsync(connectorId, new TestCdcPosition(100));

        // Act
        var deleteResult = await store.DeletePositionAsync(connectorId);
        var getResult = await store.GetPositionAsync(connectorId);

        // Assert
        deleteResult.IsRight.ShouldBeTrue("DeletePositionAsync must return Right on success");

        getResult.IsRight.ShouldBeTrue("GetPositionAsync must return Right after deletion");
        getResult.IfRight(option =>
            option.IsNone.ShouldBeTrue("GetPositionAsync must return None after position has been deleted"));
    }

    /// <summary>
    /// Contract: DeletePositionAsync must succeed (return Right) when the connector
    /// identifier does not exist. It must not treat a missing position as an error.
    /// </summary>
    [Fact]
    public async Task Contract_Delete_NonExistentConnector_ReturnsSuccess()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.DeletePositionAsync("non-existent-connector");

        // Assert
        result.IsRight.ShouldBeTrue(
            "DeletePositionAsync must return Right (success) for non-existent connector");
    }

    #endregion

    #region Case-Insensitive Connector Identity Contract

    /// <summary>
    /// Contract: Connector identifiers must be treated as case-insensitive.
    /// Saving with one casing and retrieving with another must return the same position.
    /// </summary>
    [Fact]
    public async Task Contract_ConnectorIds_AreCaseInsensitive()
    {
        // Arrange
        var store = CreateStore();
        var position = new TestCdcPosition(55);

        // Act
        await store.SavePositionAsync("My-Connector", position);
        var getResult = await store.GetPositionAsync("my-connector");

        // Assert
        getResult.IsRight.ShouldBeTrue("GetPositionAsync must return Right on success");
        getResult.IfRight(option =>
        {
            option.IsSome.ShouldBeTrue(
                "Connector IDs must be case-insensitive: 'My-Connector' and 'my-connector' should refer to the same position");
            option.IfSome(retrieved =>
                ((TestCdcPosition)retrieved).Value.ShouldBe(55L,
                    "Retrieved position must match the saved value regardless of connector ID casing"));
        });
    }

    /// <summary>
    /// Contract: Deleting a position with a differently-cased connector identifier
    /// must successfully remove the position saved under the original casing.
    /// </summary>
    [Fact]
    public async Task Contract_Delete_CaseInsensitive_RemovesPosition()
    {
        // Arrange
        var store = CreateStore();
        await store.SavePositionAsync("MY-CONNECTOR", new TestCdcPosition(77));

        // Act
        await store.DeletePositionAsync("my-connector");
        var getResult = await store.GetPositionAsync("My-Connector");

        // Assert
        getResult.IsRight.ShouldBeTrue("GetPositionAsync must return Right after deletion");
        getResult.IfRight(option =>
            option.IsNone.ShouldBeTrue(
                "Deletion with different casing must still remove the position"));
    }

    #endregion

    #region Multiple Connectors Independence Contract

    /// <summary>
    /// Contract: Positions for different connector identifiers must be stored
    /// independently. Saving, retrieving, or deleting one connector's position
    /// must not affect another connector's position.
    /// </summary>
    [Fact]
    public async Task Contract_MultipleConnectors_AreIndependent()
    {
        // Arrange
        var store = CreateStore();

        // Act
        await store.SavePositionAsync("connector-a", new TestCdcPosition(100));
        await store.SavePositionAsync("connector-b", new TestCdcPosition(200));

        var resultA = await store.GetPositionAsync("connector-a");
        var resultB = await store.GetPositionAsync("connector-b");

        // Assert
        resultA.IfRight(option =>
            option.IfSome(pos =>
                ((TestCdcPosition)pos).Value.ShouldBe(100L,
                    "Connector A position must be independent from Connector B")));

        resultB.IfRight(option =>
            option.IfSome(pos =>
                ((TestCdcPosition)pos).Value.ShouldBe(200L,
                    "Connector B position must be independent from Connector A")));
    }

    /// <summary>
    /// Contract: Deleting one connector's position must not affect other connectors.
    /// </summary>
    [Fact]
    public async Task Contract_DeleteOneConnector_DoesNotAffectOthers()
    {
        // Arrange
        var store = CreateStore();
        await store.SavePositionAsync("connector-x", new TestCdcPosition(10));
        await store.SavePositionAsync("connector-y", new TestCdcPosition(20));

        // Act
        await store.DeletePositionAsync("connector-x");

        var resultX = await store.GetPositionAsync("connector-x");
        var resultY = await store.GetPositionAsync("connector-y");

        // Assert
        resultX.IfRight(option =>
            option.IsNone.ShouldBeTrue("Deleted connector-x must return None"));

        resultY.IfRight(option =>
        {
            option.IsSome.ShouldBeTrue("connector-y must still have its position after connector-x deletion");
            option.IfSome(pos =>
                ((TestCdcPosition)pos).Value.ShouldBe(20L,
                    "connector-y position must be unaffected by connector-x deletion"));
        });
    }

    #endregion
}
