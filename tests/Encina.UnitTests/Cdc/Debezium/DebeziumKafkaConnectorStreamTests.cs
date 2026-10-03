using Confluent.Kafka;

using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Kafka;

using LanguageExt;

using Microsoft.Extensions.Logging.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for the consume loop and the consumer configuration of <see cref="DebeziumKafkaConnector"/>
/// against a fake Kafka consumer (#1557).
/// </summary>
[Trait("Category", "Unit")]
public sealed class DebeziumKafkaConnectorStreamTests
{
    private const string EventJson = """{"op":"c","after":{"id":1},"source":{"db":"testdb","table":"Orders"}}""";

    private readonly ICdcPositionStore _store = Substitute.For<ICdcPositionStore>();
    private readonly FakeLogger<DebeziumKafkaConnector> _logger = new();
    private readonly IConsumer<string, string> _consumer = Substitute.For<IConsumer<string, string>>();
    private readonly Queue<Func<ConsumeResult<string, string>>> _script = new();

    public DebeziumKafkaConnectorStreamTests()
    {
        _store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Option<CdcPosition>>(None)));
        _consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ => _script.Count == 0 ? throw new OperationCanceledException() : _script.Dequeue()());
    }

    private DebeziumKafkaConnector CreateConnector() =>
        new(new DebeziumKafkaOptions { Topics = ["orders"] }, _store, _logger, null, _consumer);

    private static ConsumeResult<string, string> Record(string? value, long offset, int partition = 2) =>
        new()
        {
            Topic = "orders",
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = new Message<string, string> { Value = value! }
        };

    private async Task<List<Either<EncinaError, ChangeEvent>>> StreamAllAsync()
    {
        using var connector = CreateConnector();
        var items = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var item in connector.StreamChangesAsync(CancellationToken.None))
        {
            items.Add(item);
        }

        return items;
    }

    [Fact]
    public async Task Stream_YieldsEventsWithTheKafkaPosition()
    {
        _script.Enqueue(() => Record(EventJson, offset: 7));

        var items = await StreamAllAsync();

        items.Count.ShouldBe(1);
        var changeEvent = (ChangeEvent)items[0];
        var position = changeEvent.Metadata.Position.ShouldBeOfType<DebeziumKafkaPosition>();
        position.Topic.ShouldBe("orders");
        position.Partition.ShouldBe(2);
        position.Offset.ShouldBe(7);
    }

    [Fact]
    public async Task Stream_SkipsEventsAtOrBeforeTheSavedPosition_ThenYieldsTheRest()
    {
        _store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Option<CdcPosition>>(
                Some<CdcPosition>(new DebeziumKafkaPosition("{}", "orders", 2, 42)))));
        _script.Enqueue(() => Record(EventJson, offset: 42));
        _script.Enqueue(() => Record(EventJson, offset: 43));
        _script.Enqueue(() => Record(EventJson, offset: 41));

        var items = await StreamAllAsync();

        // 42 is skipped; 43 passes the resume point so a later lower offset is no longer skipped.
        items.Count.ShouldBe(2);
        ((DebeziumKafkaPosition)((ChangeEvent)items[0]).Metadata.Position).Offset.ShouldBe(43);
        ((DebeziumKafkaPosition)((ChangeEvent)items[1]).Metadata.Position).Offset.ShouldBe(41);
    }

    [Fact]
    public async Task Stream_EventOfAnotherPartition_IsNotSkipped()
    {
        _store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Option<CdcPosition>>(
                Some<CdcPosition>(new DebeziumKafkaPosition("{}", "orders", 2, 42)))));
        _script.Enqueue(() => Record(EventJson, offset: 10, partition: 3));

        var items = await StreamAllAsync();

        items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Stream_MessageWithoutValue_IsIgnored()
    {
        _script.Enqueue(() => Record(null, offset: 1));

        (await StreamAllAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Stream_InvalidJson_YieldsALeft()
    {
        _script.Enqueue(() => Record("{not json", offset: 1));

        var items = await StreamAllAsync();

        items.Count.ShouldBe(1);
        items[0].IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Stream_ConsumeException_YieldsALeft_AndLogsOnlyTheErrorCode()
    {
        const string Reason = "broker kafka-prod-7.internal:9092 refused";
        _script.Enqueue(() => throw new ConsumeException(
            new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.Local_Transport, Reason)));

        var items = await StreamAllAsync();

        items.Count.ShouldBe(1);
        items[0].IsLeft.ShouldBeTrue();
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Message.Contains(nameof(ErrorCode.Local_Transport)));
        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Message.Contains("kafka-prod-7"));
    }

    [Fact]
    public void BuildConfig_MapsTheOptions()
    {
        var config = DebeziumKafkaConnector.BuildConfig(new DebeziumKafkaOptions
        {
            BootstrapServers = "broker:9092",
            GroupId = "g",
            AutoOffsetReset = "latest",
            SecurityProtocol = "SASL_SSL",
            SaslMechanism = "SCRAM-SHA-512",
            SaslUsername = "user",
            SaslPassword = "pw",
            SslCaLocation = "/ca.pem"
        });

        config.BootstrapServers.ShouldBe("broker:9092");
        config.GroupId.ShouldBe("g");
        config.AutoOffsetReset.ShouldBe(Confluent.Kafka.AutoOffsetReset.Latest);
        config.EnableAutoCommit.ShouldBe(false);
        config.SecurityProtocol.ShouldBe(Confluent.Kafka.SecurityProtocol.SaslSsl);
        config.SaslMechanism.ShouldBe(Confluent.Kafka.SaslMechanism.ScramSha512);
        config.SaslUsername.ShouldBe("user");
        config.SaslPassword.ShouldBe("pw");
        config.SslCaLocation.ShouldBe("/ca.pem");
    }

    [Theory]
    [InlineData("SSL", Confluent.Kafka.SecurityProtocol.Ssl)]
    [InlineData("SASL_PLAINTEXT", Confluent.Kafka.SecurityProtocol.SaslPlaintext)]
    [InlineData("other", Confluent.Kafka.SecurityProtocol.Plaintext)]
    public void BuildConfig_MapsTheSecurityProtocol(string value, Confluent.Kafka.SecurityProtocol expected) =>
        DebeziumKafkaConnector.BuildConfig(new DebeziumKafkaOptions { SecurityProtocol = value })
            .SecurityProtocol.ShouldBe(expected);

    [Theory]
    [InlineData("PLAIN", Confluent.Kafka.SaslMechanism.Plain)]
    [InlineData("SCRAM-SHA-256", Confluent.Kafka.SaslMechanism.ScramSha256)]
    [InlineData("GSSAPI", Confluent.Kafka.SaslMechanism.Gssapi)]
    [InlineData("other", Confluent.Kafka.SaslMechanism.Plain)]
    public void BuildConfig_MapsTheSaslMechanism(string value, Confluent.Kafka.SaslMechanism expected) =>
        DebeziumKafkaConnector.BuildConfig(new DebeziumKafkaOptions { SaslMechanism = value })
            .SaslMechanism.ShouldBe(expected);

    [Fact]
    public void BuildConfig_DefaultsToEarliestAndLeavesSecurityUnset()
    {
        var config = DebeziumKafkaConnector.BuildConfig(new DebeziumKafkaOptions { AutoOffsetReset = "bogus" });

        config.AutoOffsetReset.ShouldBe(Confluent.Kafka.AutoOffsetReset.Earliest);
        config.SecurityProtocol.ShouldBeNull();
        config.SaslUsername.ShouldBeNull();
    }
}
