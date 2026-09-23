using System.Collections.Immutable;

using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Attributes;
using Encina.Messaging.Encryption.Model;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Pipeline;

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

    [EncryptedMessage]
    private sealed record EncryptedTestNotification : INotification
    {
        public string SensitiveData { get; init; } = string.Empty;
    }

    private sealed record TestRequestWithEncryptedNotifications : IRequest<TestResponse>, IHasNotifications
    {
        private readonly List<INotification> _notifications = [];

        public void AddNotification(INotification notification) => _notifications.Add(notification);

        public IEnumerable<INotification> GetNotifications() => _notifications;
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var logger = NullLogger<OutboxPostProcessor<TestRequest, TestResponse>>.Instance;
        var messageSerializer = Substitute.For<IMessageSerializer>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxPostProcessor<TestRequest, TestResponse>(null!, messageFactory, logger, messageSerializer));
    }

    [Fact]
    public void Constructor_WithNullMessageFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var logger = NullLogger<OutboxPostProcessor<TestRequest, TestResponse>>.Instance;
        var messageSerializer = Substitute.For<IMessageSerializer>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxPostProcessor<TestRequest, TestResponse>(store, null!, logger, messageSerializer));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var messageSerializer = Substitute.For<IMessageSerializer>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxPostProcessor<TestRequest, TestResponse>(store, messageFactory, null!, messageSerializer));
    }

    [Fact]
    public void Constructor_WithNullMessageSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var logger = NullLogger<OutboxPostProcessor<TestRequest, TestResponse>>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxPostProcessor<TestRequest, TestResponse>(store, messageFactory, logger, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var logger = NullLogger<OutboxPostProcessor<TestRequest, TestResponse>>.Instance;
        var messageSerializer = Substitute.For<IMessageSerializer>();

        // Act
        var processor = new OutboxPostProcessor<TestRequest, TestResponse>(store, messageFactory, logger, messageSerializer);

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

    #region Process - Encryption

    [Fact]
    public async Task Process_WithEncryptionRegisteredAndEncryptedMessage_StoresEncryptedContentAndRoundTrips()
    {
        // Arrange
        EncryptedMessageAttributeCache.ClearCache();
        try
        {
            var innerSerializer = new JsonMessageSerializer();
            var provider = new PassthroughMessageEncryptionProvider();
            var options = Options.Create(new MessageEncryptionOptions { Enabled = true });
            var encryptingSerializer = new EncryptingMessageSerializer(
                innerSerializer,
                provider,
                options,
                NullLogger<EncryptingMessageSerializer>.Instance);

            var (processor, store, messageFactory) = CreateProcessor<TestRequestWithEncryptedNotifications>(encryptingSerializer);
            var request = new TestRequestWithEncryptedNotifications();
            request.AddNotification(new EncryptedTestNotification { SensitiveData = "patient-diagnosis-42" });

            var context = CreateContext();
            var response = Either<EncinaError, TestResponse>.Right(new TestResponse());

            string? capturedContent = null;
            messageFactory.Create(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Do<string>(c => capturedContent = c),
                Arg.Any<DateTime>())
                .Returns(Substitute.For<IOutboxMessage>());

            // Act
            await processor.Process(request, context, response, CancellationToken.None);

            // Assert: the stored content is the encrypted envelope, not plain JSON
            capturedContent.ShouldNotBeNull();
            capturedContent.ShouldStartWith("ENC:v1:");
            capturedContent.ShouldNotContain("patient-diagnosis-42");

            // Assert: it round-trips back to the original value through the same serializer
            var roundTripped = encryptingSerializer.Deserialize<EncryptedTestNotification>(capturedContent!);
            roundTripped.ShouldNotBeNull();
            roundTripped!.SensitiveData.ShouldBe("patient-diagnosis-42");
        }
        finally
        {
            EncryptedMessageAttributeCache.ClearCache();
        }
    }

    /// <summary>
    /// Test double that "encrypts" by tagging the plaintext bytes verbatim as ciphertext,
    /// and "decrypts" by returning them unchanged. This is enough to verify that
    /// <see cref="OutboxPostProcessor{TRequest, TResponse}"/> serializes through
    /// <see cref="IMessageSerializer"/> (so the encryption decorator is invoked at all)
    /// without depending on a real cryptographic provider.
    /// </summary>
    private sealed class PassthroughMessageEncryptionProvider : IMessageEncryptionProvider
    {
        public ValueTask<Either<EncinaError, EncryptedPayload>> EncryptAsync(
            ReadOnlyMemory<byte> plaintext,
            MessageEncryptionContext context,
            CancellationToken cancellationToken = default)
        {
            var payload = new EncryptedPayload
            {
                Ciphertext = ImmutableArray.Create(plaintext.Span.ToArray()),
                KeyId = context.KeyId ?? "test-key",
                Algorithm = "test-passthrough",
                Nonce = ImmutableArray.Create<byte>(1, 2, 3),
                Tag = ImmutableArray.Create<byte>(4, 5, 6),
                Version = 1
            };

            return new ValueTask<Either<EncinaError, EncryptedPayload>>(Right<EncinaError, EncryptedPayload>(payload));
        }

        public ValueTask<Either<EncinaError, ImmutableArray<byte>>> DecryptAsync(
            EncryptedPayload payload,
            MessageEncryptionContext context,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<Either<EncinaError, ImmutableArray<byte>>>(
                Right<EncinaError, ImmutableArray<byte>>(payload.Ciphertext));
        }
    }

    #endregion

    #region Helper Methods

    private static (OutboxPostProcessor<TRequest, TestResponse> Processor, IOutboxStore Store, IOutboxMessageFactory Factory)
        CreateProcessor<TRequest>(IMessageSerializer? messageSerializer = null) where TRequest : IRequest<TestResponse>
    {
        var store = Substitute.For<IOutboxStore>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var logger = NullLogger<OutboxPostProcessor<TRequest, TestResponse>>.Instance;

        var processor = new OutboxPostProcessor<TRequest, TestResponse>(
            store,
            messageFactory,
            logger,
            messageSerializer ?? new JsonMessageSerializer());

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
