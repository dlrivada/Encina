using Encina.Sharding;
using Encina.Sharding.Routing;

namespace Encina.UnitTests.Core.Sharding.Routing;

/// <summary>
/// Unit tests for <see cref="DirectoryShardRouter"/>.
/// </summary>
public sealed class DirectoryShardRouterTests
{
    private static readonly ShardInfo Shard1 = new("shard-1", "Server=shard1;Database=db1");
    private static readonly ShardInfo Shard2 = new("shard-2", "Server=shard2;Database=db2");
    private static readonly ShardInfo Shard3 = new("shard-3", "Server=shard3;Database=db3");

    private static ShardTopology CreateTopology(params ShardInfo[] shards)
        => new(shards);

    private static ShardTopology CreateDefaultTopology()
        => CreateTopology(Shard1, Shard2, Shard3);

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidInputs_CreatesInstance()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();

        // Act
        var router = new DirectoryShardRouter(topology, store);

        // Assert
        router.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IShardDirectoryStore>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DirectoryShardRouter(null!, store))
            .ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DirectoryShardRouter(topology, null!))
            .ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_WithDefaultShardId_SetsProperty()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();

        // Act
        var router = new DirectoryShardRouter(topology, store, "shard-1");

        // Assert
        router.DefaultShardId.ShouldBe("shard-1");
    }

    [Fact]
    public void Constructor_WithoutDefaultShardId_PropertyIsNull()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();

        // Act
        var router = new DirectoryShardRouter(topology, store);

        // Assert
        router.DefaultShardId.ShouldBeNull();
    }

    #endregion

    #region GetShardId Tests

    [Fact]
    public void GetShardId_NullShardKey_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardId(null!))
            .ParamName.ShouldBe("shardKey");
    }

    [Fact]
    public void GetShardId_KeyExistsInDirectory_ReturnsRightWithMappedShardId()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("tenant-abc").Returns("shard-2");
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetShardId("tenant-abc");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-2"));
    }

    [Fact]
    public void GetShardId_KeyExistsInDirectory_CallsStoreGetMapping()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("tenant-abc").Returns("shard-1");
        var router = new DirectoryShardRouter(topology, store);

        // Act
        router.GetShardId("tenant-abc");

        // Assert
        store.Received(1).GetMapping("tenant-abc");
    }

    [Fact]
    public void GetShardId_KeyNotInDirectoryButDefaultSet_ReturnsRightWithDefaultShardId()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("unknown-key").Returns((string?)null);
        var router = new DirectoryShardRouter(topology, store, "shard-3");

        // Act
        var result = router.GetShardId("unknown-key");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-3"));
    }

    [Fact]
    public void GetShardId_KeyNotInDirectoryNoDefault_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("missing-key").Returns((string?)null);
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetShardId("missing-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void GetShardId_KeyNotInDirectoryNoDefault_ErrorContainsShardNotFoundCode()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("missing-key").Returns((string?)null);
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetShardId("missing-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(ShardingErrorCodes.ShardNotFound));
        });
    }

    [Fact]
    public void GetShardId_KeyNotInDirectoryNoDefault_ErrorMessageContainsKey()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("my-tenant").Returns((string?)null);
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetShardId("my-tenant");

        // Assert
        result.IfLeft(error => error.Message.ShouldContain("my-tenant"));
    }

    #endregion

    #region AddMapping Tests

    [Fact]
    public void AddMapping_ValidParameters_DelegatesToStore()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act
        router.AddMapping("tenant-xyz", "shard-2");

        // Assert
        store.Received(1).AddMapping("tenant-xyz", "shard-2");
    }

    [Fact]
    public void AddMapping_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.AddMapping(null!, "shard-1"))
            .ParamName.ShouldBe("key");
    }

    [Fact]
    public void AddMapping_NullShardId_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.AddMapping("tenant-xyz", null!))
            .ParamName.ShouldBe("shardId");
    }

    #endregion

    #region RemoveMapping Tests

    [Fact]
    public void RemoveMapping_ExistingKey_DelegatesToStoreAndReturnsTrue()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.RemoveMapping("tenant-abc").Returns(true);
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.RemoveMapping("tenant-abc");

        // Assert
        result.ShouldBeTrue();
        store.Received(1).RemoveMapping("tenant-abc");
    }

    [Fact]
    public void RemoveMapping_NonExistingKey_DelegatesToStoreAndReturnsFalse()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.RemoveMapping("unknown").Returns(false);
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.RemoveMapping("unknown");

        // Assert
        result.ShouldBeFalse();
        store.Received(1).RemoveMapping("unknown");
    }

    [Fact]
    public void RemoveMapping_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.RemoveMapping(null!))
            .ParamName.ShouldBe("key");
    }

    #endregion

    #region GetMapping Tests

    [Fact]
    public void GetMapping_ExistingKey_DelegatesToStoreAndReturnsMappedShardId()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("tenant-abc").Returns("shard-1");
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetMapping("tenant-abc");

        // Assert
        result.ShouldBe("shard-1");
        store.Received(1).GetMapping("tenant-abc");
    }

    [Fact]
    public void GetMapping_NonExistingKey_ReturnsNull()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("unknown").Returns((string?)null);
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetMapping("unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetMapping_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetMapping(null!))
            .ParamName.ShouldBe("key");
    }

    #endregion

    #region GetAllShardIds Tests

    [Fact]
    public void GetAllShardIds_ReturnsAllShardIdsFromTopology()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetAllShardIds();

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain("shard-1");
        result.ShouldContain("shard-2");
        result.ShouldContain("shard-3");
    }

    [Fact]
    public void GetAllShardIds_SingleShard_ReturnsSingleShardId()
    {
        // Arrange
        var topology = CreateTopology(Shard1);
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetAllShardIds();

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain("shard-1");
    }

    #endregion

    #region GetShardConnectionString Tests

    [Fact]
    public void GetShardConnectionString_ValidShardId_ReturnsRightWithConnectionString()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetShardConnectionString("shard-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(cs => cs.ShouldBe("Server=shard1;Database=db1"));
    }

    [Fact]
    public void GetShardConnectionString_InvalidShardId_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetShardConnectionString("non-existent-shard");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void GetShardConnectionString_InvalidShardId_ErrorContainsShardNotFoundCode()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act
        var result = router.GetShardConnectionString("non-existent-shard");

        // Assert
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(ShardingErrorCodes.ShardNotFound));
        });
    }

    [Fact]
    public void GetShardConnectionString_NullShardId_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardConnectionString(null!))
            .ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void GetShardConnectionString_EachShardReturnsCorrectConnectionString()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act & Assert
        router.GetShardConnectionString("shard-1")
            .IfRight(cs => cs.ShouldBe("Server=shard1;Database=db1"));

        router.GetShardConnectionString("shard-2")
            .IfRight(cs => cs.ShouldBe("Server=shard2;Database=db2"));

        router.GetShardConnectionString("shard-3")
            .IfRight(cs => cs.ShouldBe("Server=shard3;Database=db3"));
    }

    #endregion

    #region DefaultShardId Property Tests

    [Fact]
    public void DefaultShardId_SetViaConstructor_IsReadable()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        const string expectedDefault = "shard-2";

        // Act
        var router = new DirectoryShardRouter(topology, store, expectedDefault);

        // Assert
        router.DefaultShardId.ShouldBe(expectedDefault);
    }

    [Fact]
    public void DefaultShardId_NotSetViaConstructor_IsNull()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();

        // Act
        var router = new DirectoryShardRouter(topology, store);

        // Assert
        router.DefaultShardId.ShouldBeNull();
    }

    #endregion

    #region IShardRouter Interface Compliance Tests

    [Fact]
    public void DirectoryShardRouter_ImplementsIShardRouter()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();

        // Act
        var router = new DirectoryShardRouter(topology, store);

        // Assert
        router.ShouldBeAssignableTo<IShardRouter>();
    }

    #endregion

    #region Store Interaction Verification Tests

    [Fact]
    public void GetShardId_WithExistingMapping_DoesNotCallStoreMoreThanOnce()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        store.GetMapping("tenant-a").Returns("shard-1");
        var router = new DirectoryShardRouter(topology, store);

        // Act
        router.GetShardId("tenant-a");

        // Assert
        store.Received(1).GetMapping("tenant-a");
        store.DidNotReceive().AddMapping(Arg.Any<string>(), Arg.Any<string>());
        store.DidNotReceive().RemoveMapping(Arg.Any<string>());
    }

    [Fact]
    public void AddMapping_DoesNotCallOtherStoreMethods()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(topology, store);

        // Act
        router.AddMapping("tenant-new", "shard-3");

        // Assert
        store.Received(1).AddMapping("tenant-new", "shard-3");
        store.DidNotReceive().GetMapping(Arg.Any<string>());
        store.DidNotReceive().RemoveMapping(Arg.Any<string>());
    }

    #endregion
}
