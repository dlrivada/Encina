using Encina.Cdc.Debezium;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumCdcOptions"/> configuration class.
/// Verifies default values and property setters.
/// </summary>
public sealed class DebeziumCdcOptionsTests
{
    #region Default Values

    /// <summary>
    /// Verifies the default ListenUrl is "http://+".
    /// </summary>
    [Fact]
    public void Defaults_ListenUrl_IsHttpPlus()
    {
        var options = new DebeziumCdcOptions();
        options.ListenUrl.ShouldBe("http://+");
    }

    /// <summary>
    /// Verifies the default ListenPort is 8080.
    /// </summary>
    [Fact]
    public void Defaults_ListenPort_Is8080()
    {
        var options = new DebeziumCdcOptions();
        options.ListenPort.ShouldBe(8080);
    }

    /// <summary>
    /// Verifies the default ListenPath is "/debezium".
    /// </summary>
    [Fact]
    public void Defaults_ListenPath_IsDebezium()
    {
        var options = new DebeziumCdcOptions();
        options.ListenPath.ShouldBe("/debezium");
    }

    /// <summary>
    /// Verifies the default DebeziumServerUrl is null.
    /// </summary>
    [Fact]
    public void Defaults_DebeziumServerUrl_IsNull()
    {
        var options = new DebeziumCdcOptions();
        options.DebeziumServerUrl.ShouldBeNull();
    }

    /// <summary>
    /// Verifies the default BearerToken is null.
    /// </summary>
    [Fact]
    public void Defaults_BearerToken_IsNull()
    {
        var options = new DebeziumCdcOptions();
        options.BearerToken.ShouldBeNull();
    }

    /// <summary>
    /// Verifies the default EventFormat is CloudEvents.
    /// </summary>
    [Fact]
    public void Defaults_EventFormat_IsCloudEvents()
    {
        var options = new DebeziumCdcOptions();
        options.EventFormat.ShouldBe(DebeziumEventFormat.CloudEvents);
    }

    /// <summary>
    /// Verifies the default ChannelCapacity is 1000.
    /// </summary>
    [Fact]
    public void Defaults_ChannelCapacity_Is1000()
    {
        var options = new DebeziumCdcOptions();
        options.ChannelCapacity.ShouldBe(1000);
    }

    /// <summary>
    /// Verifies the default MaxListenerRetries is 5.
    /// </summary>
    [Fact]
    public void Defaults_MaxListenerRetries_Is5()
    {
        var options = new DebeziumCdcOptions();
        options.MaxListenerRetries.ShouldBe(5);
    }

    /// <summary>
    /// Verifies the default ListenerRetryDelay is 2 seconds.
    /// </summary>
    [Fact]
    public void Defaults_ListenerRetryDelay_IsTwoSeconds()
    {
        var options = new DebeziumCdcOptions();
        options.ListenerRetryDelay.ShouldBe(TimeSpan.FromSeconds(2));
    }

    #endregion

    #region Property Setters

    /// <summary>
    /// Verifies that all property setters work correctly.
    /// </summary>
    [Fact]
    public void PropertySetters_ShouldRoundTrip()
    {
        // Arrange
        var options = new DebeziumCdcOptions
        {
            ListenUrl = "http://localhost",
            ListenPort = 9090,
            ListenPath = "/events",
            DebeziumServerUrl = "http://debezium:8083",
            BearerToken = "secret-token",
            EventFormat = DebeziumEventFormat.Flat,
            ChannelCapacity = 500,
            MaxListenerRetries = 10,
            ListenerRetryDelay = TimeSpan.FromSeconds(5)
        };

        // Assert
        options.ListenUrl.ShouldBe("http://localhost");
        options.ListenPort.ShouldBe(9090);
        options.ListenPath.ShouldBe("/events");
        options.DebeziumServerUrl.ShouldBe("http://debezium:8083");
        options.BearerToken.ShouldBe("secret-token");
        options.EventFormat.ShouldBe(DebeziumEventFormat.Flat);
        options.ChannelCapacity.ShouldBe(500);
        options.MaxListenerRetries.ShouldBe(10);
        options.ListenerRetryDelay.ShouldBe(TimeSpan.FromSeconds(5));
    }

    #endregion
}
