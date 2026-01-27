using Encina.Messaging.ReadWriteSeparation;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;

/// <summary>
/// Integration tests for <see cref="ReadWriteMongoCollectionFactory"/> verifying read/write
/// separation routing behavior against a real MongoDB replica set.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the collection factory correctly applies read preferences
/// based on the routing context. Note that with a single-node replica set, all operations
/// go to the same node, but we can verify the read preference is correctly configured.
/// </para>
/// </remarks>
[Collection(MongoDbReplicaSetCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[Trait("Feature", "ReadWriteSeparation")]
public sealed class ReadWriteSeparationMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbReplicaSetFixture _fixture;
    private readonly string _testCollectionName;

    public ReadWriteSeparationMongoDBIntegrationTests(MongoDbReplicaSetFixture fixture)
    {
        _fixture = fixture;
        _testCollectionName = $"test_rw_{Guid.NewGuid():N}";
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_fixture.IsAvailable && _fixture.Database is not null)
        {
            await _fixture.Database.DropCollectionAsync(_testCollectionName);
        }

        // Always clear routing context after tests
        DatabaseRoutingContext.Clear();
    }

    #region Write Collection Tests

    [SkippableFact]
    public async Task GetWriteCollectionAsync_ShouldReturnCollectionWithPrimaryReadPreference()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();

        // Act
        var collection = await factory.GetWriteCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.ShouldNotBeNull();
        collection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    [SkippableFact]
    public async Task GetWriteCollectionAsync_ShouldAllowInsertOperations()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();
        var collection = await factory.GetWriteCollectionAsync<BsonDocument>(_testCollectionName);
        var document = new BsonDocument("key", "write_test_value");

        // Act
        await collection.InsertOneAsync(document);

        // Assert
        var result = await collection.Find(new BsonDocument("key", "write_test_value")).FirstOrDefaultAsync();
        result.ShouldNotBeNull();
        result["key"].AsString.ShouldBe("write_test_value");
    }

    [SkippableFact]
    public async Task GetWriteCollectionAsync_WithEmptyCollectionName_ShouldThrow()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetWriteCollectionAsync<BsonDocument>(string.Empty));
    }

    #endregion

    #region Read Collection Tests

    [SkippableFact]
    public async Task GetReadCollectionAsync_WithDefaultOptions_ShouldUseSecondaryPreferred()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.ShouldNotBeNull();
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.SecondaryPreferred);
    }

    [SkippableFact]
    public async Task GetReadCollectionAsync_WithPrimaryReadPreference_ShouldUsePrimary()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Primary;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    [SkippableFact]
    public async Task GetReadCollectionAsync_WithSecondaryReadPreference_ShouldUseSecondary()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Secondary;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.Secondary);
    }

    [SkippableFact]
    public async Task GetReadCollectionAsync_WithNearestReadPreference_ShouldUseNearest()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Nearest;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.Nearest);
    }

    [SkippableFact]
    public async Task GetReadCollectionAsync_WithMajorityReadConcern_ShouldApplyReadConcern()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Majority;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Majority);
    }

    [SkippableFact]
    public async Task GetReadCollectionAsync_WithLocalReadConcern_ShouldApplyReadConcern()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Local);
    }

    [SkippableFact]
    public async Task GetReadCollectionAsync_ShouldAllowQueryOperations()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();
        var writeCollection = await factory.GetWriteCollectionAsync<BsonDocument>(_testCollectionName);
        await writeCollection.InsertOneAsync(new BsonDocument("key", "read_test_value"));

        // Act
        var readCollection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);
        var result = await readCollection.Find(new BsonDocument("key", "read_test_value")).FirstOrDefaultAsync();

        // Assert
        result.ShouldNotBeNull();
        result["key"].AsString.ShouldBe("read_test_value");
    }

    [SkippableFact]
    public async Task GetReadCollectionAsync_WithMaxStaleness_ShouldApplyToReadPreference()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange - MongoDB requires minimum 90 seconds for maxStaleness
        var maxStaleness = TimeSpan.FromSeconds(120);
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
            options.ReadWriteSeparationOptions.MaxStaleness = maxStaleness;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.MaxStaleness.ShouldBe(maxStaleness);
    }

    #endregion

    #region Context-Based Routing Tests (GetCollectionAsync)

    [SkippableFact]
    public async Task GetCollectionAsync_WithReadIntent_ShouldUseConfiguredReadPreference()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.SecondaryPreferred);
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithWriteIntent_ShouldUsePrimary()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithForceWriteIntent_ShouldUsePrimary()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithNoContext_ShouldDefaultToPrimary()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();
        DatabaseRoutingContext.Clear(); // Ensure no context

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert - Should default to Primary for safety
        collection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithReadIntent_AndSecondaryPreference_ShouldApplyReadConcern()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Majority;
        });
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        var collection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Majority);
    }

    [SkippableFact]
    public async Task GetCollectionAsync_SwitchingIntents_ShouldReturnDifferentReadPreferences()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();

        // Act & Assert - Read intent
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
        var readCollection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);
        readCollection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.SecondaryPreferred);

        // Act & Assert - Write intent
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;
        var writeCollection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);
        writeCollection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);

        // Act & Assert - ForceWrite intent
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.ForceWrite;
        var forceWriteCollection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);
        forceWriteCollection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    #endregion

    #region Database Name Tests

    [SkippableFact]
    public async Task GetDatabaseNameAsync_ShouldReturnConfiguredDatabaseName()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();

        // Act
        var databaseName = await factory.GetDatabaseNameAsync();

        // Assert
        databaseName.ShouldBe(MongoDbReplicaSetFixture.DatabaseName);
    }

    [SkippableFact]
    public async Task GetDatabaseNameAsync_WithEmptyDatabaseName_ShouldThrow()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.DatabaseName = string.Empty;
        });

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await factory.GetDatabaseNameAsync());
    }

    #endregion

    #region Read Preference Conversion Tests

    [SkippableFact]
    public async Task ReadPreferenceConversion_AllValues_ShouldMapCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Test all MongoReadPreference enum values
        var testCases = new[]
        {
            (MongoReadPreference.Primary, ReadPreferenceMode.Primary),
            (MongoReadPreference.PrimaryPreferred, ReadPreferenceMode.PrimaryPreferred),
            (MongoReadPreference.Secondary, ReadPreferenceMode.Secondary),
            (MongoReadPreference.SecondaryPreferred, ReadPreferenceMode.SecondaryPreferred),
            (MongoReadPreference.Nearest, ReadPreferenceMode.Nearest)
        };

        foreach (var (encinaPref, expectedMode) in testCases)
        {
            // Arrange
            var factory = CreateFactory(options =>
            {
                options.ReadWriteSeparationOptions.ReadPreference = encinaPref;
            });

            // Act
            var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

            // Assert
            collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(expectedMode,
                $"MongoReadPreference.{encinaPref} should map to ReadPreferenceMode.{expectedMode}");
        }
    }

    #endregion

    #region Read Concern Conversion Tests

    [SkippableFact]
    public async Task ReadConcernConversion_AllValues_ShouldMapCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Test all MongoReadConcern enum values
        var testCases = new[]
        {
            (MongoReadConcern.Default, ReadConcern.Default),
            (MongoReadConcern.Local, ReadConcern.Local),
            (MongoReadConcern.Majority, ReadConcern.Majority),
            (MongoReadConcern.Linearizable, ReadConcern.Linearizable),
            (MongoReadConcern.Available, ReadConcern.Available),
            (MongoReadConcern.Snapshot, ReadConcern.Snapshot)
        };

        foreach (var (encinaConcern, expectedConcern) in testCases)
        {
            // Arrange
            var factory = CreateFactory(options =>
            {
                options.ReadWriteSeparationOptions.ReadConcern = encinaConcern;
            });

            // Act
            var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

            // Assert
            collection.Settings.ReadConcern.ShouldBe(expectedConcern,
                $"MongoReadConcern.{encinaConcern} should map to ReadConcern.{expectedConcern}");
        }
    }

    #endregion

    #region Concurrent Access Tests

    [SkippableFact]
    public async Task GetCollectionAsync_ConcurrentAccessWithDifferentIntents_ShouldIsolateCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory();
        var readTasks = new List<Task<ReadPreferenceMode>>();
        var writeTasks = new List<Task<ReadPreferenceMode>>();

        // Act - Create multiple concurrent reads with Read intent
        for (int i = 0; i < 5; i++)
        {
            readTasks.Add(Task.Run(async () =>
            {
                DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
                var collection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);
                return collection.Settings.ReadPreference.ReadPreferenceMode;
            }));
        }

        // Create multiple concurrent reads with Write intent
        for (int i = 0; i < 5; i++)
        {
            writeTasks.Add(Task.Run(async () =>
            {
                DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;
                var collection = await factory.GetCollectionAsync<BsonDocument>(_testCollectionName);
                return collection.Settings.ReadPreference.ReadPreferenceMode;
            }));
        }

        var readResults = await Task.WhenAll(readTasks);
        var writeResults = await Task.WhenAll(writeTasks);

        // Assert - All read intents should have SecondaryPreferred
        readResults.ShouldAllBe(mode => mode == ReadPreferenceMode.SecondaryPreferred);

        // Assert - All write intents should have Primary
        writeResults.ShouldAllBe(mode => mode == ReadPreferenceMode.Primary);
    }

    #endregion

    #region Helper Methods

    private ReadWriteMongoCollectionFactory CreateFactory(Action<EncinaMongoDbOptions>? configure = null)
    {
        var options = new EncinaMongoDbOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = MongoDbReplicaSetFixture.DatabaseName,
            UseReadWriteSeparation = true
        };

        configure?.Invoke(options);

        return new ReadWriteMongoCollectionFactory(
            _fixture.Client!,
            Options.Create(options));
    }

    #endregion
}
