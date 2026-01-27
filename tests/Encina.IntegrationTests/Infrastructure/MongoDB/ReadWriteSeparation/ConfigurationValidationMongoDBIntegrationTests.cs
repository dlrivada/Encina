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
/// Integration tests for MongoDB read/write separation configuration validation.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that <see cref="MongoReadWriteSeparationOptions"/> configuration
/// is correctly applied and validated against a real MongoDB replica set.
/// </para>
/// </remarks>
[Collection(MongoDbReplicaSetCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[Trait("Feature", "ReadWriteSeparation")]
[Trait("Feature", "Configuration")]
public sealed class ConfigurationValidationMongoDBIntegrationTests
{
    private readonly MongoDbReplicaSetFixture _fixture;
    private readonly string _testCollectionName;

    public ConfigurationValidationMongoDBIntegrationTests(MongoDbReplicaSetFixture fixture)
    {
        _fixture = fixture;
        _testCollectionName = $"test_config_{Guid.NewGuid():N}";
    }

    #region Default Configuration Tests

    [SkippableFact]
    public void MongoReadWriteSeparationOptions_DefaultValues_ShouldBeCorrect()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange & Act
        var options = new MongoReadWriteSeparationOptions();

        // Assert
        options.ReadPreference.ShouldBe(MongoReadPreference.SecondaryPreferred);
        options.ReadConcern.ShouldBe(MongoReadConcern.Majority);
        options.ValidateOnStartup.ShouldBeFalse();
        options.FallbackToPrimaryOnNoSecondaries.ShouldBeTrue();
        options.MaxStaleness.ShouldBeNull();
    }

    [SkippableFact]
    public void EncinaMongoDbOptions_DefaultValues_ShouldBeCorrect()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange & Act
        var options = new EncinaMongoDbOptions();

        // Assert
        options.UseReadWriteSeparation.ShouldBeFalse();
        options.ReadWriteSeparationOptions.ShouldNotBeNull();
        options.ReadWriteSeparationOptions.ReadPreference.ShouldBe(MongoReadPreference.SecondaryPreferred);
    }

    #endregion

    #region Read Preference Configuration Tests

    [SkippableFact]
    public async Task Configuration_WithPrimaryReadPreference_ShouldApply()
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
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.Primary);
    }

    [SkippableFact]
    public async Task Configuration_WithPrimaryPreferredReadPreference_ShouldApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.PrimaryPreferred;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.PrimaryPreferred);
    }

    [SkippableFact]
    public async Task Configuration_WithSecondaryReadPreference_ShouldApply()
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
    public async Task Configuration_WithSecondaryPreferredReadPreference_ShouldApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.SecondaryPreferred);
    }

    [SkippableFact]
    public async Task Configuration_WithNearestReadPreference_ShouldApply()
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

    #endregion

    #region Read Concern Configuration Tests

    [SkippableFact]
    public async Task Configuration_WithDefaultReadConcern_ShouldApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Default;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Default);
    }

    [SkippableFact]
    public async Task Configuration_WithLocalReadConcern_ShouldApply()
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
    public async Task Configuration_WithMajorityReadConcern_ShouldApply()
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
    public async Task Configuration_WithLinearizableReadConcern_ShouldApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Linearizable;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Linearizable);
    }

    [SkippableFact]
    public async Task Configuration_WithAvailableReadConcern_ShouldApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Available;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Available);
    }

    [SkippableFact]
    public async Task Configuration_WithSnapshotReadConcern_ShouldApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Snapshot;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Snapshot);
    }

    #endregion

    #region MaxStaleness Configuration Tests

    [SkippableFact]
    public async Task Configuration_WithMaxStaleness_ShouldApplyToNonPrimaryPreferences()
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

    [SkippableFact]
    public async Task Configuration_WithMaxStaleness_AndPrimaryPreference_ShouldNotApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange - MaxStaleness is not valid with Primary read preference
        var maxStaleness = TimeSpan.FromSeconds(120);
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Primary;
            options.ReadWriteSeparationOptions.MaxStaleness = maxStaleness;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert - MaxStaleness should not be applied with Primary
        collection.Settings.ReadPreference.MaxStaleness.ShouldBeNull();
    }

    [SkippableFact]
    public async Task Configuration_WithNullMaxStaleness_ShouldNotSetMaxStaleness()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
            options.ReadWriteSeparationOptions.MaxStaleness = null;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.MaxStaleness.ShouldBeNull();
    }

    [SkippableFact]
    public async Task Configuration_WithMaxStalenessOnNearest_ShouldApply()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var maxStaleness = TimeSpan.FromSeconds(180);
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Nearest;
            options.ReadWriteSeparationOptions.MaxStaleness = maxStaleness;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.MaxStaleness.ShouldBe(maxStaleness);
    }

    #endregion

    #region Combined Configuration Tests

    [SkippableFact]
    public async Task Configuration_CombinedReadPreferenceAndConcern_ShouldApplyBoth()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Nearest;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.Nearest);
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Local);
    }

    [SkippableFact]
    public async Task Configuration_AllOptionsSet_ShouldApplyAll()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var maxStaleness = TimeSpan.FromSeconds(150);
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Majority;
            options.ReadWriteSeparationOptions.MaxStaleness = maxStaleness;
            options.ReadWriteSeparationOptions.ValidateOnStartup = true;
            options.ReadWriteSeparationOptions.FallbackToPrimaryOnNoSecondaries = true;
        });

        // Act
        var collection = await factory.GetReadCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert
        collection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.SecondaryPreferred);
        collection.Settings.ReadPreference.MaxStaleness.ShouldBe(maxStaleness);
        collection.Settings.ReadConcern.ShouldBe(ReadConcern.Majority);
    }

    #endregion

    #region Write Collection Configuration Tests

    [SkippableFact]
    public async Task Configuration_WriteCollection_ShouldAlwaysUsePrimary_RegardlessOfOptions()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange - Configure with non-primary read preference
        var factory = CreateFactory(options =>
        {
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Secondary;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
        });

        // Act
        var writeCollection = await factory.GetWriteCollectionAsync<BsonDocument>(_testCollectionName);

        // Assert - Write collection should always use Primary
        writeCollection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    #endregion

    #region Enum Coverage Tests

    [SkippableFact]
    public void MongoReadPreference_AllEnumValues_ShouldBeValid()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Assert - All enum values are defined
        var values = Enum.GetValues<MongoReadPreference>();
        values.ShouldContain(MongoReadPreference.Primary);
        values.ShouldContain(MongoReadPreference.PrimaryPreferred);
        values.ShouldContain(MongoReadPreference.Secondary);
        values.ShouldContain(MongoReadPreference.SecondaryPreferred);
        values.ShouldContain(MongoReadPreference.Nearest);
        values.Length.ShouldBe(5);
    }

    [SkippableFact]
    public void MongoReadConcern_AllEnumValues_ShouldBeValid()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Assert - All enum values are defined
        var values = Enum.GetValues<MongoReadConcern>();
        values.ShouldContain(MongoReadConcern.Default);
        values.ShouldContain(MongoReadConcern.Local);
        values.ShouldContain(MongoReadConcern.Majority);
        values.ShouldContain(MongoReadConcern.Linearizable);
        values.ShouldContain(MongoReadConcern.Available);
        values.ShouldContain(MongoReadConcern.Snapshot);
        values.Length.ShouldBe(6);
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
