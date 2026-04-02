using Encina.Database;
using Encina.MongoDB.Health;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Health;

public class HealthCheckGuardTests
{
    #region MongoDbHealthCheck — constructor does not have null guards on serviceProvider
    // serviceProvider is NOT null-checked in MongoDbHealthCheck constructor, but
    // MongoDbDatabaseHealthMonitor does check it.

    #endregion

    #region MongoDbDatabaseHealthMonitor

    [Fact]
    public void DatabaseHealthMonitor_NullServiceProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MongoDbDatabaseHealthMonitor(null!));

    [Fact]
    public void DatabaseHealthMonitor_NullServiceProvider_WithOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MongoDbDatabaseHealthMonitor(null!, new DatabaseResilienceOptions()));

    [Fact]
    public void DatabaseHealthMonitor_ProviderName_IsMongoDB()
    {
        var sp = Substitute.For<IServiceProvider>();
        var monitor = new MongoDbDatabaseHealthMonitor(sp);
        monitor.ProviderName.ShouldBe("mongodb");
    }

    [Fact]
    public void DatabaseHealthMonitor_IsCircuitOpen_DefaultsFalse()
    {
        var sp = Substitute.For<IServiceProvider>();
        var monitor = new MongoDbDatabaseHealthMonitor(sp);
        monitor.IsCircuitOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task DatabaseHealthMonitor_ClearPoolAsync_DoesNotThrow()
    {
        var sp = Substitute.For<IServiceProvider>();
        var monitor = new MongoDbDatabaseHealthMonitor(sp);
        await Should.NotThrowAsync(async () =>
            await monitor.ClearPoolAsync());
    }

    #endregion

    #region MongoDbHealthCheck — construction with and without options

    [Fact]
    public void MongoDbHealthCheck_NullOptions_UsesDefaults()
    {
        var sp = Substitute.For<IServiceProvider>();
        var check = new MongoDbHealthCheck(sp, null);
        check.Name.ShouldBe(MongoDbHealthCheck.DefaultName);
    }

    #endregion
}
