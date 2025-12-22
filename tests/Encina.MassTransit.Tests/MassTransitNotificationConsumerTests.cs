using Encina.MassTransit;
using LanguageExt;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.MassTransit.Tests;

public class MassTransitNotificationConsumerTests
{
    private readonly IEncina _Encina;
    private readonly ILogger<MassTransitNotificationConsumer<TestNotification>> _logger;
    private readonly IOptions<EncinaMassTransitOptions> _options;
    private readonly MassTransitNotificationConsumer<TestNotification> _consumer;
    private readonly ConsumeContext<TestNotification> _context;

    public MassTransitNotificationConsumerTests()
    {
        _Encina = Substitute.For<IEncina>();
        _logger = Substitute.For<ILogger<MassTransitNotificationConsumer<TestNotification>>>();
        _options = Options.Create(new EncinaMassTransitOptions());
        _context = Substitute.For<ConsumeContext<TestNotification>>();
        _consumer = new MassTransitNotificationConsumer<TestNotification>(_Encina, _logger, _options);
    }

    [Fact]
    public async Task Consume_WithSuccessfulNotification_LogsSuccess()
    {
        // Arrange
        var notification = new TestNotification("test-data");
        _context.Message.Returns(notification);
        _context.MessageId.Returns(Guid.NewGuid());
        _Encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _Encina.Received(1).Publish(
            Arg.Is<TestNotification>(n => n.Data == "test-data"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithFailedNotification_ThrowsException()
    {
        // Arrange
        var notification = new TestNotification("test-data");
        var error = EncinaError.New("Test error message");
        _context.Message.Returns(notification);
        _context.MessageId.Returns(Guid.NewGuid());
        _Encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EncinaConsumerException>(
            () => _consumer.Consume(_context));
        exception.EncinaError.Message.Should().Be("Test error message");
    }

    [Fact]
    public async Task Consume_WithFailedNotification_WhenThrowOnErrorDisabled_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new EncinaMassTransitOptions { ThrowOnEncinaError = false });
        var consumer = new MassTransitNotificationConsumer<TestNotification>(_Encina, _logger, options);
        var notification = new TestNotification("test-data");
        var error = EncinaError.New("Test error message");
        _context.Message.Returns(notification);
        _context.MessageId.Returns(Guid.NewGuid());
        _Encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        // Act & Assert (should not throw)
        await consumer.Consume(_context);
    }

    [Fact]
    public async Task Consume_PassesCancellationToken()
    {
        // Arrange
        var notification = new TestNotification("test-data");
        var cts = new CancellationTokenSource();
        _context.Message.Returns(notification);
        _context.MessageId.Returns(Guid.NewGuid());
        _context.CancellationToken.Returns(cts.Token);
        _Encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _Encina.Received(1).Publish(
            Arg.Any<TestNotification>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public void Constructor_WithNullEncina_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MassTransitNotificationConsumer<TestNotification>(null!, _logger, _options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MassTransitNotificationConsumer<TestNotification>(_Encina, null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MassTransitNotificationConsumer<TestNotification>(_Encina, _logger, null!));
    }

    [Fact]
    public async Task Consume_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consumer.Consume(null!));
    }

    // Test types
    public record TestNotification(string Data) : INotification;
}
