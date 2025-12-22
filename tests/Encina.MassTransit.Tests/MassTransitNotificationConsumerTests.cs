using Encina.MassTransit;
using LanguageExt;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.MassTransit.Tests;

public class MassTransitNotificationConsumerTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<MassTransitNotificationConsumer<TestNotification>> _logger;
    private readonly IOptions<EncinaMassTransitOptions> _options;
    private readonly MassTransitNotificationConsumer<TestNotification> _consumer;
    private readonly ConsumeContext<TestNotification> _context;

    public MassTransitNotificationConsumerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<MassTransitNotificationConsumer<TestNotification>>>();
        _options = Options.Create(new EncinaMassTransitOptions());
        _context = Substitute.For<ConsumeContext<TestNotification>>();
        _consumer = new MassTransitNotificationConsumer<TestNotification>(_mediator, _logger, _options);
    }

    [Fact]
    public async Task Consume_WithSuccessfulNotification_LogsSuccess()
    {
        // Arrange
        var notification = new TestNotification("test-data");
        _context.Message.Returns(notification);
        _context.MessageId.Returns(Guid.NewGuid());
        _mediator.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, Unit>(Unit.Default));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<TestNotification>(n => n.Data == "test-data"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithFailedNotification_ThrowsException()
    {
        // Arrange
        var notification = new TestNotification("test-data");
        var error = MediatorError.New("Test error message");
        _context.Message.Returns(notification);
        _context.MessageId.Returns(Guid.NewGuid());
        _mediator.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Left<MediatorError, Unit>(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MediatorConsumerException>(
            () => _consumer.Consume(_context));
        exception.MediatorError.Message.Should().Be("Test error message");
    }

    [Fact]
    public async Task Consume_WithFailedNotification_WhenThrowOnErrorDisabled_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new EncinaMassTransitOptions { ThrowOnMediatorError = false });
        var consumer = new MassTransitNotificationConsumer<TestNotification>(_mediator, _logger, options);
        var notification = new TestNotification("test-data");
        var error = MediatorError.New("Test error message");
        _context.Message.Returns(notification);
        _context.MessageId.Returns(Guid.NewGuid());
        _mediator.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Left<MediatorError, Unit>(error));

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
        _mediator.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, Unit>(Unit.Default));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Any<TestNotification>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
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
            new MassTransitNotificationConsumer<TestNotification>(_mediator, null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MassTransitNotificationConsumer<TestNotification>(_mediator, _logger, null!));
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
