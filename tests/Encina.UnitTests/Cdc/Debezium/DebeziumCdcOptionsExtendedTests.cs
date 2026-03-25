using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Kafka;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Extended tests for <see cref="DebeziumCdcOptions"/> and <see cref="DebeziumKafkaOptions"/>
/// covering ToString and additional edge cases.
/// </summary>
public sealed class DebeziumCdcOptionsExtendedTests
{
    [Fact]
    public void DebeziumCdcOptions_ToString_ContainsRelevantInfo()
    {
        var options = new DebeziumCdcOptions
        {
            ListenUrl = "http://0.0.0.0",
            ListenPort = 9090,
            ListenPath = "/events",
            EventFormat = DebeziumEventFormat.Flat
        };

        var result = options.ToString();
        result.ShouldContain("http://0.0.0.0");
        result.ShouldContain("9090");
        result.ShouldContain("/events");
        result.ShouldContain("Flat");
    }

    [Fact]
    public void DebeziumCdcOptions_ToString_DefaultValues_ContainsDefaults()
    {
        var options = new DebeziumCdcOptions();

        var result = options.ToString();
        result.ShouldContain("http://+");
        result.ShouldContain("8080");
        result.ShouldContain("/debezium");
        result.ShouldContain("CloudEvents");
    }

    [Fact]
    public void DebeziumKafkaOptions_ToString_ContainsRelevantInfo()
    {
        var options = new DebeziumKafkaOptions
        {
            BootstrapServers = "broker1:9092,broker2:9092",
            GroupId = "my-group",
            Topics = ["topic1", "topic2", "topic3"]
        };

        var result = options.ToString();
        result.ShouldContain("broker1:9092,broker2:9092");
        result.ShouldContain("my-group");
        result.ShouldContain("3"); // Topics count
    }

    [Fact]
    public void DebeziumKafkaOptions_ToString_DefaultValues()
    {
        var options = new DebeziumKafkaOptions();

        var result = options.ToString();
        result.ShouldContain("localhost:9092");
        result.ShouldContain("encina-cdc-debezium");
        result.ShouldContain("0"); // Topics count
    }

    #region Null Guard Tests

    [Fact]
    public void DebeziumCdcPosition_NullOffsetJson_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new DebeziumCdcPosition(null!));
    }

    [Fact]
    public void DebeziumCdcPosition_EmptyOffsetJson_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new DebeziumCdcPosition(""));
    }

    [Fact]
    public void DebeziumCdcPosition_WhitespaceOffsetJson_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new DebeziumCdcPosition("   "));
    }

    [Fact]
    public void DebeziumCdcPosition_FromBytes_NullBytes_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => DebeziumCdcPosition.FromBytes(null!));
    }

    [Fact]
    public void DebeziumKafkaPosition_NullOffsetJson_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new DebeziumKafkaPosition(null!, "topic", 0, 0));
    }

    [Fact]
    public void DebeziumKafkaPosition_NullTopic_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new DebeziumKafkaPosition("{}", null!, 0, 0));
    }

    [Fact]
    public void DebeziumKafkaPosition_EmptyTopic_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new DebeziumKafkaPosition("{}", "", 0, 0));
    }

    [Fact]
    public void DebeziumKafkaPosition_FromBytes_NullBytes_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => DebeziumKafkaPosition.FromBytes(null!));
    }

    #endregion

    #region ServiceCollectionExtensions Null Guard Tests

    [Fact]
    public void AddEncinaCdcDebezium_NullServices_ThrowsArgumentNullException()
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaCdcDebezium(_ => { }));
    }

    [Fact]
    public void AddEncinaCdcDebezium_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcDebezium(null!));
    }

    [Fact]
    public void AddEncinaCdcDebeziumKafka_NullServices_ThrowsArgumentNullException()
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaCdcDebeziumKafka(_ => { }));
    }

    [Fact]
    public void AddEncinaCdcDebeziumKafka_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcDebeziumKafka(null!));
    }

    #endregion
}
