using Encina.MassTransit;
using LanguageExt;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.MassTransit.Tests;

public class MassTransitRequestConsumerTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<MassTransitRequestConsumer<TestRequest, TestResponse>> _logger;
    private readonly IOptions<EncinaMassTransitOptions> _options;
    private readonly MassTransitRequestConsumer<TestRequest, TestResponse> _consumer;
    private readonly ConsumeContext<TestRequest> _context;

    public MassTransitRequestConsumerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<MassTransitRequestConsumer<TestRequest, TestResponse>>>();
        _options = Options.Create(new EncinaMassTransitOptions());
        _context = Substitute.For<ConsumeContext<TestRequest>>();
        _consumer = new MassTransitRequestConsumer<TestRequest, TestResponse>(_mediator, _logger, _options);
    }

    [Fact]
    public async Task Consume_WithSuccessfulRequest_LogsSuccess()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _context.Message.Returns(request);
        _context.MessageId.Returns(Guid.NewGuid());
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, TestResponse>(new TestResponse("success")));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<TestRequest>(r => r.Data == "test-data"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WithFailedRequest_ThrowsException()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = MediatorError.New("Test error message");
        _context.Message.Returns(request);
        _context.MessageId.Returns(Guid.NewGuid());
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<MediatorError, TestResponse>(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MediatorConsumerException>(
            () => _consumer.Consume(_context));
        exception.MediatorError.Message.Should().Be("Test error message");
    }

    [Fact]
    public async Task Consume_WithFailedRequest_WhenThrowOnErrorDisabled_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new EncinaMassTransitOptions { ThrowOnMediatorError = false });
        var consumer = new MassTransitRequestConsumer<TestRequest, TestResponse>(_mediator, _logger, options);
        var request = new TestRequest("test-data");
        var error = MediatorError.New("Test error message");
        _context.Message.Returns(request);
        _context.MessageId.Returns(Guid.NewGuid());
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<MediatorError, TestResponse>(error));

        // Act & Assert (should not throw)
        await consumer.Consume(_context);
    }

    [Fact]
    public async Task Consume_PassesCancellationToken()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var cts = new CancellationTokenSource();
        _context.Message.Returns(request);
        _context.MessageId.Returns(Guid.NewGuid());
        _context.CancellationToken.Returns(cts.Token);
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, TestResponse>(new TestResponse("success")));

        // Act
        await _consumer.Consume(_context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<TestRequest>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MassTransitRequestConsumer<TestRequest, TestResponse>(null!, _logger, _options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MassTransitRequestConsumer<TestRequest, TestResponse>(_mediator, null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MassTransitRequestConsumer<TestRequest, TestResponse>(_mediator, _logger, null!));
    }

    [Fact]
    public async Task Consume_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consumer.Consume(null!));
    }

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
