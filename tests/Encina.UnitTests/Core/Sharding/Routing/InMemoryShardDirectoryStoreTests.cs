using Encina.Sharding.Routing;
using Shouldly;

namespace Encina.UnitTests.Core.Sharding.Routing;

/// <summary>
/// Unit tests for <see cref="InMemoryShardDirectoryStore"/>.
/// </summary>
public class InMemoryShardDirectoryStoreTests
{
    #region Constructor

    [Fact]
    public void Constructor_Default_ShouldCreateEmptyStore()
    {
        // Arrange & Act
        var store = new InMemoryShardDirectoryStore();

        // Assert
        store.GetAllMappings().ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithInitialMappings_ShouldPopulateStore()
    {
        // Arrange
        var initialMappings = new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a",
            ["tenant-2"] = "shard-b",
            ["tenant-3"] = "shard-c"
        };

        // Act
        var store = new InMemoryShardDirectoryStore(initialMappings);

        // Assert
        var allMappings = store.GetAllMappings();
        allMappings.Count.ShouldBe(3);
        allMappings["tenant-1"].ShouldBe("shard-a");
        allMappings["tenant-2"].ShouldBe("shard-b");
        allMappings["tenant-3"].ShouldBe("shard-c");
    }

    [Fact]
    public void Constructor_NullInitialMappings_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<KeyValuePair<string, string>>? nullMappings = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new InMemoryShardDirectoryStore(nullMappings!));
    }

    #endregion

    #region GetMapping

    [Fact]
    public void GetMapping_ExistingKey_ShouldReturnShardId()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a"
        });

        // Act
        var result = store.GetMapping("tenant-1");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("shard-a");
    }

    [Fact]
    public void GetMapping_NonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a"
        });

        // Act
        var result = store.GetMapping("tenant-unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("KEY1")]
    [InlineData("Key1")]
    [InlineData("key1")]
    [InlineData("kEy1")]
    public void GetMapping_CaseInsensitiveKey_ShouldReturnSameMapping(string lookupKey)
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["key1"] = "shard-a"
        });

        // Act
        var result = store.GetMapping(lookupKey);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("shard-a");
    }

    [Fact]
    public void GetMapping_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => store.GetMapping(null!));
    }

    #endregion

    #region AddMapping

    [Fact]
    public void AddMapping_NewKey_ShouldBeRetrievable()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();

        // Act
        store.AddMapping("tenant-1", "shard-a");

        // Assert
        store.GetMapping("tenant-1").ShouldBe("shard-a");
        store.GetAllMappings().Count.ShouldBe(1);
    }

    [Fact]
    public void AddMapping_ExistingKey_ShouldUpsertValue()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a"
        });

        // Act
        store.AddMapping("tenant-1", "shard-b");

        // Assert
        store.GetMapping("tenant-1").ShouldBe("shard-b");
        store.GetAllMappings().Count.ShouldBe(1);
    }

    [Fact]
    public void AddMapping_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => store.AddMapping(null!, "shard-a"));
    }

    [Fact]
    public void AddMapping_NullShardId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => store.AddMapping("tenant-1", null!));
    }

    #endregion

    #region RemoveMapping

    [Fact]
    public void RemoveMapping_ExistingKey_ShouldReturnTrueAndRemoveMapping()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a",
            ["tenant-2"] = "shard-b"
        });

        // Act
        var result = store.RemoveMapping("tenant-1");

        // Assert
        result.ShouldBeTrue();
        store.GetMapping("tenant-1").ShouldBeNull();
        store.GetAllMappings().Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveMapping_NonExistingKey_ShouldReturnFalse()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a"
        });

        // Act
        var result = store.RemoveMapping("tenant-unknown");

        // Assert
        result.ShouldBeFalse();
        store.GetAllMappings().Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveMapping_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => store.RemoveMapping(null!));
    }

    #endregion

    #region GetAllMappings

    [Fact]
    public void GetAllMappings_ShouldReturnAllCurrentMappings()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a",
            ["tenant-2"] = "shard-b",
            ["tenant-3"] = "shard-c"
        });

        // Act
        var allMappings = store.GetAllMappings();

        // Assert
        allMappings.Count.ShouldBe(3);
        allMappings["tenant-1"].ShouldBe("shard-a");
        allMappings["tenant-2"].ShouldBe("shard-b");
        allMappings["tenant-3"].ShouldBe("shard-c");
    }

    [Fact]
    public void GetAllMappings_ShouldReturnDefensiveCopy()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore(new Dictionary<string, string>
        {
            ["tenant-1"] = "shard-a"
        });

        // Act
        var firstSnapshot = store.GetAllMappings();
        store.AddMapping("tenant-2", "shard-b");
        var secondSnapshot = store.GetAllMappings();

        // Assert
        firstSnapshot.Count.ShouldBe(1);
        secondSnapshot.Count.ShouldBe(2);
    }

    [Fact]
    public void GetAllMappings_EmptyStore_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();

        // Act
        var allMappings = store.GetAllMappings();

        // Assert
        allMappings.ShouldBeEmpty();
    }

    #endregion

    #region Thread Safety

    [Fact]
    public void AddMapping_ConcurrentOperations_ShouldNotThrow()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();
        const int operationCount = 1000;

        // Act & Assert
        Should.NotThrow(() =>
        {
            Parallel.For(0, operationCount, i =>
            {
                store.AddMapping($"tenant-{i}", $"shard-{i % 10}");
            });
        });

        store.GetAllMappings().Count.ShouldBe(operationCount);
    }

    [Fact]
    public void MixedOperations_ConcurrentAccess_ShouldNotThrow()
    {
        // Arrange
        var store = new InMemoryShardDirectoryStore();
        const int operationCount = 500;

        // Pre-populate some entries for remove/read operations
        for (var i = 0; i < operationCount; i++)
        {
            store.AddMapping($"pre-{i}", $"shard-{i % 5}");
        }

        // Act & Assert
        Should.NotThrow(() =>
        {
            Parallel.Invoke(
                () =>
                {
                    for (var i = 0; i < operationCount; i++)
                    {
                        store.AddMapping($"new-{i}", $"shard-{i % 10}");
                    }
                },
                () =>
                {
                    for (var i = 0; i < operationCount; i++)
                    {
                        store.GetMapping($"pre-{i}");
                    }
                },
                () =>
                {
                    for (var i = 0; i < operationCount; i++)
                    {
                        store.RemoveMapping($"pre-{i}");
                    }
                },
                () =>
                {
                    for (var i = 0; i < operationCount; i++)
                    {
                        store.GetAllMappings();
                    }
                }
            );
        });
    }

    #endregion
}
