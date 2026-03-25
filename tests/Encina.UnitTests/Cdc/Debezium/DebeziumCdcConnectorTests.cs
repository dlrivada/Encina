using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumCdcConnector"/> (internal, tested via ICdcConnector).
/// Covers GetCurrentPositionAsync, StreamChangesAsync, constructor validation,
/// resume-from-position, and error handling.
/// </summary>
public sealed class DebeziumCdcConnectorTests
{
    private readonly DebeziumCdcOptions _options;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<DebeziumCdcConnector> _logger;
    private readonly Channel<JsonElement> _channel;
    private readonly TimeProvider _timeProvider;

    public DebeziumCdcConnectorTests()
    {
        _options = new DebeziumCdcOptions();
        _positionStore = Substitute.For<ICdcPositionStore>();
        _logger = Substitute.For<ILogger<DebeziumCdcConnector>>();
        _channel = Channel.CreateUnbounded<JsonElement>();
        _timeProvider = TimeProvider.System;
    }

    private DebeziumCdcConnector CreateConnector() =>
        new(_options, _channel, _positionStore, _logger, _timeProvider);

    #region Constructor Tests

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DebeziumCdcConnector(null!, _channel, _positionStore, _logger));
    }

    [Fact]
    public void Constructor_NullChannel_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DebeziumCdcConnector(_options, null!, _positionStore, _logger));
    }

    [Fact]
    public void Constructor_NullPositionStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DebeziumCdcConnector(_options, _channel, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DebeziumCdcConnector(_options, _channel, _positionStore, null!));
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        var connector = new DebeziumCdcConnector(_options, _channel, _positionStore, _logger, null);
        connector.ShouldNotBeNull();
    }

    #endregion

    #region ConnectorId Tests

    [Fact]
    public void ConnectorId_ShouldReturnExpectedValue()
    {
        var connector = CreateConnector();
        connector.ConnectorId.ShouldBe("encina-cdc-debezium");
    }

    #endregion

    #region GetCurrentPositionAsync Tests

    [Fact]
    public async Task GetCurrentPositionAsync_WhenPositionExists_ReturnsPosition()
    {
        // Arrange
        var savedPosition = new DebeziumCdcPosition("{\"lsn\":12345}");
        var option = Option<CdcPosition>.Some(savedPosition);
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<CdcPosition>>(option));

        var connector = CreateConnector();

        // Act
        var result = await connector.GetCurrentPositionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentPositionAsync_WhenNoPositionSaved_ReturnsAwaitingEventsPosition()
    {
        // Arrange
        var option = Option<CdcPosition>.None;
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<CdcPosition>>(option));

        var connector = CreateConnector();

        // Act
        var result = await connector.GetCurrentPositionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var position = (CdcPosition)result;
        position.ShouldBeOfType<DebeziumCdcPosition>();
        var debeziumPos = (DebeziumCdcPosition)position;
        debeziumPos.OffsetJson.ShouldContain("awaiting_events");
    }

    [Fact]
    public async Task GetCurrentPositionAsync_WhenPositionStoreThrows_ReturnsLeftError()
    {
        // Arrange
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Store unavailable"));

        var connector = CreateConnector();

        // Act
        var result = await connector.GetCurrentPositionAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentPositionAsync_WhenPositionStoreReturnsLeft_ReturnsAwaitingEventsPosition()
    {
        // Arrange
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<CdcPosition>>(EncinaError.New("Store error")));

        var connector = CreateConnector();

        // Act
        var result = await connector.GetCurrentPositionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var position = (CdcPosition)result;
        var debeziumPos = (DebeziumCdcPosition)position;
        debeziumPos.OffsetJson.ShouldContain("awaiting_events");
    }

    #endregion

    #region StreamChangesAsync Tests

    [Fact]
    public async Task StreamChangesAsync_WithValidEvent_YieldsChangeEvent()
    {
        // Arrange
        var option = Option<CdcPosition>.None;
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<CdcPosition>>(option));

        var connector = CreateConnector();
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"testdb","table":"Orders"}}""");
        await _channel.Writer.WriteAsync(json);
        _channel.Writer.Complete();

        // Act
        var events = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var evt in connector.StreamChangesAsync())
        {
            events.Add(evt);
        }

        // Assert
        events.Count.ShouldBe(1);
        events[0].IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task StreamChangesAsync_WithNoResumePosition_DoesNotSkipEvents()
    {
        // Arrange
        var option = Option<CdcPosition>.None;
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<CdcPosition>>(option));

        var connector = CreateConnector();
        var json1 = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"db","table":"T"}}""");
        var json2 = ParseJson("""{"op":"u","before":{"id":1},"after":{"id":1},"source":{"db":"db","table":"T"}}""");
        await _channel.Writer.WriteAsync(json1);
        await _channel.Writer.WriteAsync(json2);
        _channel.Writer.Complete();

        // Act
        var events = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var evt in connector.StreamChangesAsync())
        {
            events.Add(evt);
        }

        // Assert
        events.Count.ShouldBe(2);
    }

    [Fact]
    public async Task StreamChangesAsync_WhenPositionStoreThrowsDuringResume_StartsFromBeginning()
    {
        // Arrange
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Store unavailable"));

        var connector = CreateConnector();
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"db","table":"T"}}""");
        await _channel.Writer.WriteAsync(json);
        _channel.Writer.Complete();

        // Act
        var events = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var evt in connector.StreamChangesAsync())
        {
            events.Add(evt);
        }

        // Assert
        events.Count.ShouldBe(1);
    }

    [Fact]
    public async Task StreamChangesAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var option = Option<CdcPosition>.None;
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<CdcPosition>>(option));

        var connector = CreateConnector();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var events = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var evt in connector.StreamChangesAsync(cts.Token))
        {
            events.Add(evt);
        }

        // Assert
        events.Count.ShouldBe(0);
    }

    [Fact]
    public async Task StreamChangesAsync_WithSchemaChangeEvent_YieldsLeftError()
    {
        // Arrange
        var option = Option<CdcPosition>.None;
        _positionStore.GetPositionAsync("encina-cdc-debezium", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<CdcPosition>>(option));

        var connector = CreateConnector();
        // Schema change event (no "op" field)
        var json = ParseJson("""{"source":{"db":"testdb","table":"__debezium_schema"}}""");
        await _channel.Writer.WriteAsync(json);
        _channel.Writer.Complete();

        // Act
        var events = new List<Either<EncinaError, ChangeEvent>>();
        await foreach (var evt in connector.StreamChangesAsync())
        {
            events.Add(evt);
        }

        // Assert
        events.Count.ShouldBe(1);
        events[0].IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private static JsonElement ParseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    #endregion
}
