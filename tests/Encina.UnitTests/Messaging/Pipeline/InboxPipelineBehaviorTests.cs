using Encina.Messaging.Inbox;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Messaging.Pipeline;

/// <summary>
/// Unit tests for <see cref="InboxPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class InboxPipelineBehaviorTests
{
    private sealed record TestRequest : IRequest<TestResponse>
    {
        public int Value { get; init; }
    }

    private sealed record IdempotentTestRequest : IRequest<TestResponse>, IIdempotentRequest
    {
        public int Value { get; init; }
    }

    private sealed record TestResponse
    {
        public string Result { get; init; } = string.Empty;
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullOrchestrator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new InboxPipelineBehavior<TestRequest, TestResponse>(null!));
    }

    [Fact]
    public void Constructor_WithValidOrchestrator_Succeeds()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();

        // Act
        var behavior = new InboxPipelineBehavior<TestRequest, TestResponse>(orchestrator);

        // Assert
        behavior.ShouldNotBeNull();
    }

    #endregion

    #region Handle - Non-Idempotent Requests

    [Fact]
    public async Task Handle_WithNonIdempotentRequest_CallsNextStepDirectly()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var behavior = new InboxPipelineBehavior<TestRequest, TestResponse>(orchestrator);

        var request = new TestRequest { Value = 42 };
        var context = CreateContext();
        var expectedResponse = new TestResponse { Result = "Success" };
        var nextStepCalled = false;

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Either<EncinaError, TestResponse>.Right(expectedResponse));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Result.ShouldBe("Success"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Handle - Idempotent Requests

    [Fact]
    public async Task Handle_WithIdempotentRequest_WhenNullMessageId_ReturnsError()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var behavior = new InboxPipelineBehavior<IdempotentTestRequest, TestResponse>(orchestrator);

        var request = new IdempotentTestRequest { Value = 42 };
        var context = CreateContext(idempotencyKey: null);

        RequestHandlerCallback<TestResponse> nextStep = () =>
            throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Validation should fail due to null idempotencyKey
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithIdempotentRequest_WhenEmptyMessageId_ReturnsError()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var behavior = new InboxPipelineBehavior<IdempotentTestRequest, TestResponse>(orchestrator);

        var request = new IdempotentTestRequest { Value = 42 };
        var context = CreateContext(idempotencyKey: string.Empty);

        RequestHandlerCallback<TestResponse> nextStep = () =>
            throw new InvalidOperationException("Should not be called");

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Validation should fail due to empty idempotencyKey
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithIdempotentRequest_WhenNewMessage_ProcessesSuccessfully()
    {
        // Arrange
        var store = Substitute.For<IInboxStore>();
        store.GetMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IInboxMessage?)null);

        var orchestrator = CreateOrchestrator(store);
        var behavior = new InboxPipelineBehavior<IdempotentTestRequest, TestResponse>(orchestrator);

        var request = new IdempotentTestRequest { Value = 42 };
        var context = CreateContext("unique-message-id");
        var expectedResponse = new TestResponse { Result = "Processed" };
        var nextStepCalled = false;

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Either<EncinaError, TestResponse>.Right(expectedResponse));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithIdempotentRequest_WhenAlreadyProcessed_ReturnsCachedResponse()
    {
        // Arrange
        var existingMessage = Substitute.For<IInboxMessage>();
        existingMessage.IsProcessed.Returns(true);
        existingMessage.Response.Returns("{\"isSuccess\":true,\"value\":{\"result\":\"Cached\"}}");

        var store = Substitute.For<IInboxStore>();
        store.GetMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingMessage);

        var orchestrator = CreateOrchestrator(store);
        var behavior = new InboxPipelineBehavior<IdempotentTestRequest, TestResponse>(orchestrator);

        var request = new IdempotentTestRequest { Value = 42 };
        var context = CreateContext("already-processed-id");
        var nextStepCalled = false;

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Either<EncinaError, TestResponse>.Right(new TestResponse()));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - should skip because already processed
        nextStepCalled.ShouldBeFalse();
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private static InboxOrchestrator CreateOrchestrator(IInboxStore? store = null)
    {
        var inboxStore = store ?? Substitute.For<IInboxStore>();
        var options = new InboxOptions();
        var logger = NullLogger<InboxOrchestrator>.Instance;
        var messageFactory = Substitute.For<IInboxMessageFactory>();
        messageFactory.Create(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<InboxMetadata?>())
            .Returns(_ => Substitute.For<IInboxMessage>());

        return new InboxOrchestrator(inboxStore, options, logger, messageFactory);
    }

    private static IRequestContext CreateContext(
        string? idempotencyKey = "test-key",
        string correlationId = "test-correlation",
        string userId = "user-123",
        string tenantId = "tenant-456",
        DateTime? timestamp = null)
    {
        var context = Substitute.For<IRequestContext>();
        context.IdempotencyKey.Returns(idempotencyKey);
        context.CorrelationId.Returns(correlationId);
        context.UserId.Returns(userId);
        context.TenantId.Returns(tenantId);
        context.Timestamp.Returns(timestamp ?? DateTime.UtcNow);
        return context;
    }

    #endregion
}
