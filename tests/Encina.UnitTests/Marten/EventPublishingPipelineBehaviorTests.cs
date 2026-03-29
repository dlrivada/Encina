using Encina.Marten;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten;

public class EventPublishingPipelineBehaviorTests
{
    private readonly IDocumentSession _session;
    private readonly IEncina _encina;
    private readonly ILogger<EventPublishingPipelineBehavior<TestCommand, TestResponse>> _logger;
    private readonly IOptions<EncinaMartenOptions> _options;
    private readonly IRequestContext _requestContext;

    public EventPublishingPipelineBehaviorTests()
    {
        _session = Substitute.For<IDocumentSession>();
        _encina = Substitute.For<IEncina>();
        _logger = NullLogger<EventPublishingPipelineBehavior<TestCommand, TestResponse>>.Instance;
        _options = Options.Create(new EncinaMartenOptions());
        _requestContext = Substitute.For<IRequestContext>();
    }

    private EventPublishingPipelineBehavior<TestCommand, TestResponse> CreateSut()
    {
        return new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, _logger, _options);
    }

    // Constructor null guard tests

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            null!, _encina, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullEncina_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, null!, _logger, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("encina");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, null!, _options);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, _logger, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    // Handle tests

    [Fact]
    public async Task Handle_CommandFails_ReturnLeftWithoutPublishing()
    {
        // Arrange
        var sut = CreateSut();
        var error = EncinaErrors.Create("test", "command failed");
        RequestHandlerCallback<TestResponse> next = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Left<EncinaError, TestResponse>(error));

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(
            Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AutoPublishDisabled_DoesNotPublish()
    {
        // Arrange
        var options = Options.Create(new EncinaMartenOptions { AutoPublishDomainEvents = false });
        var sut = new EventPublishingPipelineBehavior<TestCommand, TestResponse>(
            _session, _encina, _logger, options);

        var response = new TestResponse();
        RequestHandlerCallback<TestResponse> next = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Right<EncinaError, TestResponse>(response));

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(
            Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPendingEvents_ReturnsResultWithoutPublishing()
    {
        // Arrange
        var sut = CreateSut();
        var response = new TestResponse();
        RequestHandlerCallback<TestResponse> next = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Right<EncinaError, TestResponse>(response));

        // PendingChanges.Streams() returns empty
        _session.PendingChanges.Streams().Returns([]);

        // Act
        var result = await sut.Handle(new TestCommand(), _requestContext, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(
            Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    // Test types

    public sealed record TestCommand : ICommand<TestResponse>;
    public sealed record TestResponse;
    public sealed record TestNotification(string Message) : INotification;
}
