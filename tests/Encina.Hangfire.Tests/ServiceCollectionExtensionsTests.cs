using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Hangfire.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaHangfire_RegistersAdapters()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaHangfire();

        // Assert - Verify the generic type registrations exist
        services.ShouldContain(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(HangfireRequestJobAdapter<,>));

        services.ShouldContain(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(HangfireNotificationJobAdapter<>));
    }

    [Fact]
    public void AddEncinaHangfire_RegistersAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaHangfire();

        // Assert
        var requestAdapterDescriptor = services.FirstOrDefault(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(HangfireRequestJobAdapter<,>));

        var notificationAdapterDescriptor = services.FirstOrDefault(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(HangfireNotificationJobAdapter<>));

        requestAdapterDescriptor.ShouldNotBeNull();
        requestAdapterDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Transient);

        notificationAdapterDescriptor.ShouldNotBeNull();
        notificationAdapterDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaHangfire_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaHangfire();
        services.AddEncinaHangfire(); // Should not throw

        // Assert
        services.ShouldNotBeEmpty();
    }

    // Extension methods tests (EnqueueRequest, ScheduleRequest, etc.) require Hangfire infrastructure
    // These are integration tests and would need a running Hangfire server
    // For unit tests, we verify the adapters themselves work correctly (tested above)

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
    public record TestNotification(string Message) : INotification;
}
