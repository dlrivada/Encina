using Encina.Hangfire;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.PropertyTests;

/// <summary>
/// Property-based tests for HangfireNotificationJobAdapter.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class HangfireNotificationJobAdapterPropertyTests
{
    [Fact]
    public async Task Property_SuccessfulPublication_AlwaysCompletes()
    {
        // Property: When Encina succeeds, adapter ALWAYS completes without exception

        var testNotifications = new[]
        {
            new TestNotification("message1"),
            new TestNotification("message2"),
            new TestNotification("message3"),
        };

        foreach (var notification in testNotifications)
        {
            // Arrange
            var Encina = Substitute.For<IEncina>();
            var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
            var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);

            Encina.Publish(notification, Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(unit));

            // Act & Assert - Should not throw
            await adapter.PublishAsync(notification);

            await Encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Property_Idempotency_SameNotificationSameOutcome()
    {
        // Property: Same notification ALWAYS produces same outcome

        var notification = new TestNotification("idempotent-test");
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
        var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);

        Encina.Publish(notification, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        // Act - Multiple publications
        await adapter.PublishAsync(notification);
        await adapter.PublishAsync(notification);
        await adapter.PublishAsync(notification);

        // Assert - Encina invoked 3 times (once per call)
        await Encina.Received(3).Publish(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Property_ConcurrentPublication_ThreadSafe()
    {
        // Property: Concurrent publications are thread-safe

        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
        var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);

        Encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        // Act - Execute concurrently
        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () => await adapter.PublishAsync(new TestNotification($"msg-{i}"))))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All calls complete without exception
        tasks.All(t => t.IsCompletedSuccessfully).ShouldBeTrue();
    }

    [Fact]
    public async Task Property_EncinaInvocation_AlwaysCalledExactlyOnce()
    {
        // Property: Encina ALWAYS invoked exactly once per publication

        var testNotifications = new[]
        {
            new TestNotification("notif1"),
            new TestNotification("notif2"),
            new TestNotification("notif3"),
        };

        foreach (var notification in testNotifications)
        {
            var Encina = Substitute.For<IEncina>();
            var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
            var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);

            Encina.Publish(notification, Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(unit));

            // Act
            await adapter.PublishAsync(notification);

            // Assert
            await Encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Property_DifferentNotifications_IndependentExecution()
    {
        // Property: Different notifications execute independently

        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();
        var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);

        var notifications = new[]
        {
            new TestNotification("notif-A"),
            new TestNotification("notif-B"),
            new TestNotification("notif-C"),
        };

        Encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        // Act
        foreach (var notification in notifications)
        {
            await adapter.PublishAsync(notification);
        }

        // Assert - Each notification published independently
        foreach (var notification in notifications)
        {
            await Encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        }
    }
}

// Test types
public sealed record TestNotification(string Message) : INotification;
