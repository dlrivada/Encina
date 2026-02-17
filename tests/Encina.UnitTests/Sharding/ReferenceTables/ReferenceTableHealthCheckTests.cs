using Encina.Messaging.Health;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables.Health;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableHealthCheck"/>.
/// </summary>
public sealed class ReferenceTableHealthCheckTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Country
    {
        public int Id { get; set; }
    }

    private sealed class Currency
    {
        public int Id { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private readonly IReferenceTableRegistry _registry = Substitute.For<IReferenceTableRegistry>();
    private readonly IReferenceTableStateStore _stateStore = Substitute.For<IReferenceTableStateStore>();

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableHealthCheck(null!, _stateStore));
    }

    [Fact]
    public void Constructor_NullStateStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableHealthCheck(_registry, null!));
    }

    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            new ReferenceTableHealthCheck(_registry, _stateStore));
    }

    [Fact]
    public void Constructor_WithOptions_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            new ReferenceTableHealthCheck(_registry, _stateStore, new ReferenceTableHealthCheckOptions()));
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Name and Tags
    // ────────────────────────────────────────────────────────────

    #region Name and Tags

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Act & Assert
        ReferenceTableHealthCheck.DefaultName.ShouldBe("encina-reference-table-replication");
    }

    [Fact]
    public void DefaultTags_ContainsExpectedTags()
    {
        // Act & Assert
        ReferenceTableHealthCheck.DefaultTags.ShouldContain("ready");
        ReferenceTableHealthCheck.DefaultTags.ShouldContain("database");
        ReferenceTableHealthCheck.DefaultTags.ShouldContain("sharding");
        ReferenceTableHealthCheck.DefaultTags.ShouldContain("replication");
    }

    [Fact]
    public void Name_ReturnsDefaultName()
    {
        // Arrange
        var check = new ReferenceTableHealthCheck(_registry, _stateStore);

        // Act & Assert
        check.Name.ShouldBe(ReferenceTableHealthCheck.DefaultName);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  CheckHealthAsync — No Tables
    // ────────────────────────────────────────────────────────────

    #region No Registered Tables

    [Fact]
    public async Task CheckHealthAsync_NoRegisteredTables_ReturnsHealthy()
    {
        // Arrange
        _registry.GetAllConfigurations()
            .Returns(new List<ReferenceTableConfiguration>());
        var check = new ReferenceTableHealthCheck(_registry, _stateStore);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("registered_tables");
        result.Data["registered_tables"].ShouldBe(0);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  CheckHealthAsync — Within Thresholds
    // ────────────────────────────────────────────────────────────

    #region All Within Threshold

    [Fact]
    public async Task CheckHealthAsync_AllTablesWithinThreshold_ReturnsHealthy()
    {
        // Arrange
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns(DateTime.UtcNow.AddSeconds(-10));

        var check = new ReferenceTableHealthCheck(_registry, _stateStore);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  CheckHealthAsync — Never Replicated
    // ────────────────────────────────────────────────────────────

    #region Never Replicated

    [Fact]
    public async Task CheckHealthAsync_TableNeverReplicated_ReturnsUnhealthy()
    {
        // Arrange
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns((DateTime?)null);

        var check = new ReferenceTableHealthCheck(_registry, _stateStore);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  CheckHealthAsync — Degraded
    // ────────────────────────────────────────────────────────────

    #region Degraded Threshold

    [Fact]
    public async Task CheckHealthAsync_TableExceedsDegradedThreshold_ReturnsDegraded()
    {
        // Arrange
        var options = new ReferenceTableHealthCheckOptions
        {
            DegradedThreshold = TimeSpan.FromMinutes(1),
            UnhealthyThreshold = TimeSpan.FromMinutes(5)
        };
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns(DateTime.UtcNow.AddMinutes(-2));

        var check = new ReferenceTableHealthCheck(_registry, _stateStore, options);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  CheckHealthAsync — Unhealthy Threshold
    // ────────────────────────────────────────────────────────────

    #region Unhealthy Threshold

    [Fact]
    public async Task CheckHealthAsync_TableExceedsUnhealthyThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var options = new ReferenceTableHealthCheckOptions
        {
            DegradedThreshold = TimeSpan.FromMinutes(1),
            UnhealthyThreshold = TimeSpan.FromMinutes(5)
        };
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns(DateTime.UtcNow.AddMinutes(-10));

        var check = new ReferenceTableHealthCheck(_registry, _stateStore, options);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  CheckHealthAsync — Mixed Health
    // ────────────────────────────────────────────────────────────

    #region Mixed Health

    [Fact]
    public async Task CheckHealthAsync_MixedHealthTables_ReturnsWorstStatus()
    {
        // Arrange
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions()),
            new(typeof(Currency), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        // Country: recently replicated (healthy)
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns(DateTime.UtcNow.AddSeconds(-10));
        // Currency: never replicated (unhealthy)
        _stateStore.GetLastReplicationTimeAsync(typeof(Currency), Arg.Any<CancellationToken>())
            .Returns((DateTime?)null);

        var check = new ReferenceTableHealthCheck(_registry, _stateStore);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_HealthyAndDegraded_ReturnsDegraded()
    {
        // Arrange
        var options = new ReferenceTableHealthCheckOptions
        {
            DegradedThreshold = TimeSpan.FromMinutes(1),
            UnhealthyThreshold = TimeSpan.FromMinutes(5)
        };
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions()),
            new(typeof(Currency), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        // Country: recently replicated (healthy)
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns(DateTime.UtcNow.AddSeconds(-10));
        // Currency: stale but within unhealthy threshold (degraded)
        _stateStore.GetLastReplicationTimeAsync(typeof(Currency), Arg.Any<CancellationToken>())
            .Returns(DateTime.UtcNow.AddMinutes(-2));

        var check = new ReferenceTableHealthCheck(_registry, _stateStore, options);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  CheckHealthAsync — Data Dictionary
    // ────────────────────────────────────────────────────────────

    #region Data Dictionary

    [Fact]
    public async Task CheckHealthAsync_ReturnsDataWithRegisteredTableCount()
    {
        // Arrange
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns(DateTime.UtcNow);

        var check = new ReferenceTableHealthCheck(_registry, _stateStore);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("registered_tables");
        result.Data["registered_tables"].ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_UnhealthyResult_ContainsThresholdData()
    {
        // Arrange
        var options = new ReferenceTableHealthCheckOptions
        {
            DegradedThreshold = TimeSpan.FromMinutes(1),
            UnhealthyThreshold = TimeSpan.FromMinutes(5)
        };
        var configs = new List<ReferenceTableConfiguration>
        {
            new(typeof(Country), new ReferenceTableOptions())
        };
        _registry.GetAllConfigurations().Returns(configs);
        _stateStore.GetLastReplicationTimeAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns((DateTime?)null);

        var check = new ReferenceTableHealthCheck(_registry, _stateStore, options);

        // Act
        var result = await check.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("unhealthy_threshold_minutes");
        result.Data["unhealthy_threshold_minutes"].ShouldBe(5.0);
        result.Data.ShouldContainKey("degraded_threshold_minutes");
        result.Data["degraded_threshold_minutes"].ShouldBe(1.0);
    }

    #endregion
}
