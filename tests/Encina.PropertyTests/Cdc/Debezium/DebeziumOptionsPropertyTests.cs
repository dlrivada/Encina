using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Kafka;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.Debezium;

/// <summary>
/// Property-based tests for <see cref="DebeziumCdcOptions"/> and <see cref="DebeziumKafkaOptions"/>.
/// Verifies that property setters and getters round-trip correctly for randomized values.
/// </summary>
[Trait("Category", "Property")]
public sealed class DebeziumOptionsPropertyTests
{
    #region Kafka Options Set/Get

    /// <summary>
    /// Property: BootstrapServers setter/getter round-trips for all non-null strings.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_KafkaOptions_BootstrapServers_RoundTrips()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            value =>
            {
                var options = new DebeziumKafkaOptions { BootstrapServers = value };
                return options.BootstrapServers == value;
            });
    }

    /// <summary>
    /// Property: GroupId setter/getter round-trips for all non-null strings.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_KafkaOptions_GroupId_RoundTrips()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            value =>
            {
                var options = new DebeziumKafkaOptions { GroupId = value };
                return options.GroupId == value;
            });
    }

    /// <summary>
    /// Property: SessionTimeoutMs setter/getter round-trips for all positive integers.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_KafkaOptions_SessionTimeoutMs_RoundTrips(PositiveInt value)
    {
        var options = new DebeziumKafkaOptions { SessionTimeoutMs = value.Get };
        return options.SessionTimeoutMs == value.Get;
    }

    /// <summary>
    /// Property: Topics array setter/getter round-trips correctly.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_KafkaOptions_Topics_RoundTrips()
    {
        return Prop.ForAll(
            Arb.From(Gen.ArrayOf(GenNonEmptyString())),
            topics =>
            {
                var options = new DebeziumKafkaOptions { Topics = topics };
                return options.Topics.SequenceEqual(topics);
            });
    }

    #endregion

    #region HTTP Options Set/Get

    /// <summary>
    /// Property: ListenPort setter/getter round-trips for all valid port values.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_HttpOptions_ListenPort_RoundTrips(PositiveInt value)
    {
        var port = value.Get % 65536;
        var options = new DebeziumCdcOptions { ListenPort = port };
        return options.ListenPort == port;
    }

    /// <summary>
    /// Property: ChannelCapacity setter/getter round-trips for all positive integers.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_HttpOptions_ChannelCapacity_RoundTrips(PositiveInt value)
    {
        var options = new DebeziumCdcOptions { ChannelCapacity = value.Get };
        return options.ChannelCapacity == value.Get;
    }

    /// <summary>
    /// Property: MaxListenerRetries setter/getter round-trips for all positive integers.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_HttpOptions_MaxListenerRetries_RoundTrips(PositiveInt value)
    {
        var options = new DebeziumCdcOptions { MaxListenerRetries = value.Get };
        return options.MaxListenerRetries == value.Get;
    }

    #endregion

    #region Generators

    private static Gen<string> GenNonEmptyString()
    {
        return Gen.Elements("localhost:9092", "broker1:9092", "group-1", "topic-1", "test-value")
            .SelectMany(prefix =>
                Gen.Choose(1, 1000).Select(n => $"{prefix}-{n}"));
    }

    #endregion
}
