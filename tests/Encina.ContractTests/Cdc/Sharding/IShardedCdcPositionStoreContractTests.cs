using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Cdc.Sharding;

/// <summary>
/// Contract tests verifying that any <see cref="IShardedCdcPositionStore"/> implementation
/// correctly satisfies the interface contract for per-shard position persistence, retrieval,
/// deletion, bulk retrieval, and case-insensitive composite key behavior.
/// Uses <see cref="InMemoryShardedCdcPositionStore"/> as the concrete implementation under test.
/// </summary>
[Trait("Category", "Contract")]
public sealed class IShardedCdcPositionStoreContractTests
{
    #region Test Helpers

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;

        public long Value { get; }

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static InMemoryShardedCdcPositionStore CreateStore() => new();

    #endregion

    #region GetPositionAsync Contract

    /// <summary>
    /// Contract: GetPositionAsync must return <see cref="Option{A}.None"/> when no
    /// position has been saved for the given (shardId, connectorId) composite key.
    /// </summary>
    [Fact]
    public async Task Contract_GetPosition_ReturnsNone_ForNonExistentCompositeKey()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetPositionAsync("non-existent-shard", "non-existent-connector");

        // Assert
        result.IsRight.ShouldBeTrue("GetPositionAsync must return Right for non-existent composite key");

        result.IfRight(option =>
            option.IsNone.ShouldBeTrue("GetPositionAsync must return None when no position exists for the composite key"));
    }

    #endregion

    #region SavePositionAsync / GetPositionAsync Round-Trip Contract

    /// <summary>
    /// Contract: A position saved via SavePositionAsync must be retrievable
    /// via GetPositionAsync using the same (shardId, connectorId) composite key.
    /// </summary>
    [Fact]
    public async Task Contract_SaveThenGet_ReturnsSavedPosition()
    {
        // Arrange
        var store = CreateStore();
        var shardId = "shard-1";
        var connectorId = "connector-1";
        var position = new TestCdcPosition(42);

        // Act
        var saveResult = await store.SavePositionAsync(shardId, connectorId, position);
        var getResult = await store.GetPositionAsync(shardId, connectorId);

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
    /// for the same (shardId, connectorId) composite key.
    /// </summary>
    [Fact]
    public async Task Contract_Save_OverwritesPreviousPosition()
    {
        // Arrange
        var store = CreateStore();

        // Act
        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(10));
        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(20));
        var getResult = await store.GetPositionAsync("shard-1", "connector-1");

        // Assert
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(option =>
        {
            option.IsSome.ShouldBeTrue("Position must exist after overwrite");
            option.IfSome(retrieved =>
                ((TestCdcPosition)retrieved).Value.ShouldBe(20L,
                    "Position must reflect the latest saved value, not the original"));
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
        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(100));

        // Act
        var deleteResult = await store.DeletePositionAsync("shard-1", "connector-1");
        var getResult = await store.GetPositionAsync("shard-1", "connector-1");

        // Assert
        deleteResult.IsRight.ShouldBeTrue("DeletePositionAsync must return Right on success");

        getResult.IsRight.ShouldBeTrue("GetPositionAsync must return Right after deletion");
        getResult.IfRight(option =>
            option.IsNone.ShouldBeTrue("GetPositionAsync must return None after position has been deleted"));
    }

    /// <summary>
    /// Contract: DeletePositionAsync must succeed (return Right) when the composite key
    /// does not exist. It must not treat a missing position as an error.
    /// </summary>
    [Fact]
    public async Task Contract_Delete_NonExistentKey_ReturnsSuccess()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.DeletePositionAsync("non-existent-shard", "non-existent-connector");

        // Assert
        result.IsRight.ShouldBeTrue(
            "DeletePositionAsync must return Right (success) for non-existent composite key");
    }

    #endregion

    #region GetAllPositionsAsync Contract

    /// <summary>
    /// Contract: GetAllPositionsAsync must return all saved positions for a given connector,
    /// keyed by shard identifier.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllPositions_ReturnsAllShardsForConnector()
    {
        // Arrange
        var store = CreateStore();
        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-2", "connector-1", new TestCdcPosition(200));
        await store.SavePositionAsync("shard-3", "connector-1", new TestCdcPosition(300));

        // Act
        var result = await store.GetAllPositionsAsync("connector-1");

        // Assert
        result.IsRight.ShouldBeTrue("GetAllPositionsAsync must return Right on success");
        result.IfRight(positions =>
        {
            positions.Count.ShouldBe(3, "Must return positions for all 3 shards");
        });
    }

    /// <summary>
    /// Contract: GetAllPositionsAsync must return an empty dictionary when no positions
    /// have been saved for the connector.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllPositions_ReturnsEmpty_ForNonExistentConnector()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetAllPositionsAsync("non-existent-connector");

        // Assert
        result.IsRight.ShouldBeTrue("GetAllPositionsAsync must return Right for non-existent connector");
        result.IfRight(positions =>
            positions.Count.ShouldBe(0, "Must return empty dictionary when no positions exist"));
    }

    /// <summary>
    /// Contract: GetAllPositionsAsync must only return positions for the specified connector,
    /// excluding positions from other connectors.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllPositions_ExcludesOtherConnectors()
    {
        // Arrange
        var store = CreateStore();
        await store.SavePositionAsync("shard-1", "connector-a", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-2", "connector-b", new TestCdcPosition(200));
        await store.SavePositionAsync("shard-3", "connector-a", new TestCdcPosition(300));

        // Act
        var result = await store.GetAllPositionsAsync("connector-a");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(positions =>
        {
            positions.Count.ShouldBe(2, "Must only return positions for connector-a (2 shards)");
        });
    }

    #endregion

    #region Case-Insensitive Composite Key Contract

    /// <summary>
    /// Contract: Both shardId and connectorId in the composite key must be treated as
    /// case-insensitive. Saving with one casing and retrieving with another must work.
    /// </summary>
    [Fact]
    public async Task Contract_CompositeKey_IsCaseInsensitive()
    {
        // Arrange
        var store = CreateStore();
        var position = new TestCdcPosition(55);

        // Act
        await store.SavePositionAsync("MyShardId", "MyConnector", position);
        var getResult = await store.GetPositionAsync("myshardid", "myconnector");

        // Assert
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(option =>
        {
            option.IsSome.ShouldBeTrue(
                "Composite key must be case-insensitive for both shardId and connectorId");
            option.IfSome(retrieved =>
                ((TestCdcPosition)retrieved).Value.ShouldBe(55L));
        });
    }

    /// <summary>
    /// Contract: GetAllPositionsAsync must be case-insensitive for the connectorId parameter.
    /// </summary>
    [Fact]
    public async Task Contract_GetAllPositions_ConnectorId_IsCaseInsensitive()
    {
        // Arrange
        var store = CreateStore();
        await store.SavePositionAsync("shard-1", "MyConnector", new TestCdcPosition(42));

        // Act
        var result = await store.GetAllPositionsAsync("myconnector");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(positions =>
            positions.Count.ShouldBe(1,
                "GetAllPositionsAsync must match connectorId case-insensitively"));
    }

    /// <summary>
    /// Contract: DeletePositionAsync must be case-insensitive for both shardId and connectorId.
    /// </summary>
    [Fact]
    public async Task Contract_Delete_CaseInsensitive_RemovesPosition()
    {
        // Arrange
        var store = CreateStore();
        await store.SavePositionAsync("SHARD-1", "CONNECTOR-1", new TestCdcPosition(77));

        // Act
        await store.DeletePositionAsync("shard-1", "connector-1");
        var getResult = await store.GetPositionAsync("Shard-1", "Connector-1");

        // Assert
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(option =>
            option.IsNone.ShouldBeTrue("Deletion with different casing must still remove the position"));
    }

    #endregion

    #region Composite Key Independence Contract

    /// <summary>
    /// Contract: Positions for different shardId values with the same connectorId
    /// must be stored independently.
    /// </summary>
    [Fact]
    public async Task Contract_DifferentShards_SameConnector_AreIndependent()
    {
        // Arrange
        var store = CreateStore();

        // Act
        await store.SavePositionAsync("shard-a", "connector-1", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-b", "connector-1", new TestCdcPosition(200));

        // Assert
        var resultA = await store.GetPositionAsync("shard-a", "connector-1");
        var resultB = await store.GetPositionAsync("shard-b", "connector-1");

        resultA.IfRight(opt => opt.IfSome(pos =>
            ((TestCdcPosition)pos).Value.ShouldBe(100L,
                "Shard A position must be independent from Shard B")));

        resultB.IfRight(opt => opt.IfSome(pos =>
            ((TestCdcPosition)pos).Value.ShouldBe(200L,
                "Shard B position must be independent from Shard A")));
    }

    /// <summary>
    /// Contract: Positions for the same shardId but different connectorId values
    /// must be stored independently.
    /// </summary>
    [Fact]
    public async Task Contract_SameShard_DifferentConnectors_AreIndependent()
    {
        // Arrange
        var store = CreateStore();

        // Act
        await store.SavePositionAsync("shard-1", "connector-a", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-1", "connector-b", new TestCdcPosition(200));

        // Assert
        var resultA = await store.GetPositionAsync("shard-1", "connector-a");
        var resultB = await store.GetPositionAsync("shard-1", "connector-b");

        resultA.IfRight(opt => opt.IfSome(pos =>
            ((TestCdcPosition)pos).Value.ShouldBe(100L)));

        resultB.IfRight(opt => opt.IfSome(pos =>
            ((TestCdcPosition)pos).Value.ShouldBe(200L)));
    }

    /// <summary>
    /// Contract: Deleting one shard's position must not affect positions of other shards
    /// for the same connector.
    /// </summary>
    [Fact]
    public async Task Contract_DeleteOneShard_DoesNotAffectOtherShards()
    {
        // Arrange
        var store = CreateStore();
        await store.SavePositionAsync("shard-x", "connector-1", new TestCdcPosition(10));
        await store.SavePositionAsync("shard-y", "connector-1", new TestCdcPosition(20));

        // Act
        await store.DeletePositionAsync("shard-x", "connector-1");

        // Assert
        var resultX = await store.GetPositionAsync("shard-x", "connector-1");
        var resultY = await store.GetPositionAsync("shard-y", "connector-1");

        resultX.IfRight(opt => opt.IsNone.ShouldBeTrue("Deleted shard must return None"));
        resultY.IfRight(opt =>
        {
            opt.IsSome.ShouldBeTrue("Other shard must still have its position");
            opt.IfSome(pos => ((TestCdcPosition)pos).Value.ShouldBe(20L));
        });
    }

    #endregion
}
