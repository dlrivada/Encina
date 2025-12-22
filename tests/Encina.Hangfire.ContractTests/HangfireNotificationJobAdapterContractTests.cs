using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.ContractTests;

/// <summary>
/// Contract tests for HangfireNotificationJobAdapter.
/// Verifies that the adapter correctly implements its contract.
/// </summary>
public sealed class HangfireNotificationJobAdapterContractTests
{
    [Fact]
    public async Task PublishAsync_WithValidNotification_ShouldInvokeEncinaPublish()
    {
        // Arrange
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
        var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);
        var notification = new TestNotification("test message");

        Encina.Publish(notification, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        // Act
        await adapter.PublishAsync(notification);

        // Assert
        await Encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
    }
}

// Test types (must be public for NSubstitute proxying with strong-named assemblies)
public sealed record TestNotification(string Message) : INotification;
