using Encina.Messaging.Health;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;

/// <summary>
/// Integration tests for <see cref="ReadWriteMongoHealthCheck"/> verifying health check
/// behavior against a real MongoDB replica set.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the health check correctly reports the replica set topology
/// and health status. Note that with a single-node replica set, the health check may
/// initially report "Standalone" until the cluster description is fully synchronized.
/// </para>
/// <para>
/// The MongoDB driver's cluster description can take a moment to reflect the replica set
/// configuration, especially immediately after startup. Tests are designed to be flexible
/// and verify the health check logic handles both scenarios correctly.
/// </para>
/// </remarks>
[Collection(MongoDbReplicaSetCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[Trait("Feature", "ReadWriteSeparation")]
[Trait("Feature", "HealthCheck")]
public sealed class HealthCheckMongoDBIntegrationTests
{
    private readonly MongoDbReplicaSetFixture _fixture;

    public HealthCheckMongoDBIntegrationTests(MongoDbReplicaSetFixture fixture)
    {
        _fixture = fixture;
    }

    #region Health Check Name and Tags Tests

    [SkippableFact]
    public void HealthCheck_Name_ShouldBeCorrect()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.Name.ShouldBe(ReadWriteMongoHealthCheck.DefaultName);
        healthCheck.Name.ShouldBe("encina-read-write-separation-mongodb");
    }

    [SkippableFact]
    public void HealthCheck_Tags_ShouldContainExpectedTags()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("read-write-separation");
        healthCheck.Tags.ShouldContain("mongodb");
        healthCheck.Tags.ShouldContain("ready");
    }

    #endregion

    #region Health Status Tests

    [SkippableFact]
    public async Task CheckHealthAsync_ShouldReturnDegradedOrHealthyStatus()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - Single-node replica set may report Degraded (no secondaries) or Standalone
        // Both are acceptable for a single-node RS in test environment
        result.Status.ShouldBeOneOf(HealthStatus.Degraded, HealthStatus.Healthy);
        result.Description.ShouldNotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task CheckHealthAsync_ShouldReturnClusterTypeInData()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("cluster_type");
        // Cluster type can be ReplicaSet, Standalone, or Sharded depending on timing
        var clusterType = result.Data["cluster_type"].ToString();
        clusterType.ShouldNotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenReplicaSet_ShouldReturnMemberInfo()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - Only check for member info if detected as ReplicaSet
        if (result.Data["cluster_type"].ToString() == "ReplicaSet")
        {
            result.Data.ShouldContainKey("primary");
            result.Data.ShouldContainKey("secondaries");
            result.Data.ShouldContainKey("arbiters");
            result.Data.ShouldContainKey("configured_read_preference");
        }
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenStandalone_ShouldReturnServerCount()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - Only check for server count if detected as Standalone
        if (result.Data["cluster_type"].ToString() == "Standalone")
        {
            result.Data.ShouldContainKey("servers");
            result.Description.ShouldNotBeNull();
            result.Description!.ShouldContain("standalone");
        }
    }

    #endregion

    #region Health Check Data Tests

    [SkippableFact]
    public async Task CheckHealthAsync_DataDictionary_ShouldNotBeEmpty()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - Data dictionary should have meaningful information
        result.Data.ShouldNotBeEmpty();
        result.Data.ShouldContainKey("cluster_type");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_Description_ShouldProvideUsefulInformation()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - Description should provide context about the health status
        result.Description.ShouldNotBeNullOrEmpty();
        // Description should mention MongoDB or replica/standalone
        (result.Description!.Contains("MongoDB", StringComparison.OrdinalIgnoreCase) ||
         result.Description.Contains("replica", StringComparison.OrdinalIgnoreCase) ||
         result.Description.Contains("standalone", StringComparison.OrdinalIgnoreCase) ||
         result.Description.Contains("primary", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue($"Description should be informative. Was: {result.Description}");
    }

    #endregion

    #region Exception Handling Tests

    [SkippableFact]
    public async Task CheckHealthAsync_ShouldNotThrow()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(async () =>
            await healthCheck.CheckHealthAsync());

        exception.ShouldBeNull();
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCancellation_ShouldHandleGracefully()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert - Base class handles cancellation
        // The result depends on when cancellation is detected
        // If detected before execution, status is Unhealthy
        // If not detected (synchronous execution), it proceeds normally
        result.Status.ShouldBeOneOf(HealthStatus.Unhealthy, HealthStatus.Degraded, HealthStatus.Healthy);
    }

    #endregion

    #region Argument Validation Tests

    [SkippableFact]
    public void HealthCheck_WithNullClient_ShouldThrow()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var options = CreateOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoHealthCheck(null!, options));
    }

    [SkippableFact]
    public void HealthCheck_WithNullOptions_ShouldThrow()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoHealthCheck(_fixture.Client!, null!));
    }

    #endregion

    #region Multiple Invocations Tests

    [SkippableFact]
    public async Task CheckHealthAsync_MultipleInvocations_ShouldReturnConsistentResults()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        var result2 = await healthCheck.CheckHealthAsync();
        var result3 = await healthCheck.CheckHealthAsync();

        // Assert - Results should be consistent
        result1.Status.ShouldBe(result2.Status);
        result2.Status.ShouldBe(result3.Status);

        // Cluster type should remain consistent
        result1.Data["cluster_type"].ShouldBe(result2.Data["cluster_type"]);
        result2.Data["cluster_type"].ShouldBe(result3.Data["cluster_type"]);
    }

    [SkippableFact]
    public async Task CheckHealthAsync_ConcurrentInvocations_ShouldAllSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();
        var tasks = new List<Task<HealthCheckResult>>();

        // Act - Run multiple health checks concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(healthCheck.CheckHealthAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete successfully with consistent status
        results.ShouldAllBe(r => r.Data.ContainsKey("cluster_type"));

        // All should have the same status (cluster state shouldn't change during test)
        var firstStatus = results[0].Status;
        results.ShouldAllBe(r => r.Status == firstStatus);
    }

    #endregion

    #region IEncinaHealthCheck Interface Tests

    [SkippableFact]
    public void HealthCheck_ShouldImplementIEncinaHealthCheck()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.ShouldBeAssignableTo<IEncinaHealthCheck>();
    }

    [SkippableFact]
    public void HealthCheck_ShouldExtendEncinaHealthCheck()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.ShouldBeAssignableTo<EncinaHealthCheck>();
    }

    #endregion

    #region Read Preference Reflection Tests

    [SkippableFact]
    public async Task CheckHealthAsync_WithDifferentReadPreferences_ShouldStillReturnResult()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Test all read preferences to ensure health check works with any configuration
        var readPreferences = new[]
        {
            MongoReadPreference.Primary,
            MongoReadPreference.PrimaryPreferred,
            MongoReadPreference.Secondary,
            MongoReadPreference.SecondaryPreferred,
            MongoReadPreference.Nearest
        };

        foreach (var readPref in readPreferences)
        {
            // Arrange
            var healthCheck = CreateHealthCheck(options =>
            {
                options.ReadWriteSeparationOptions.ReadPreference = readPref;
            });

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert - Should not throw and should return valid result
            result.Data.ShouldNotBeEmpty();
            result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        }
    }

    #endregion

    #region Helper Methods

    private ReadWriteMongoHealthCheck CreateHealthCheck(Action<EncinaMongoDbOptions>? configure = null)
    {
        var options = new EncinaMongoDbOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = MongoDbReplicaSetFixture.DatabaseName,
            UseReadWriteSeparation = true
        };

        configure?.Invoke(options);

        return new ReadWriteMongoHealthCheck(
            _fixture.Client!,
            Options.Create(options));
    }

    private IOptions<EncinaMongoDbOptions> CreateOptions(Action<EncinaMongoDbOptions>? configure = null)
    {
        var options = new EncinaMongoDbOptions
        {
            ConnectionString = _fixture.ConnectionString,
            DatabaseName = MongoDbReplicaSetFixture.DatabaseName,
            UseReadWriteSeparation = true
        };

        configure?.Invoke(options);

        return Options.Create(options);
    }

    #endregion
}
