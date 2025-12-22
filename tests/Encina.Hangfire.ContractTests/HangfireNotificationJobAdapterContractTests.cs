using Encina.Hangfire;
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
    public async Task PublishAsync_WithValidNotification_ShouldInvokeMediatorPublish()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
        var adapter = new HangfireNotificationJobAdapter<TestNotification>(mediator, logger);
        var notification = new TestNotification("test message");

        mediator.Publish(notification, Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, Unit>(unit));

        // Act
        await adapter.PublishAsync(notification);

        // Assert
        await mediator.Received(1).Publish(notification, Arg.Any<CancellationToken>());
    }
}

// Test types (must be public for NSubstitute proxying with strong-named assemblies)
public sealed record TestNotification(string Message) : INotification;
