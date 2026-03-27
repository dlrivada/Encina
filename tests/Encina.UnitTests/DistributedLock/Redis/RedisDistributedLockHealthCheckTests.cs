using Encina.DistributedLock.Redis.Health;
using Encina.Messaging.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Encina.UnitTests.DistributedLock.Redis;

public class RedisDistributedLockHealthCheckTests
{
    private readonly IConnectionMultiplexer _connection = Substitute.For<IConnectionMultiplexer>();

    [Fact]
    public void Constructor_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockHealthCheck(null!, new ProviderHealthCheckOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockHealthCheck(_connection, null!));
    }

    [Fact]
    public void Name_DefaultOptions_ReturnsDefaultName()
    {
        var hc = new RedisDistributedLockHealthCheck(_connection, new ProviderHealthCheckOptions());
        hc.Name.ShouldBe("redis-distributed-lock");
    }

    [Fact]
    public void Name_CustomName_ReturnsCustom()
    {
        var hc = new RedisDistributedLockHealthCheck(_connection, new ProviderHealthCheckOptions { Name = "my-lock" });
        hc.Name.ShouldBe("my-lock");
    }

    [Fact]
    public void Tags_DefaultOptions_ContainsExpected()
    {
        var hc = new RedisDistributedLockHealthCheck(_connection, new ProviderHealthCheckOptions());
        var tags = hc.Tags.ToList();
        // Default ProviderHealthCheckOptions has ["encina", "database", "ready"]
        tags.ShouldContain("encina");
        tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessfulSetNx_ReturnsHealthy()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);
        _connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        var hc = new RedisDistributedLockHealthCheck(_connection, new ProviderHealthCheckOptions());
        var result = await hc.CheckHealthAsync();

        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ConnectionFails_ReturnsUnhealthy()
    {
        _connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "timeout"));

        var hc = new RedisDistributedLockHealthCheck(_connection, new ProviderHealthCheckOptions());
        var result = await hc.CheckHealthAsync();

        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Redis connection failed");
    }
}
