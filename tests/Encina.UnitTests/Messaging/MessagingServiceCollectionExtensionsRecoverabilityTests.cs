using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.Scheduling;
using Encina.Recoverability;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// Unit tests for the Recoverability branch (including the delayed-retry sub-branch) of
/// <see cref="MessagingServiceCollectionExtensions.AddMessagingServices{TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TSagaStore, TSagaFactory, TScheduledStore, TScheduledFactory, TOutboxProcessor}"/>,
/// which the six ADO.NET/Dapper packages share but had no direct test coverage for (#1333 CRAP
/// remediation). Routing Slip, Content Router and Scatter-Gather still have no dedicated
/// registration tests here; see the #1333 knowledge record's backlog note.
/// </summary>
public sealed class MessagingServiceCollectionExtensionsRecoverabilityTests
{
    private static ServiceCollection AddMessagingServices(MessagingConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddMessagingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(config);
        return services;
    }

    [Fact]
    public void AddMessagingServices_TransactionsAndInbox_RegistersTransactionBehaviorBeforeInboxBehavior()
    {
        // Behaviors run in registration order, first registered outermost: the inbox must run inside the
        // business transaction to enlist its processed mark in it (ADR-048).
        var services = AddMessagingServices(new MessagingConfiguration { UseTransactions = true, UseInbox = true });

        var behaviors = services
            .Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(sd => sd.ImplementationType)
            .ToList();

        var transaction = behaviors.IndexOf(typeof(TransactionPipelineBehavior<,>));
        var inbox = behaviors.IndexOf(typeof(InboxPipelineBehavior<,>));
        transaction.ShouldBeGreaterThanOrEqualTo(0);
        inbox.ShouldBeGreaterThan(transaction);
    }

    [Fact]
    public void AddMessagingServices_UseRecoverabilityFalse_DoesNotRegisterRecoverabilityBehavior()
    {
        var services = AddMessagingServices(new MessagingConfiguration { UseRecoverability = false });

        services.ShouldNotContain(sd =>
            sd.ServiceType == typeof(IPipelineBehavior<,>) &&
            sd.ImplementationType == typeof(RecoverabilityPipelineBehavior<,>));
    }

    [Fact]
    public void AddMessagingServices_UseRecoverabilityTrue_RegistersRecoverabilityBehaviorAndErrorClassifier()
    {
        var services = AddMessagingServices(new MessagingConfiguration { UseRecoverability = true });

        services.ShouldContain(sd => sd.ServiceType == typeof(IConfigureOptions<RecoverabilityOptions>));
        services.ShouldContain(sd => sd.ServiceType == typeof(IErrorClassifier));
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IPipelineBehavior<,>) &&
            sd.ImplementationType == typeof(RecoverabilityPipelineBehavior<,>));
    }

    [Fact]
    public void AddMessagingServices_UseRecoverabilityTrueWithCustomErrorClassifier_KeepsCustomClassifier()
    {
        var customClassifier = Substitute.For<IErrorClassifier>();
        var config = new MessagingConfiguration { UseRecoverability = true };
        config.RecoverabilityOptions.ErrorClassifier = customClassifier;

        var services = AddMessagingServices(config);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(customClassifier);
    }

    [Fact]
    public void AddMessagingServices_UseRecoverabilityTrueWithDefaults_ProcessorAndCheckIdleAndNoScheduler()
    {
        // Delayed retries are off by default (#2248): the check and the processor are registered but idle,
        // and the scheduler resolves to null.
        var services = AddMessagingServices(new MessagingConfiguration { UseRecoverability = true });
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetService<IDelayedRetryScheduler>().ShouldBeNull();
        var hosted = provider.GetServices<IHostedService>().Select(h => h.GetType()).ToList();
        hosted.ShouldContain(typeof(DelayedRetryStartupCheck));
        hosted.ShouldContain(typeof(DelayedRetryProcessor));
        provider.GetRequiredService<IOptions<RecoverabilityOptions>>().Value.EnableDelayedRetries.ShouldBeFalse();
    }

    [Fact]
    public void AddMessagingServices_UseRecoverabilityTrueWithDelayedRetriesEnabled_RegistersCheckSchedulerAndProcessor()
    {
        var config = new MessagingConfiguration { UseRecoverability = true };
        config.RecoverabilityOptions.EnableDelayedRetries = true;

        var services = AddMessagingServices(config);

        services.ShouldContain(sd => sd.ServiceType == typeof(IDelayedRetryScheduler));
        var hosted = services
            .Where(sd => sd.ServiceType == typeof(IHostedService))
            .Select(sd => sd.ImplementationType)
            .ToList();
        hosted.ShouldContain(typeof(DelayedRetryProcessor));
        hosted.IndexOf(typeof(DelayedRetryStartupCheck)).ShouldBeLessThan(hosted.IndexOf(typeof(DelayedRetryProcessor)));
    }

    [Fact]
    public void AddMessagingServices_UseRecoverabilityNextToAddEncinaRecoverability_ThrowsInBothOrders()
    {
        var config = new MessagingConfiguration { UseRecoverability = true };

        var useFirst = AddMessagingServices(config);
        var afterUse = Should.Throw<InvalidOperationException>(() => useFirst.AddEncinaRecoverability());

        var addFirst = new ServiceCollection();
        addFirst.AddEncinaRecoverability();
        var afterAdd = Should.Throw<InvalidOperationException>(() => addFirst.AddMessagingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(config));

        foreach (var message in new[] { afterUse.Message, afterAdd.Message })
        {
            message.ShouldContain("RecoverabilityPipelineBehavior");
            message.ShouldContain("ADR-050");
            message.ShouldContain("UseRecoverability");
        }
    }

    [Fact]
    public void AddMessagingServicesCore_UseRecoverabilityNextToAddEncinaRecoverability_ThrowsInBothOrders()
    {
        var config = new MessagingConfiguration { UseRecoverability = true };

        var useFirst = new ServiceCollection();
        useFirst.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            StubOutboxProcessor>(config);
        var afterUse = Should.Throw<InvalidOperationException>(() => useFirst.AddEncinaRecoverability());

        var addFirst = new ServiceCollection();
        addFirst.AddEncinaRecoverability();
        var afterAdd = Should.Throw<InvalidOperationException>(() => addFirst.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            StubOutboxProcessor>(config));

        foreach (var message in new[] { afterUse.Message, afterAdd.Message })
        {
            message.ShouldContain("RecoverabilityPipelineBehavior");
            message.ShouldContain("ADR-050");
            message.ShouldContain("UseRecoverability");
        }
    }

    #region Stub Types (minimal implementations for generic constraints)

    private sealed class StubInboxFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => Substitute.For<IInboxMessage>();
    }

    private sealed class StubSagaFactory : ISagaStateFactory
    {
        public ISagaState Create(Guid sagaId, string sagaType, string data, string status, int currentStep, DateTime startedAtUtc, DateTime? timeoutAtUtc = null)
            => Substitute.For<ISagaState>();
    }

    private sealed class StubScheduledFactory : IScheduledMessageFactory
    {
        public IScheduledMessage Create(Guid id, string requestType, string content, DateTime scheduledAtUtc, DateTime createdAtUtc, bool isRecurring, string? cronExpression)
            => Substitute.For<IScheduledMessage>();
    }

    private sealed class StubOutboxProcessor : IHostedService
    {
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    #endregion
}
