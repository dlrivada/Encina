using Encina.Sharding;
using Encina.Sharding.ReferenceTables;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ReferenceTableReplicator"/>.
/// Verifies null parameter handling for the primary constructor parameters.
/// </summary>
public sealed class ReferenceTableReplicatorGuardTests
{
    private readonly IReferenceTableRegistry _registry = Substitute.For<IReferenceTableRegistry>();
    private readonly IShardTopologyProvider _topologyProvider = Substitute.For<IShardTopologyProvider>();
    private readonly IReferenceTableStoreFactory _storeFactory = Substitute.For<IReferenceTableStoreFactory>();
    private readonly IOptions<ReferenceTableGlobalOptions> _globalOptions = Options.Create(new ReferenceTableGlobalOptions());
    private readonly ILogger<ReferenceTableReplicator> _logger = NullLogger<ReferenceTableReplicator>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws when registry is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableReplicator(null!, _topologyProvider, _storeFactory, _globalOptions, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when topology provider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTopologyProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableReplicator(_registry, null!, _storeFactory, _globalOptions, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when store factory is null.
    /// </summary>
    [Fact]
    public void Constructor_NullStoreFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableReplicator(_registry, _topologyProvider, null!, _globalOptions, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when global options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullGlobalOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableReplicator(_registry, _topologyProvider, _storeFactory, null!, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableReplicator(_registry, _topologyProvider, _storeFactory, _globalOptions, null!));
    }

    /// <summary>
    /// Verifies that the constructor succeeds with all valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        var replicator = new ReferenceTableReplicator(
            _registry, _topologyProvider, _storeFactory, _globalOptions, _logger);

        replicator.ShouldNotBeNull();
    }

    #endregion

    #region ReplicateAsync Guards

    /// <summary>
    /// Verifies that ReplicateAsync returns error when entity is not registered.
    /// </summary>
    [Fact]
    public async Task ReplicateAsync_UnregisteredEntity_ReturnsLeftError()
    {
        _registry.IsRegistered<TestReferenceEntity>().Returns(false);

        var replicator = CreateReplicator();

        var result = await replicator.ReplicateAsync<TestReferenceEntity>();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ReplicateAllAsync Guards

    /// <summary>
    /// Verifies that ReplicateAllAsync returns empty result when no entities registered.
    /// </summary>
    [Fact]
    public async Task ReplicateAllAsync_NoRegisteredEntities_ReturnsEmptyResult()
    {
        _registry.GetAllConfigurations().Returns(new List<ReferenceTableConfiguration>());

        var replicator = CreateReplicator();

        var result = await replicator.ReplicateAllAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.RowsSynced,
            Left: _ => -1).ShouldBe(0);
    }

    #endregion

    #region Test Helpers

    private ReferenceTableReplicator CreateReplicator() =>
        new(_registry, _topologyProvider, _storeFactory, _globalOptions, _logger);

    private sealed class TestReferenceEntity;

    #endregion
}
