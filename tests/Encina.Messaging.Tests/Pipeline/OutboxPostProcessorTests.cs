using Encina.Messaging.Outbox;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.Messaging.Tests.Pipeline;

/// <summary>
/// Unit tests for <see cref="OutboxPostProcessor{TRequest, TResponse}"/>.
/// </summary>
public sealed class OutboxPostProcessorTests
{
    private sealed record TestRequest : IRequest<TestResponse>
    {
        public int Value { get; init; }
    }

    private sealed record TestRequestWithNotifications : IRequest<TestResponse>, IHasNotifications
    {
        public int Value { get; init; }
        private readonly List<INotification> _notifications = [];

        public void AddNotification(INotification notification) => _notifications.Add(notification);

        public IEnumerable<INotification> GetNotifications() => _notifications;
    }

    private sealed record TestResponse
    {
        public string Result { get; init; } = string.Empty;
    }

    private sealed record TestNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var logger = NullLogger<OutboxPostProcessor<TestRequest, TestResponse>>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxPostProcessor<TestRequest, TestResponse>(null!, messageFactory, logger));
    }

    [Fact]
    public void Constructor_WithNullMessageFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var logger = NullLogger<OutboxPostProcessor<TestRequest, TestResponse>>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxPostProcessor<TestRequest, TestResponse>(store, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxPostProcessor<TestRequest, TestResponse>(store, messageFactory, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var logger = NullLogger<OutboxPostProcessor<TestRequest, TestResponse>>.Instance;

        // Act
        var processor = new OutboxPostProcessor<TestRequest, TestResponse>(store, messageFactory, logger);

        // Assert
        processor.ShouldNotBeNull();
    }

    #endregion

    #region Process - Non-Notification Requests

    [Fact]
    public async Task Process_WithNonNotificationRequest_DoesNothing()
    {
        // Arrange
        var (processor, store, messageFactory) = CreateProcessor<TestRequest>();
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();
        var response = Either<EncinaError, TestResponse>.Right(new TestResponse());

        // Act
        await processor.Process(request, context, response, CancellationToken.None);

        // Assert
        await store.DidNotReceive().AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Process - Empty Notifications

    [Fact]
    public async Task Process_WithEmptyNotifications_DoesNothing()
    {
        // Arrange
        var (processor, store, messageFactory) = CreateProcessor<TestRequestWithNotifications>();
        var request = new TestRequestWithNotifications { Value = 42 };
        var context = CreateContext();
        var response = Either<EncinaError, TestResponse>.Right(new TestResponse());

        // Act
        await processor.Process(request, context, response, CancellationToken.None);

        // Assert
        await store.DidNotReceive().AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Process - Success Response with Notifications

    [Fact]
    public async Task Process_WithSuccessAndNotifications_StoresNotifications()
    {
        // Arrange
        var (processor, store, messageFactory) = CreateProcessor<TestRequestWithNotifications>();
        var request = new TestRequestWithNotifications { Value = 42 };
        request.AddNotification(new TestNotification { Message = "Notification 1" });
        request.AddNotification(new TestNotification { Message = "Notification 2" });

        var context = CreateContext();
        var response = Either<EncinaError, TestResponse>.Right(new TestResponse());

        var mockMessage = Substitute.For<IOutboxMessage>();
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>())
            .Returns(mockMessage);

        // Act
        await processor.Process(request, context, response, CancellationToken.None);

        // Assert
        await store.Received(2).AddAsync(mockMessage, Arg.Any<CancellationToken>());
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_WithSuccessAndNotifications_CreatesCorrectMessageContent()
    {
        // Arrange
        var (processor, store, messageFactory) = CreateProcessor<TestRequestWithNotifications>();
        var request = new TestRequestWithNotifications { Value = 42 };
        request.AddNotification(new TestNotification { Message = "Test notification message" });

        var context = CreateContext();
        var response = Either<EncinaError, TestResponse>.Right(new TestResponse());

        string? capturedContent = null;
        string? capturedType = null;

        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Do<string>(t => capturedType = t),
            Arg.Do<string>(c => capturedContent = c),
            Arg.Any<DateTime>())
            .Returns(Substitute.For<IOutboxMessage>());

        // Act
        await processor.Process(request, context, response, CancellationToken.None);

        // Assert
        capturedType.ShouldNotBeNull();
        capturedType.ShouldContain(nameof(TestNotification));
        capturedContent.ShouldNotBeNull();
        capturedContent.ShouldContain("Test notification message");
    }

    #endregion

    #region Process - Error Response

    [Fact]
    public async Task Process_WithErrorResponse_DoesNotStoreNotifications()
    {
        // Arrange
        var (processor, store, messageFactory) = CreateProcessor<TestRequestWithNotifications>();
        var request = new TestRequestWithNotifications { Value = 42 };
        request.AddNotification(new TestNotification { Message = "Should not be stored" });

        var context = CreateContext();
        var error = EncinaError.New("Something went wrong");
        var response = Either<EncinaError, TestResponse>.Left(error);

        // Act
        await processor.Process(request, context, response, CancellationToken.None);

        // Assert
        await store.DidNotReceive().AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Process - Multiple Notifications

    [Fact]
    public async Task Process_WithMultipleNotifications_StoresEachOne()
    {
        // Arrange
        var (processor, store, messageFactory) = CreateProcessor<TestRequestWithNotifications>();
        var request = new TestRequestWithNotifications { Value = 42 };
        request.AddNotification(new TestNotification { Message = "First" });
        request.AddNotification(new TestNotification { Message = "Second" });
        request.AddNotification(new TestNotification { Message = "Third" });

        var context = CreateContext();
        var response = Either<EncinaError, TestResponse>.Right(new TestResponse());

        var messages = new List<IOutboxMessage>();
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>())
            .Returns(_ =>
            {
                var msg = Substitute.For<IOutboxMessage>();
                messages.Add(msg);
                return msg;
            });

        // Act
        await processor.Process(request, context, response, CancellationToken.None);

        // Assert
        messages.Count.ShouldBe(3);
        await store.Received(3).AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private static (OutboxPostProcessor<TRequest, TestResponse> Processor, IOutboxStore Store, IOutboxMessageFactory Factory)
        CreateProcessor<TRequest>() where TRequest : IRequest<TestResponse>
    {
        var store = Substitute.For<IOutboxStore>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var logger = NullLogger<OutboxPostProcessor<TRequest, TestResponse>>.Instance;

        var processor = new OutboxPostProcessor<TRequest, TestResponse>(store, messageFactory, logger);

        return (processor, store, messageFactory);
    }

    private static IRequestContext CreateContext(string correlationId = "test-correlation")
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns(correlationId);
        return context;
    }

    #endregion
}
