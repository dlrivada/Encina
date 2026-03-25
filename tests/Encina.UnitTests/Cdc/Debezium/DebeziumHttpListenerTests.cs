using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc.Debezium;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumHttpListener"/> constructor validation.
/// ExecuteAsync tests require real HttpListener and are integration tests.
/// </summary>
public sealed class DebeziumHttpListenerTests
{
    private readonly DebeziumCdcOptions _options = new();
    private readonly Channel<JsonElement> _channel = Channel.CreateUnbounded<JsonElement>();
    private readonly ILogger<DebeziumHttpListener> _logger = Substitute.For<ILogger<DebeziumHttpListener>>();

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DebeziumHttpListener(null!, _channel, _logger));
    }

    [Fact]
    public void Constructor_NullChannel_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DebeziumHttpListener(_options, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DebeziumHttpListener(_options, _channel, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        var listener = new DebeziumHttpListener(_options, _channel, _logger);
        listener.ShouldNotBeNull();
    }
}
