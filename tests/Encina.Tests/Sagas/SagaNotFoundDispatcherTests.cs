using Encina.Messaging.Sagas;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Sagas;

public sealed class SagaNotFoundDispatcherTests
{
    private readonly Guid _sagaId = Guid.NewGuid();
    private const string SagaType = "OrderSaga";

    [Fact]
    public async Task DispatchAsync_WithNoHandler_ReturnsSuccess()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);
        var context = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));

        // Act
        var result = await dispatcher.DispatchAsync(new TestMessage(), context);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task DispatchAsync_WithHandler_InvokesHandler()
    {
        // Arrange
        var handler = new TestHandler();
        var services = new ServiceCollection()
            .AddSingleton<IHandleSagaNotFound<TestMessage>>(handler)
            .BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);
        var message = new TestMessage { Data = "test-data" };
        var context = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));

        // Act
        var result = await dispatcher.DispatchAsync(message, context);

        // Assert
        result.ShouldBeSuccess();
        handler.HandleCalled.ShouldBeTrue();
        handler.ReceivedMessage.ShouldBe(message);
        handler.ReceivedContext.ShouldBe(context);
    }

    [Fact]
    public async Task DispatchAsync_WithHandler_PassesCancellationToken()
    {
        // Arrange
        var handler = new TestHandler();
        var services = new ServiceCollection()
            .AddSingleton<IHandleSagaNotFound<TestMessage>>(handler)
            .BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);
        var context = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await dispatcher.DispatchAsync(new TestMessage(), context, token);

        // Assert
        handler.ReceivedCancellationToken.ShouldBe(token);
    }

    [Fact]
    public async Task DispatchAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);
        var context = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync<TestMessage>(null!, context));
    }

    [Fact]
    public async Task DispatchAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync(new TestMessage(), null!));
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrows_ReturnsError()
    {
        // Arrange
        var handler = new ThrowingHandler(new InvalidOperationException("Handler error"));
        var services = new ServiceCollection()
            .AddSingleton<IHandleSagaNotFound<TestMessage>>(handler)
            .BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);
        var context = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));

        // Act
        var result = await dispatcher.DispatchAsync(new TestMessage(), context);

        // Assert
        var error = result.ShouldBeError();
        error.GetCode().Match(
            Some: code => code.ShouldBe(SagaErrorCodes.HandlerFailed),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("failed");
    }

    [Fact]
    public async Task DispatchAsync_WhenCancelled_ReturnsError()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var handler = new CancellingHandler(cts);
        var services = new ServiceCollection()
            .AddSingleton<IHandleSagaNotFound<TestMessage>>(handler)
            .BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);
        var context = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));

        // Act
        var result = await dispatcher.DispatchAsync(new TestMessage(), context, cts.Token);

        // Assert
        var error = result.ShouldBeError();
        error.GetCode().Match(
            Some: code => code.ShouldBe(SagaErrorCodes.HandlerCancelled),
            None: () => Assert.Fail("Expected error code"));
        error.Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleMessageTypes_InvokesCorrectHandler()
    {
        // Arrange
        var handler1 = new TestHandler();
        var handler2 = new AnotherHandler();
        var services = new ServiceCollection()
            .AddSingleton<IHandleSagaNotFound<TestMessage>>(handler1)
            .AddSingleton<IHandleSagaNotFound<AnotherMessage>>(handler2)
            .BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);

        var context1 = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));
        var context2 = new SagaNotFoundContext(_sagaId, SagaType, typeof(AnotherMessage));

        // Act
        await dispatcher.DispatchAsync(new TestMessage(), context1);
        await dispatcher.DispatchAsync(new AnotherMessage(), context2);

        // Assert
        handler1.HandleCalled.ShouldBeTrue();
        handler2.HandleCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerSetsIgnore_ReturnsSucess()
    {
        // Arrange
        var handler = new IgnoringHandler();
        var services = new ServiceCollection()
            .AddSingleton<IHandleSagaNotFound<TestMessage>>(handler)
            .BuildServiceProvider();
        var dispatcher = CreateDispatcher(services);
        var context = new SagaNotFoundContext(_sagaId, SagaType, typeof(TestMessage));

        // Act
        var result = await dispatcher.DispatchAsync(new TestMessage(), context);

        // Assert
        result.ShouldBeSuccess();
        context.WasIgnored.ShouldBeTrue();
        context.Action.ShouldBe(SagaNotFoundAction.Ignored);
    }

    private static SagaNotFoundDispatcher CreateDispatcher(IServiceProvider services)
    {
        var logger = NullLogger<SagaNotFoundDispatcher>.Instance;
        return new SagaNotFoundDispatcher(services, logger);
    }

    private sealed class TestMessage
    {
        public string? Data { get; init; }
    }

    private sealed class AnotherMessage;

    private sealed class TestHandler : IHandleSagaNotFound<TestMessage>
    {
        public bool HandleCalled { get; private set; }
        public TestMessage? ReceivedMessage { get; private set; }
        public SagaNotFoundContext? ReceivedContext { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task HandleAsync(TestMessage message, SagaNotFoundContext context, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            ReceivedMessage = message;
            ReceivedContext = context;
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class AnotherHandler : IHandleSagaNotFound<AnotherMessage>
    {
        public bool HandleCalled { get; private set; }

        public Task HandleAsync(AnotherMessage message, SagaNotFoundContext context, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler(Exception exception) : IHandleSagaNotFound<TestMessage>
    {
        public Task HandleAsync(TestMessage message, SagaNotFoundContext context, CancellationToken cancellationToken)
        {
            throw exception;
        }
    }

    private sealed class CancellingHandler(CancellationTokenSource cts) : IHandleSagaNotFound<TestMessage>
    {
        public Task HandleAsync(TestMessage message, SagaNotFoundContext context, CancellationToken cancellationToken)
        {
            cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class IgnoringHandler : IHandleSagaNotFound<TestMessage>
    {
        public Task HandleAsync(TestMessage message, SagaNotFoundContext context, CancellationToken cancellationToken)
        {
            context.Ignore();
            return Task.CompletedTask;
        }
    }
}
