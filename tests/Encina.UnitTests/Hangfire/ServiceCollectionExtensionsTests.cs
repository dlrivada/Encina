using Encina.Hangfire;
using Encina.Hangfire.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Hangfire;

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

    [Fact]
    public void AddEncinaHangfire_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configureInvoked = false;

        // Act
        services.AddEncinaHangfire(options =>
        {
            configureInvoked = true;
        });

        // Assert
        configureInvoked.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaHangfire_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IServiceProvider>());

        // Act
        services.AddEncinaHangfire(options =>
        {
            options.ProviderHealthCheck.Enabled = true;
        });

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IEncinaHealthCheck));
        services.ShouldContain(sd => sd.ServiceType == typeof(ProviderHealthCheckOptions));
    }

    [Fact]
    public void AddEncinaHangfire_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaHangfire(options =>
        {
            options.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        services.ShouldNotContain(sd => sd.ServiceType == typeof(IEncinaHealthCheck));
    }

    [Fact]
    public void AddEncinaHangfire_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaHangfire();

        // Assert
        result.ShouldBe(services);
    }

    [Fact]
    public void AddEncinaHangfire_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaHangfire();
        services.AddEncinaHangfire();

        // Assert - Should only have one registration of each type
        var requestAdapters = services.Count(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(HangfireRequestJobAdapter<,>));

        var notificationAdapters = services.Count(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(HangfireNotificationJobAdapter<>));

        requestAdapters.ShouldBe(1);
        notificationAdapters.ShouldBe(1);
    }

    // NOTE: Extension methods like EnqueueRequest, ScheduleRequestWithDelay, etc.
    // cannot be unit tested with mocks because they use Hangfire's expression parsing.
    // These are integration-tested with a real Hangfire instance in integration tests.
    // The coverage for ServiceCollectionExtensions focuses on the DI registration logic.

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
    public record TestNotification(string Message) : INotification;
}
