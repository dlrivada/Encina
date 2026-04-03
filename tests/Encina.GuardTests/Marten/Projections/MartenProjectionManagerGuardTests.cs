using Encina.Marten;
using Encina.Marten.Projections;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Projections;

/// <summary>
/// Deep guard tests for <see cref="MartenProjectionManager"/> covering constructor
/// and method-level null/invalid argument validation.
/// </summary>
public class MartenProjectionManagerGuardTests
{
    private static readonly IDocumentStore Store = Substitute.For<IDocumentStore>();
    private static readonly IServiceProvider ServiceProvider = Substitute.For<IServiceProvider>();
    private static readonly ILogger<MartenProjectionManager> Logger = NullLogger<MartenProjectionManager>.Instance;
    private static readonly ProjectionRegistry Registry = new();

    #region Constructor Guards

    [Fact]
    public void Constructor_NullStore_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(null!, ServiceProvider, Logger, Registry));

    [Fact]
    public void Constructor_NullServiceProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(Store, null!, Logger, Registry));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(Store, ServiceProvider, null!, Registry));

    [Fact]
    public void Constructor_NullRegistry_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(Store, ServiceProvider, Logger, null!));

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        // Null timeProvider should not throw - it defaults to TimeProvider.System
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry, timeProvider: null);
        manager.ShouldNotBeNull();
    }

    #endregion

    #region RebuildAsync Guards

    [Fact]
    public async Task RebuildAsync_WithOptions_NullOptions_Throws()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await manager.RebuildAsync<TestReadModel>(null!));
    }

    [Fact]
    public async Task RebuildAsync_UnregisteredReadModel_ReturnsLeft()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        var result = await manager.RebuildAsync<TestReadModel>();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetStatusAsync Guards

    [Fact]
    public async Task GetStatusAsync_UnregisteredReadModel_ReturnsLeft()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        var result = await manager.GetStatusAsync<TestReadModel>();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region StartAsync Guards

    [Fact]
    public async Task StartAsync_UnregisteredReadModel_ReturnsLeft()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        var result = await manager.StartAsync<TestReadModel>();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region StopAsync Guards

    [Fact]
    public async Task StopAsync_UnregisteredReadModel_ReturnsLeft()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        var result = await manager.StopAsync<TestReadModel>();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region PauseAsync Guards

    [Fact]
    public async Task PauseAsync_UnregisteredReadModel_ReturnsLeft()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        var result = await manager.PauseAsync<TestReadModel>();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ResumeAsync Guards

    [Fact]
    public async Task ResumeAsync_UnregisteredReadModel_ReturnsLeft()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        var result = await manager.ResumeAsync<TestReadModel>();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetAllStatusesAsync

    [Fact]
    public async Task GetAllStatusesAsync_EmptyRegistry_ReturnsEmptyDictionary()
    {
        var manager = new MartenProjectionManager(Store, ServiceProvider, Logger, Registry);
        var result = await manager.GetAllStatusesAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: statuses => statuses.Count.ShouldBe(0),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    public class TestReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }
}
