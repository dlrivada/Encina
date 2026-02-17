using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Kafka;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumKafkaOptions"/> configuration class.
/// Verifies default values and property setters.
/// </summary>
public sealed class DebeziumKafkaOptionsTests
{
    #region Default Values

    /// <summary>
    /// Verifies the default BootstrapServers is "localhost:9092".
    /// </summary>
    [Fact]
    public void Defaults_BootstrapServers_IsLocalhost9092()
    {
        var options = new DebeziumKafkaOptions();
        options.BootstrapServers.ShouldBe("localhost:9092");
    }

    /// <summary>
    /// Verifies the default GroupId is "encina-cdc-debezium".
    /// </summary>
    [Fact]
    public void Defaults_GroupId_IsEncinaCdcDebezium()
    {
        var options = new DebeziumKafkaOptions();
        options.GroupId.ShouldBe("encina-cdc-debezium");
    }

    /// <summary>
    /// Verifies the default Topics is an empty array.
    /// </summary>
    [Fact]
    public void Defaults_Topics_IsEmptyArray()
    {
        var options = new DebeziumKafkaOptions();
        options.Topics.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies the default AutoOffsetReset is "earliest".
    /// </summary>
    [Fact]
    public void Defaults_AutoOffsetReset_IsEarliest()
    {
        var options = new DebeziumKafkaOptions();
        options.AutoOffsetReset.ShouldBe("earliest");
    }

    /// <summary>
    /// Verifies the default SessionTimeoutMs is 45000.
    /// </summary>
    [Fact]
    public void Defaults_SessionTimeoutMs_Is45000()
    {
        var options = new DebeziumKafkaOptions();
        options.SessionTimeoutMs.ShouldBe(45000);
    }

    /// <summary>
    /// Verifies the default MaxPollIntervalMs is 300000.
    /// </summary>
    [Fact]
    public void Defaults_MaxPollIntervalMs_Is300000()
    {
        var options = new DebeziumKafkaOptions();
        options.MaxPollIntervalMs.ShouldBe(300000);
    }

    /// <summary>
    /// Verifies the default EventFormat is Flat (Kafka uses raw Debezium envelopes).
    /// </summary>
    [Fact]
    public void Defaults_EventFormat_IsFlat()
    {
        var options = new DebeziumKafkaOptions();
        options.EventFormat.ShouldBe(DebeziumEventFormat.Flat);
    }

    /// <summary>
    /// Verifies that all security-related properties default to null.
    /// </summary>
    [Fact]
    public void Defaults_SecurityProperties_AreAllNull()
    {
        var options = new DebeziumKafkaOptions();

        options.SecurityProtocol.ShouldBeNull();
        options.SaslMechanism.ShouldBeNull();
        options.SaslUsername.ShouldBeNull();
        options.SaslPassword.ShouldBeNull();
        options.SslCaLocation.ShouldBeNull();
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
        var topics = new[] { "topic1", "topic2" };
        var options = new DebeziumKafkaOptions
        {
            BootstrapServers = "broker1:9092,broker2:9092",
            GroupId = "my-custom-group",
            Topics = topics,
            AutoOffsetReset = "latest",
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 600000,
            EventFormat = DebeziumEventFormat.CloudEvents,
            SecurityProtocol = "SASL_SSL",
            SaslMechanism = "SCRAM-SHA-256",
            SaslUsername = "user",
            SaslPassword = "pass",
            SslCaLocation = "/etc/ssl/ca.pem"
        };

        // Assert
        options.BootstrapServers.ShouldBe("broker1:9092,broker2:9092");
        options.GroupId.ShouldBe("my-custom-group");
        options.Topics.ShouldBe(topics);
        options.AutoOffsetReset.ShouldBe("latest");
        options.SessionTimeoutMs.ShouldBe(30000);
        options.MaxPollIntervalMs.ShouldBe(600000);
        options.EventFormat.ShouldBe(DebeziumEventFormat.CloudEvents);
        options.SecurityProtocol.ShouldBe("SASL_SSL");
        options.SaslMechanism.ShouldBe("SCRAM-SHA-256");
        options.SaslUsername.ShouldBe("user");
        options.SaslPassword.ShouldBe("pass");
        options.SslCaLocation.ShouldBe("/etc/ssl/ca.pem");
    }

    #endregion
}
