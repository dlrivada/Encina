using System.Diagnostics;
using Encina;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Behaviors;
using FluentAssertions;
using LanguageExt;
using NSubstitute;
using Xunit;

namespace Encina.OpenTelemetry.Tests.Contracts;

/// <summary>
/// Contract tests for <see cref="MessagingEnricherPipelineBehavior{TRequest, TResponse}"/>
/// to verify correct implementation of <see cref="IPipelineBehavior{TRequest, TResponse}"/> contract.
/// </summary>
public sealed class MessagingEnricherBehaviorContractTests : IDisposable
{
    private sealed record TestRequest(string Data) : IRequest<string>;

    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;

    public MessagingEnricherBehaviorContractTests()
    {
        _activitySource = new ActivitySource("Test");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Test",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
        _activitySource?.Dispose();
    }

    [Fact]
    public async Task Handle_ShouldCallNextStepExactlyOnce()
    {
        // Arrange
        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();
        var nextStepCalled = 0;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled++;
            return new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().Be(1, "nextStep should be called exactly once");
    }

    [Fact]
    public async Task Handle_ShouldReturnNextStepResult()
    {
        // Arrange
        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();
        context.Metadata.Returns(new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>);

        RequestHandlerCallback<string> nextStep = () => new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("expected"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue("behavior should return successful result from nextStep");
        result.IfRight(r => r.Should().Be("expected"));
    }

    [Fact]
    public async Task Handle_WhenNextStepReturnsError_ShouldReturnError()
    {
        // Arrange
        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();
        context.Metadata.Returns(new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>);
        var error = MediatorError.New("Test error message");

        RequestHandlerCallback<string> nextStep = () => new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Left(error));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue("behavior should preserve error results");
        result.IfLeft(e => e.Message.Should().Be("Test error message"));
    }

    [Fact]
    public async Task Handle_WhenNoActivityCurrent_ShouldNotThrow()
    {
        // Arrange
        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));

        // Ensure no activity is running
        Activity.Current = null;

        // Act
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("behavior should handle null Activity.Current gracefully");
    }

    [Fact]
    public async Task Handle_WithOutboxMessage_ShouldEnrichActivityAndCallNextStep()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestActivity");

        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        var outboxMessage = Substitute.For<IOutboxMessage>();
        outboxMessage.Id.Returns(Guid.NewGuid());
        outboxMessage.NotificationType.Returns("TestNotification");
        outboxMessage.IsProcessed.Returns(false);

        var metadata = new Dictionary<string, object?> { { "OutboxMessage", outboxMessage } };
        context.Metadata.Returns(metadata as IReadOnlyDictionary<string, object?>);

        var nextStepCalled = false;
        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue("nextStep should be called even when enriching");
        activity!.Tags.Should().Contain(tag => tag.Key == "messaging.system" && tag.Value == "encina.outbox",
            "enricher should add messaging.system tag");
    }

    [Fact]
    public async Task Handle_WithInboxMessage_ShouldEnrichActivityAndCallNextStep()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestActivity");

        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        var inboxMessage = Substitute.For<IInboxMessage>();
        inboxMessage.MessageId.Returns(Guid.NewGuid().ToString());
        inboxMessage.RequestType.Returns("TestNotification");
        inboxMessage.IsProcessed.Returns(false);

        var metadata = new Dictionary<string, object?> { { "InboxMessage", inboxMessage } };
        context.Metadata.Returns(metadata as IReadOnlyDictionary<string, object?>);

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        activity!.Tags.Should().Contain(tag => tag.Key == "messaging.system" && tag.Value == "encina.inbox",
            "enricher should add messaging.system tag");
    }

    [Fact]
    public async Task Handle_WithSagaState_ShouldEnrichActivityAndCallNextStep()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestActivity");

        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        var sagaState = Substitute.For<ISagaState>();
        sagaState.SagaId.Returns(Guid.NewGuid());
        sagaState.SagaType.Returns("TestSaga");
        sagaState.Status.Returns("Running");

        var metadata = new Dictionary<string, object?> { { "SagaState", sagaState } };
        context.Metadata.Returns(metadata as IReadOnlyDictionary<string, object?>);

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        activity!.Tags.Should().Contain(tag => tag.Key == "saga.type" && tag.Value == "TestSaga",
            "enricher should add saga.type tag");
    }

    [Fact]
    public async Task Handle_WithScheduledMessage_ShouldEnrichActivityAndCallNextStep()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestActivity");

        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        var scheduledMessage = Substitute.For<IScheduledMessage>();
        scheduledMessage.Id.Returns(Guid.NewGuid());
        scheduledMessage.RequestType.Returns("TestCommand");
        scheduledMessage.IsProcessed.Returns(false);
        scheduledMessage.IsRecurring.Returns(false);

        var metadata = new Dictionary<string, object?> { { "ScheduledMessage", scheduledMessage } };
        context.Metadata.Returns(metadata as IReadOnlyDictionary<string, object?>);

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        activity!.Tags.Should().Contain(tag => tag.Key == "messaging.system" && tag.Value == "encina.scheduling",
            "enricher should add messaging.system tag");
    }

    [Fact]
    public async Task Handle_WithMultipleMessagingPatterns_ShouldEnrichWithAll()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestActivity");

        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        var outboxMessage = Substitute.For<IOutboxMessage>();
        outboxMessage.Id.Returns(Guid.NewGuid());
        outboxMessage.IsProcessed.Returns(false);

        var sagaState = Substitute.For<ISagaState>();
        sagaState.SagaId.Returns(Guid.NewGuid());

        var metadata = new Dictionary<string, object?>
        {
            { "OutboxMessage", outboxMessage },
            { "SagaState", sagaState }
        };
        context.Metadata.Returns(metadata as IReadOnlyDictionary<string, object?>);

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        activity!.Tags.Should().Contain(tag => tag.Key == "messaging.system" && tag.Value == "encina.outbox",
            "enricher should add messaging.system tag for outbox");
        activity.Tags.Should().Contain(tag => tag.Key.StartsWith("saga", StringComparison.Ordinal),
            "enricher should add saga.* tags");
    }

    [Fact]
    public async Task Handle_WithEmptyMetadata_ShouldNotEnrichButStillCallNextStep()
    {
        // Arrange
        using var activity = _activitySource.StartActivity("TestActivity");

        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();
        context.Metadata.Returns(new Dictionary<string, object?>() as IReadOnlyDictionary<string, object?>);

        var nextStepCalled = false;
        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<MediatorError, string>>(Either<MediatorError, string>.Right("result"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue("nextStep should always be called");
        activity!.Tags.Should().NotContain(tag => tag.Key == "Encina.messaging_enabled",
            "should not add messaging_enabled tag when no messaging context found");
    }

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<string> nextStep = () =>
            throw new OperationCanceledException(cts.Token);

        // Act
        var act = async () => await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>("behavior should propagate cancellation");
    }
}
