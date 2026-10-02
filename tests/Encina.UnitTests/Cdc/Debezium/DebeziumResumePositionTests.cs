using System.Text.Json;
using System.Threading.Channels;

using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Kafka;
using Encina.UnitTests.Support;

using LanguageExt;

using Microsoft.Extensions.Logging.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for the resume-position lookup of <see cref="DebeziumCdcConnector"/> and
/// <see cref="DebeziumKafkaConnector"/>: a saved position of the connector's own kind is returned, every
/// other outcome starts from the beginning and a store failure is logged redacted (#1557).
/// </summary>
[Trait("Category", "Unit")]
public sealed class DebeziumResumePositionTests
{
    private const string Sentinel = "SENTINEL-POSITION-STORE-0d4";

    private readonly ICdcPositionStore _store = Substitute.For<ICdcPositionStore>();
    private readonly FakeLogger<DebeziumCdcConnector> _httpLogger = new();
    private readonly FakeLogger<DebeziumKafkaConnector> _kafkaLogger = new();

    private static readonly DebeziumCdcPosition HttpPosition = new("{\"lsn\":1}");
    private static readonly DebeziumKafkaPosition KafkaPosition = new("{\"o\":1}", "orders", 2, 42);

    private DebeziumCdcConnector CreateHttpConnector() =>
        new(new DebeziumCdcOptions(), Channel.CreateUnbounded<JsonElement>(), _store, _httpLogger);

    private DebeziumKafkaConnector CreateKafkaConnector() =>
        new(new DebeziumKafkaOptions { Topics = ["orders"] }, _store, _kafkaLogger);

    private void StoreReturns(Either<EncinaError, Option<CdcPosition>> result) =>
        _store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

    [Fact]
    public async Task Http_SavedDebeziumPosition_IsReturned()
    {
        StoreReturns(Right<EncinaError, Option<CdcPosition>>(Some<CdcPosition>(HttpPosition)));

        var position = await CreateHttpConnector().GetResumePositionAsync(CancellationToken.None);

        position.ShouldBeSameAs(HttpPosition);
    }

    [Fact]
    public async Task Http_NoSavedPosition_ReturnsNull()
    {
        StoreReturns(Right<EncinaError, Option<CdcPosition>>(None));

        (await CreateHttpConnector().GetResumePositionAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Http_StoreReturnsError_ReturnsNull()
    {
        StoreReturns(Left<EncinaError, Option<CdcPosition>>(EncinaErrors.Create("test.store", "failed")));

        (await CreateHttpConnector().GetResumePositionAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Http_SavedPositionOfAnotherKind_ReturnsNull()
    {
        StoreReturns(Right<EncinaError, Option<CdcPosition>>(Some<CdcPosition>(KafkaPosition)));

        (await CreateHttpConnector().GetResumePositionAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Http_StoreThrows_ReturnsNullAndLogsRedactedException()
    {
        _store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(Sentinel));

        var position = await CreateHttpConnector().GetResumePositionAsync(CancellationToken.None);

        position.ShouldBeNull();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_httpLogger, Sentinel);
    }

    [Fact]
    public async Task Kafka_SavedKafkaPosition_IsReturned()
    {
        StoreReturns(Right<EncinaError, Option<CdcPosition>>(Some<CdcPosition>(KafkaPosition)));
        using var connector = CreateKafkaConnector();

        var position = await connector.GetResumePositionAsync(CancellationToken.None);

        position.ShouldBeSameAs(KafkaPosition);
    }

    [Fact]
    public async Task Kafka_NoSavedPosition_ReturnsNull()
    {
        StoreReturns(Right<EncinaError, Option<CdcPosition>>(None));
        using var connector = CreateKafkaConnector();

        (await connector.GetResumePositionAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Kafka_StoreReturnsError_ReturnsNull()
    {
        StoreReturns(Left<EncinaError, Option<CdcPosition>>(EncinaErrors.Create("test.store", "failed")));
        using var connector = CreateKafkaConnector();

        (await connector.GetResumePositionAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Kafka_SavedPositionOfAnotherKind_ReturnsNull()
    {
        StoreReturns(Right<EncinaError, Option<CdcPosition>>(Some<CdcPosition>(HttpPosition)));
        using var connector = CreateKafkaConnector();

        (await connector.GetResumePositionAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Kafka_StoreThrows_ReturnsNullAndLogsRedactedException()
    {
        _store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(Sentinel));
        using var connector = CreateKafkaConnector();

        var position = await connector.GetResumePositionAsync(CancellationToken.None);

        position.ShouldBeNull();
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_kafkaLogger, Sentinel);
    }
}
