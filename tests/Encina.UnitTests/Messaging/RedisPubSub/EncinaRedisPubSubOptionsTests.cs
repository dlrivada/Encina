using Encina.Redis.PubSub;

namespace Encina.UnitTests.Messaging.RedisPubSub;

/// <summary>
/// Tests for the <see cref="EncinaRedisPubSubOptions"/> class.
/// </summary>
public sealed class EncinaRedisPubSubOptionsTests
{
    #region Default Values

    [Fact]
    public void Defaults_ConnectionString_IsLocalhost()
    {
        var options = new EncinaRedisPubSubOptions();
        options.ConnectionString.ShouldBe("localhost:6379");
    }

    [Fact]
    public void Defaults_ChannelPrefix_IsEncina()
    {
        var options = new EncinaRedisPubSubOptions();
        options.ChannelPrefix.ShouldBe("encina");
    }

    [Fact]
    public void Defaults_CommandChannel_IsCommands()
    {
        var options = new EncinaRedisPubSubOptions();
        options.CommandChannel.ShouldBe("commands");
    }

    [Fact]
    public void Defaults_EventChannel_IsEvents()
    {
        var options = new EncinaRedisPubSubOptions();
        options.EventChannel.ShouldBe("events");
    }

    [Fact]
    public void Defaults_UsePatternSubscription_IsFalse()
    {
        var options = new EncinaRedisPubSubOptions();
        options.UsePatternSubscription.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_ConnectTimeout_Is5000()
    {
        var options = new EncinaRedisPubSubOptions();
        options.ConnectTimeout.ShouldBe(5000);
    }

    [Fact]
    public void Defaults_SyncTimeout_Is5000()
    {
        var options = new EncinaRedisPubSubOptions();
        options.SyncTimeout.ShouldBe(5000);
    }

    #endregion

    #region Property Setters

    [Fact]
    public void AllProperties_AreSettable()
    {
        var options = new EncinaRedisPubSubOptions
        {
            ConnectionString = "redis-server:6380,password=secret",
            ChannelPrefix = "myapp",
            CommandChannel = "cmds",
            EventChannel = "evts",
            UsePatternSubscription = true,
            ConnectTimeout = 10000,
            SyncTimeout = 3000
        };

        options.ConnectionString.ShouldBe("redis-server:6380,password=secret");
        options.ChannelPrefix.ShouldBe("myapp");
        options.CommandChannel.ShouldBe("cmds");
        options.EventChannel.ShouldBe("evts");
        options.UsePatternSubscription.ShouldBeTrue();
        options.ConnectTimeout.ShouldBe(10000);
        options.SyncTimeout.ShouldBe(3000);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ReturnsExpectedFormat_WithDefaultPrefix()
    {
        var options = new EncinaRedisPubSubOptions();
        options.ToString().ShouldBe("EncinaRedisPubSubOptions { Prefix=encina }");
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat_WithCustomPrefix()
    {
        var options = new EncinaRedisPubSubOptions { ChannelPrefix = "custom-app" };
        options.ToString().ShouldBe("EncinaRedisPubSubOptions { Prefix=custom-app }");
    }

    [Fact]
    public void ToString_DoesNotContainConnectionString()
    {
        // ConnectionString has [JsonIgnore] — ensure ToString also does not leak it
        var options = new EncinaRedisPubSubOptions
        {
            ConnectionString = "redis-server:6380,password=SuperSecret123"
        };

        options.ToString().ShouldNotContain("SuperSecret123");
        options.ToString().ShouldNotContain("redis-server");
    }

    #endregion
}
