using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for UseCaseHandlerExtensions and IntegrationEvent base class.
/// </summary>
public sealed class UseCaseHandlerTests
{
    #region Test Types

    public sealed record CreateOrderCommand(string CustomerName, decimal Total);
    public sealed record CreateOrderResult(Guid OrderId);

    public sealed class CreateOrderHandler : IUseCaseHandler<CreateOrderCommand, CreateOrderResult>
    {
        public Task<CreateOrderResult> HandleAsync(
            CreateOrderCommand input,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CreateOrderResult(Guid.NewGuid()));
        }
    }

    public sealed record SendNotificationCommand(string Message);

    public sealed class SendNotificationHandler : IUseCaseHandler<SendNotificationCommand>
    {
        public Task HandleAsync(
            SendNotificationCommand input,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public sealed record OrderCreatedIntegrationEvent : IntegrationEvent
    {
        public Guid OrderId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
    }

    public sealed record PaymentProcessedEvent : IntegrationEvent
    {
        public override int EventVersion => 2;
        public decimal Amount { get; init; }
    }

    #endregion

    #region UseCaseHandlerExtensions Tests

    [Fact]
    public void AddUseCaseHandlersFromAssembly_RegistersHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(UseCaseHandlerTests).Assembly;

        // Act
        services.AddUseCaseHandlersFromAssembly(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var handler1 = provider.GetService<IUseCaseHandler<CreateOrderCommand, CreateOrderResult>>();
        handler1.ShouldNotBeNull();
        handler1.ShouldBeOfType<CreateOrderHandler>();

        var handler2 = provider.GetService<IUseCaseHandler<SendNotificationCommand>>();
        handler2.ShouldNotBeNull();
        handler2.ShouldBeOfType<SendNotificationHandler>();
    }

    [Fact]
    public void AddUseCaseHandlersFromAssembly_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = typeof(UseCaseHandlerTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddUseCaseHandlersFromAssembly(assembly));
    }

    [Fact]
    public void AddUseCaseHandlersFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddUseCaseHandlersFromAssembly(null!));
    }

    [Fact]
    public void AddUseCaseHandlersFromAssembly_WithLifetime_UsesSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(UseCaseHandlerTests).Assembly;

        // Act
        services.AddUseCaseHandlersFromAssembly(assembly, ServiceLifetime.Singleton);

        // Assert
        // Find the descriptor for our handler
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IUseCaseHandler<CreateOrderCommand, CreateOrderResult>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddUseCaseHandler_RegistersSpecificHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUseCaseHandler<CreateOrderHandler>();
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IUseCaseHandler<CreateOrderCommand, CreateOrderResult>>();
        handler.ShouldNotBeNull();
    }

    [Fact]
    public void AddUseCaseHandler_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddUseCaseHandler<CreateOrderHandler>());
    }

    [Fact]
    public void AddUseCaseHandler_WithLifetime_UsesSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUseCaseHandler<CreateOrderHandler>(ServiceLifetime.Singleton);

        // Assert
        var descriptor = services.First();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public async Task UseCaseHandler_WithResult_ReturnsResult()
    {
        // Arrange
        var handler = new CreateOrderHandler();
        var command = new CreateOrderCommand("John", 100m);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.OrderId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task UseCaseHandler_WithoutResult_Completes()
    {
        // Arrange
        var handler = new SendNotificationHandler();
        var command = new SendNotificationCommand("Hello");

        // Act
        var ex = await Record.ExceptionAsync(() => handler.HandleAsync(command));

        // Assert
        ex.ShouldBeNull();
    }

    #endregion

    #region IntegrationEvent Tests

    [Fact]
    public void IntegrationEvent_DefaultValues_AreCorrect()
    {
        // Arrange - capture a reference time to make time assertions stable
        var utcBefore = DateTime.UtcNow;

        // Act
        var @event = new OrderCreatedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "John"
        };

        var utcAfter = DateTime.UtcNow;

        // Assert
        @event.EventId.ShouldNotBe(Guid.Empty);
        @event.OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(utcBefore);
        @event.OccurredAtUtc.ShouldBeLessThanOrEqualTo(utcAfter.AddMilliseconds(500));
        @event.EventVersion.ShouldBe(1);
        @event.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void IntegrationEvent_CorrelationId_CanBeSet()
    {
        // Act
        var @event = new OrderCreatedIntegrationEvent
        {
            CorrelationId = "corr-123",
            OrderId = Guid.NewGuid(),
            CustomerName = "John"
        };

        // Assert
        @event.CorrelationId.ShouldBe("corr-123");
    }

    [Fact]
    public void IntegrationEvent_EventId_CanBeOverridden()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var @event = new OrderCreatedIntegrationEvent
        {
            EventId = eventId,
            OrderId = Guid.NewGuid(),
            CustomerName = "John"
        };

        // Assert
        @event.EventId.ShouldBe(eventId);
    }

    [Fact]
    public void IntegrationEvent_OccurredAtUtc_CanBeOverridden()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var @event = new OrderCreatedIntegrationEvent
        {
            OccurredAtUtc = occurredAt,
            OrderId = Guid.NewGuid(),
            CustomerName = "John"
        };

        // Assert
        @event.OccurredAtUtc.ShouldBe(occurredAt);
    }

    [Fact]
    public void IntegrationEvent_EventVersion_CanBeOverriddenInDerivedClass()
    {
        // Act
        var @event = new PaymentProcessedEvent
        {
            Amount = 100m
        };

        // Assert
        @event.EventVersion.ShouldBe(2);
    }

    [Fact]
    public void IntegrationEvent_ImplementsIIntegrationEvent()
    {
        // Arrange - capture time window to make OccurredAtUtc assertions stable
        var utcBefore = DateTime.UtcNow;
        var @event = new OrderCreatedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "John"
        };
        var utcAfter = DateTime.UtcNow;

        // Act & Assert - Verify interface is correctly implemented by accessing properties through interface
        @event.ShouldBeAssignableTo<IIntegrationEvent>();
        ((IIntegrationEvent)@event).EventId.ShouldNotBe(Guid.Empty);
        ((IIntegrationEvent)@event).OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(utcBefore);
        ((IIntegrationEvent)@event).OccurredAtUtc.ShouldBeLessThanOrEqualTo(utcAfter.AddMilliseconds(100));
        ((IIntegrationEvent)@event).EventVersion.ShouldBe(1);
    }

    [Fact]
    public void IntegrationEvent_SameValues_AreEqual()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var event1 = new OrderCreatedIntegrationEvent
        {
            EventId = eventId,
            OccurredAtUtc = occurredAt,
            OrderId = orderId,
            CustomerName = "John"
        };

        var event2 = new OrderCreatedIntegrationEvent
        {
            EventId = eventId,
            OccurredAtUtc = occurredAt,
            OrderId = orderId,
            CustomerName = "John"
        };

        // Assert
        event1.ShouldBe(event2);
        event1.GetHashCode().ShouldBe(event2.GetHashCode());
    }

    [Fact]
    public void IntegrationEvent_With_CreatesNewEventWithModifiedValue()
    {
        // Arrange
        var original = new OrderCreatedIntegrationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "Original"
        };

        // Act
        var modified = original with { CustomerName = "Modified" };

        // Assert
        modified.CustomerName.ShouldBe("Modified");
        modified.EventId.ShouldBe(original.EventId); // unchanged
        original.CustomerName.ShouldBe("Original"); // original unchanged
    }

    #endregion
}
