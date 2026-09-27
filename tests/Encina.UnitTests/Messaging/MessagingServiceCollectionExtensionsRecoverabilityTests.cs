using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.Scheduling;
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

        services.ShouldContain(sd => sd.ServiceType == typeof(RecoverabilityOptions));
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
    public void AddMessagingServices_UseRecoverabilityTrueWithDelayedRetriesDisabled_DoesNotRegisterDelayedRetryProcessor()
    {
        var config = new MessagingConfiguration { UseRecoverability = true };
        config.RecoverabilityOptions.EnableDelayedRetries = false;

        var services = AddMessagingServices(config);

        services.ShouldNotContain(sd =>
            sd.ServiceType == typeof(IHostedService) &&
            sd.ImplementationType == typeof(DelayedRetryProcessor));
    }

    [Fact]
    public void AddMessagingServices_UseRecoverabilityTrueWithDelayedRetriesEnabled_RegistersDelayedRetryProcessorAndScheduler()
    {
        var config = new MessagingConfiguration { UseRecoverability = true };
        config.RecoverabilityOptions.EnableDelayedRetries = true;

        var services = AddMessagingServices(config);

        services.ShouldContain(sd => sd.ServiceType == typeof(IDelayedRetryScheduler));
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IHostedService) &&
            sd.ImplementationType == typeof(DelayedRetryProcessor));
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
